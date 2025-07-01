using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.Rendering
{
    // Describes a single type in a GraphicsArchetype that overrides a material property
    internal struct ArchetypePropertyOverride : IEquatable<ArchetypePropertyOverride>, IComparable<ArchetypePropertyOverride>
    {
        // NameID of the shader material property being overridden, also used as the sorting key
        public int NameID;
        // IComponentData (or ISharedComponentData) TypeIndex of the overriding component
        public int TypeIndex;
        // Byte size of the ECS data in the chunks
        public short SizeBytesCPU;
        // Byte size of the GPU data, can differ from SizeBytesCPU e.g. for transform matrices
        public short SizeBytesGPU;

        public bool Equals(ArchetypePropertyOverride other)
        {
            return CompareTo(other) == 0;
        }

        public int CompareTo(ArchetypePropertyOverride other)
        {
            int cmp_NameID = NameID.CompareTo(other.NameID);
            int cmp_TypeIndex = TypeIndex.CompareTo(other.TypeIndex);

            if (cmp_NameID != 0) return cmp_NameID;
            return cmp_TypeIndex;
        }
    }

    // Precomputed collection of the subset of types in an EntityArchetype that are used by Entities Graphics package
    internal unsafe struct GraphicsArchetype : IDisposable, IEquatable<GraphicsArchetype>, IComparable<GraphicsArchetype>
    {
        // All IComponentData overrides, which allocate memory per entity.
        // Sorted in NameID order.
        public UnsafeList<ArchetypePropertyOverride> PropertyComponents;

        public GraphicsArchetype Clone(Allocator allocator)
        {
            var overrides = new UnsafeList<ArchetypePropertyOverride>(PropertyComponents.Length, allocator);
            overrides.AddRangeNoResize(PropertyComponents);

            return new GraphicsArchetype
            {
                PropertyComponents = overrides,
            };
        }

        public int MaxEntitiesPerBatch
        {
            get
            {
                int fixedBytes = 0;
                int bytesPerEntity = 0;

                for (int i = 0; i < PropertyComponents.Length; ++i)
                    bytesPerEntity += PropertyComponents[i].SizeBytesGPU;

                int maxBytes = EntitiesGraphicsSystem.MaxBytesPerBatch;
                int maxBytesForEntities = maxBytes - fixedBytes;

                return maxBytesForEntities / math.max(1, bytesPerEntity);
            }
        }

        // Shouldn't need to compare globals, as they should be completely determined
        // by the components

        public bool Equals(GraphicsArchetype other)
        {
            return CompareTo(other) == 0;
        }

        public int CompareTo(GraphicsArchetype other)
        {
            int numA = PropertyComponents.Length;
            int numB = other.PropertyComponents.Length;

            if (numA < numB) return -1;
            if (numA > numB) return 1;

            return UnsafeUtility.MemCmp(
                PropertyComponents.Ptr,
                other.PropertyComponents.Ptr,
                numA * UnsafeUtility.SizeOf<ArchetypePropertyOverride>());
        }

        public override int GetHashCode()
        {
            return (int)xxHash3.Hash64(
                PropertyComponents.Ptr,
                PropertyComponents.Length * UnsafeUtility.SizeOf<ArchetypePropertyOverride>()).x;
        }

        public void Dispose()
        {
            if (PropertyComponents.IsCreated) PropertyComponents.Dispose();
        }

        public struct MetadataValueComparer : IComparer<MetadataValue>
        {
            public int Compare(MetadataValue x, MetadataValue y)
            {
                return x.NameID.CompareTo(y.NameID);
            }
        }
    }

    internal struct EntitiesGraphicsArchetypes : IDisposable
    {
        private NativeParallelHashMap<EntityArchetype, int> m_GraphicsArchetypes;
        private NativeParallelHashMap<GraphicsArchetype, int> m_GraphicsArchetypeDeduplication;
        private NativeList<GraphicsArchetype> m_GraphicsArchetypeList;

        public EntitiesGraphicsArchetypes(int capacity)
        {
            m_GraphicsArchetypes = new NativeParallelHashMap<EntityArchetype, int>(capacity, Allocator.Persistent);
            m_GraphicsArchetypeDeduplication =
                new NativeParallelHashMap<GraphicsArchetype, int>(capacity, Allocator.Persistent);
            m_GraphicsArchetypeList = new NativeList<GraphicsArchetype>(capacity, Allocator.Persistent);
        }

        public void Dispose()
        {
            for (int i = 0; i < m_GraphicsArchetypeList.Length; ++i)
                m_GraphicsArchetypeList[i].Dispose();

            m_GraphicsArchetypes.Dispose();
            m_GraphicsArchetypeDeduplication.Dispose();
            m_GraphicsArchetypeList.Dispose();
        }

        public GraphicsArchetype GetGraphicsArchetype(int index) => m_GraphicsArchetypeList[index];

        public int GetGraphicsArchetypeIndex(
            EntityArchetype archetype,
            NativeParallelHashMap<int, MaterialPropertyType> typeIndexToMaterialProperty, ref MaterialPropertyType failureProperty)
        {
            int archetypeIndex;
            if (m_GraphicsArchetypes.TryGetValue(archetype, out archetypeIndex))
                return archetypeIndex;

            var types = archetype.GetComponentTypes(Allocator.Temp);

            var overrides = new UnsafeList<ArchetypePropertyOverride>(types.Length, Allocator.Temp);
            bool AddOverrideForType(ComponentType type)
            {
                if (typeIndexToMaterialProperty.TryGetValue(type.TypeIndex, out var property))
                {
                    if (type.TypeIndex != property.TypeIndex)
                        return false;

                    overrides.Add(new ArchetypePropertyOverride
                    {
                        NameID = property.NameID,
                        TypeIndex = property.TypeIndex,
                        SizeBytesCPU = property.SizeBytesCPU,
                        SizeBytesGPU = property.SizeBytesGPU,
                    });
                }

                return true;

                // If the type is not found, it was a CPU only type and ignored by Entities Graphics package.
            }

            for (int i = 0; i < types.Length; ++i)
            {
                if (!AddOverrideForType(types[i]))
                {
                    typeIndexToMaterialProperty.TryGetValue(types[i].TypeIndex, out failureProperty);
                    return -1;
                }
            }

            // Entity is not returned by GetComponentTypes, so we handle it explicitly
            AddOverrideForType(ComponentType.ReadOnly<Entity>());

            overrides.Sort();

            GraphicsArchetype graphicsArchetype = new GraphicsArchetype
            {
                PropertyComponents = overrides,
            };

            // If the same archetype has already been created, make sure to use the same index
            if (m_GraphicsArchetypeDeduplication.TryGetValue(graphicsArchetype, out archetypeIndex))
            {
                graphicsArchetype.Dispose();
                return archetypeIndex;
            }
            // If this is the first time this archetype has been seen, make a permanent copy of it.
            else
            {
                archetypeIndex = m_GraphicsArchetypeList.Length;
                graphicsArchetype = graphicsArchetype.Clone(Allocator.Persistent);
                overrides.Dispose();

                m_GraphicsArchetypeDeduplication[graphicsArchetype] = archetypeIndex;
                m_GraphicsArchetypeList.Add(graphicsArchetype);
                return archetypeIndex;
            }
        }
    }

}
