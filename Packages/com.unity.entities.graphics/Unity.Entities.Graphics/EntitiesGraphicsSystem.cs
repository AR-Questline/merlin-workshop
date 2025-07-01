// This define fails tests due to the extra log spam. Don't check this in enabled
// #define DEBUG_LOG_HYBRID_RENDERER

// #define DEBUG_LOG_CHUNK_CHANGES
// #define DEBUG_LOG_GARBAGE_COLLECTION
// #define DEBUG_LOG_BATCH_UPDATES
// #define DEBUG_LOG_CHUNKS
// #define DEBUG_LOG_INVALID_CHUNKS
// #define DEBUG_LOG_UPLOADS
// #define DEBUG_LOG_BATCH_CREATION
// #define DEBUG_LOG_BATCH_DELETION
// #define DEBUG_LOG_PROPERTY_ALLOCATIONS
// #define DEBUG_LOG_PROPERTY_UPDATES
// #define DEBUG_LOG_VISIBLE_INSTANCES
// #define DEBUG_LOG_MATERIAL_PROPERTY_TYPES
// #define DEBUG_LOG_MEMORY_USAGE
// #define DEBUG_LOG_AMBIENT_PROBE
// #define DEBUG_LOG_DRAW_COMMANDS
// #define DEBUG_LOG_DRAW_COMMANDS_VERBOSE
// #define DEBUG_VALIDATE_DRAW_COMMAND_SORT
// #define DEBUG_LOG_BRG_MATERIAL_MESH
// #define DEBUG_LOG_GLOBAL_AABB
// #define PROFILE_BURST_JOB_INTERNALS
// #define DISABLE_HYBRID_RENDERER_ERROR_LOADING_SHADER
// #define DISABLE_INCLUDE_EXCLUDE_LIST_FILTERING
// #define DISABLE_MATERIALMESHINFO_BOUNDS_CHECKING

// Entities Graphics is disabled if SRP 10 is not found, unless an override define is present
// It is also disabled if -nographics is given from the command line.
#if !(SRP_10_0_0_OR_NEWER || HYBRID_RENDERER_ENABLE_WITHOUT_SRP)
#define HYBRID_RENDERER_DISABLED
#endif

#if UNITY_EDITOR
#define USE_PROPERTY_ASSERTS
#endif

#if UNITY_EDITOR
#define DEBUG_PROPERTY_NAMES
#endif

#if ENABLE_UNITY_OCCLUSION
#define USE_UNITY_OCCLUSION
#endif

#if UNITY_EDITOR && !DISABLE_HYBRID_RENDERER_PICKING
#define ENABLE_PICKING
#endif

#if (ENABLE_UNITY_COLLECTIONS_CHECKS || DEVELOPMENT_BUILD) && !DISABLE_MATERIALMESHINFO_BOUNDS_CHECKING
#define ENABLE_MATERIALMESHINFO_BOUNDS_CHECKING
#endif

using System;
using System.Collections.Generic;
using System.Text;
using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Profiling;
using Unity.Entities.Graphics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

#if URP_10_0_0_OR_NEWER && UNITY_EDITOR
using System.Reflection;
using UnityEngine.Rendering.Universal;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

#if USE_UNITY_OCCLUSION
using Unity.Rendering.Occlusion;
#endif

namespace Unity.Rendering
{
    // Describes a single material property that can be mapped to an ECS type.
    // Contains the name as a string, unlike the other types.
    internal struct NamedPropertyMapping
    {
        public string Name;
        public short SizeCPU;
        public short SizeGPU;
    }

    internal struct EntitiesGraphicsTuningConstants
    {
        public const int kMaxInstancesPerDrawCommand = 4096;
        public const int kMaxInstancesPerDrawRange   = 4096;
        public const int kMaxDrawCommandsPerDrawRange = 512;
    }

    // Contains the immutable properties that are set
    // upon batch creation. Only chunks with identical BatchCreateInfo
    // can be combined in a single batch.
    internal struct BatchCreateInfo : IEquatable<BatchCreateInfo>, IComparable<BatchCreateInfo>
    {
        // Unique deduplicated GraphicsArchetype index. Chunks can be combined if their
        // index is the same.
        public int GraphicsArchetypeIndex;
        public ArchetypeChunk Chunk;

        public bool Equals(BatchCreateInfo other)
        {
            return CompareTo(other) == 0;
        }

        public int CompareTo(BatchCreateInfo other) => GraphicsArchetypeIndex.CompareTo(other.GraphicsArchetypeIndex);
    }

    internal struct BatchCreateInfoFactory
    {
        public EntitiesGraphicsArchetypes GraphicsArchetypes;
        public NativeParallelHashMap<int, MaterialPropertyType> TypeIndexToMaterialProperty;

        public BatchCreateInfo Create(ArchetypeChunk chunk, ref MaterialPropertyType failureProperty)
        {
            return new BatchCreateInfo
            {
                GraphicsArchetypeIndex =
                    GraphicsArchetypes.GetGraphicsArchetypeIndex(chunk.Archetype, TypeIndexToMaterialProperty, ref failureProperty),
                Chunk = chunk,
            };
        }
    }

    internal struct BatchInfo
    {
        public HeapBlock GPUMemoryAllocation;
        public HeapBlock ChunkMetadataAllocation;
    }

    public struct BatchMaterialMeshSubMesh
    {
        public BatchMaterialID Material;
        public BatchMeshID Mesh;
        public int SubMeshIndex;
    }

    public struct BRGRenderMeshArray
    {
        public int Version;
        public UnsafeList<BatchMaterialID> UniqueMaterials;
        public UnsafeList<BatchMeshID> UniqueMeshes;
        public UnsafeList<BatchMaterialMeshSubMesh> MaterialMeshSubMeshes;
        public uint4 Hash128;

        public BatchMaterialID GetMaterialID(MaterialMeshInfo materialMeshInfo)
        {
            // When using an index range, just return the first material of the range
            if (materialMeshInfo.HasMaterialMeshIndexRange)
            {
                if (!MaterialMeshSubMeshes.IsCreated)
                    return BatchMaterialID.Null;

                RangeInt range = materialMeshInfo.MaterialMeshIndexRange;
                Assert.IsTrue(range.length > 0);

                return MaterialMeshSubMeshes[range.start].Material;
            }
            else
            {
                if (!UniqueMaterials.IsCreated)
                    return BatchMaterialID.Null;

                int materialIndex = materialMeshInfo.MaterialArrayIndex;
                if (materialIndex == -1 || materialIndex >= UniqueMaterials.Length)
                    return BatchMaterialID.Null;

                return UniqueMaterials[materialIndex];
            }
        }

        public BatchMeshID GetMeshID(MaterialMeshInfo materialMeshInfo)
        {
            // When using an index range, just return the first mesh of the range
            if (materialMeshInfo.HasMaterialMeshIndexRange)
            {
                if (!MaterialMeshSubMeshes.IsCreated)
                    return BatchMeshID.Null;

                RangeInt range = materialMeshInfo.MaterialMeshIndexRange;
                Assert.IsTrue(range.length > 0);

                return MaterialMeshSubMeshes[range.start].Mesh;
            }
            else
            {
                if (!UniqueMeshes.IsCreated)
                    return BatchMeshID.Null;

                int meshIndex = materialMeshInfo.MeshArrayIndex;
                if (meshIndex == -1 || meshIndex >= UniqueMeshes.Length)
                    return BatchMeshID.Null;

                return UniqueMeshes[meshIndex];
            }
        }
    }

    [BurstCompile]
    internal struct BoundsCheckMaterialMeshIndexJob : IJobChunk
    {
        [ReadOnly] public SharedComponentTypeHandle<RenderMeshArray> RenderMeshArrayHandle;
        [ReadOnly] public NativeParallelHashMap<int, BRGRenderMeshArray> BRGRenderMeshArrays;
        [ReadOnly] public ComponentTypeHandle<MaterialMeshInfo> MaterialMeshInfoHandle;
        public EntityTypeHandle EntityHandle;
        public NativeList<Entity>.ParallelWriter EntitiesWithOutOfBoundsMMI;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMaskIn)
        {
            var chunkEnabledMask = useEnabledMask ? chunkEnabledMaskIn : EntitiesGraphicsUtils.ComputeBitmask(chunk.Count);

            var sharedComponentIndex = chunk.GetSharedComponentIndex(RenderMeshArrayHandle);
            var materialMeshInfos = chunk.GetNativeArray(ref MaterialMeshInfoHandle);
            var entities = chunk.GetNativeArray(EntityHandle);

            BRGRenderMeshArray brgRenderMeshArray;
            bool found = BRGRenderMeshArrays.TryGetValue(sharedComponentIndex, out brgRenderMeshArray);

            if (!found)
                return;
            var materials = brgRenderMeshArray.UniqueMaterials;
            var meshes = brgRenderMeshArray.UniqueMeshes;
            var matMeshSubMeshes = brgRenderMeshArray.MaterialMeshSubMeshes;

            for (var j = 0; j < 2; ++j)
            {
                var enabledWord = (j == 0) ? chunkEnabledMask.ULong0 : chunkEnabledMask.ULong1;
                while (enabledWord != 0)
                {
                    int bitIndex = math.tzcnt(enabledWord);
                    int entityIndex = (j << 6) + bitIndex;
                    ulong entityMask = 1ul << bitIndex;
                    enabledWord ^= entityMask;

                    var materialMeshInfo = materialMeshInfos[entityIndex];
                    bool outOfBounds = false;

                    if (materialMeshInfo.HasMaterialMeshIndexRange)
                    {
                        RangeInt matMeshIndexRange = materialMeshInfo.MaterialMeshIndexRange;
                        outOfBounds = outOfBounds || matMeshIndexRange.end > matMeshSubMeshes.Length;
                    }
                    else
                    {
                        if (!materialMeshInfo.IsRuntimeMaterial)
                            outOfBounds = outOfBounds || (materialMeshInfo.MaterialArrayIndex) >= materials.Length;

                        if (!materialMeshInfo.IsRuntimeMesh)
                            outOfBounds = outOfBounds || materialMeshInfo.MeshArrayIndex >= meshes.Length;
                    }

                    if (outOfBounds)
                        EntitiesWithOutOfBoundsMMI.AddNoResize(entities[entityIndex]);

                }
            }
        }
    }

    [BurstCompile]
    internal struct InitializeUnreferencedIndicesScatterJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<int> ExistingBatchIndices;
        public NativeArray<long> UnreferencedBatchIndices;

        public unsafe void Execute(int index)
        {
            int batchIndex = ExistingBatchIndices[index];

            AtomicHelpers.IndexToQwIndexAndMask(batchIndex, out int qw, out long mask);

            Assert.IsTrue(qw < UnreferencedBatchIndices.Length, "Batch index out of bounds");

            AtomicHelpers.AtomicOr((long*)UnreferencedBatchIndices.GetUnsafePtr(), qw, mask);
        }
    }

    internal struct BatchCreationTypeHandles
    {
        public ComponentTypeHandle<LODRange> LODRange;
        public ComponentTypeHandle<PerInstanceCullingTag> PerInstanceCulling;

        public BatchCreationTypeHandles(ComponentSystemBase componentSystemBase)
        {
            LODRange = componentSystemBase.GetComponentTypeHandle<LODRange>(true);
            PerInstanceCulling = componentSystemBase.GetComponentTypeHandle<PerInstanceCullingTag>(true);
        }
    }

    internal struct ChunkProperty
    {
        public int ComponentTypeIndex;
        public int ValueSizeBytesCPU;
        public int ValueSizeBytesGPU;
        public int GPUDataBegin;
    }

    // Describes a single ECS component type => material property mapping
    internal struct MaterialPropertyType
    {
        public int TypeIndex;
        public int NameID;
        public short SizeBytesCPU;
        public short SizeBytesGPU;

        public string TypeName => EntitiesGraphicsSystem.TypeIndexToName(TypeIndex);
        public string PropertyName => EntitiesGraphicsSystem.NameIDToName(NameID);
    }

    /// <summary>
    /// A system that registers Materials and meshes with the BatchRendererGroup.
    /// </summary>
    //@TODO: Updating always necessary due to empty component group. When Component group and archetype chunks are unified, [RequireMatchingQueriesForUpdate] can be added.
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(EntitiesGraphicsSystem))]
    [CreateAfter(typeof(EntitiesGraphicsSystem))]
    public partial class RegisterMaterialsAndMeshesSystem : SystemBase
    {
        // Reuse Lists used for GetAllUniqueSharedComponentData to avoid GC allocs every frame
        private List<RenderMeshArray> m_RenderMeshArrays = new List<RenderMeshArray>();
        private List<int> m_SharedComponentIndices = new List<int>();
        private List<int> m_SharedComponentVersions = new List<int>();

        NativeParallelHashMap<int, BRGRenderMeshArray> m_BRGRenderMeshArrays;
        public NativeParallelHashMap<int, BRGRenderMeshArray> BRGRenderMeshArrays => m_BRGRenderMeshArrays;

        EntitiesGraphicsSystem m_RendererSystem;

#if ENABLE_MATERIALMESHINFO_BOUNDS_CHECKING
        private EntityQuery m_ChangedMaterialMeshQuery = default;
        private NativeList<Entity> m_EntitiesWithOutOfBoundsMMI;
        private JobHandle m_BoundsCheckHandle = default;
#endif

        /// <summary>
        /// Called when this system is created.
        /// </summary>
        protected override void OnCreate()
        {
            if (!EntitiesGraphicsSystem.EntitiesGraphicsEnabled)
            {
                Enabled = false;
                return;
            }

            m_BRGRenderMeshArrays = new NativeParallelHashMap<int, BRGRenderMeshArray>(256, Allocator.Persistent);
            m_RendererSystem = World.GetExistingSystemManaged<EntitiesGraphicsSystem>();

#if ENABLE_MATERIALMESHINFO_BOUNDS_CHECKING
            m_ChangedMaterialMeshQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadWrite<MaterialMeshInfo>()
                },
                Options = EntityQueryOptions.IncludeDisabledEntities,
            });
            m_ChangedMaterialMeshQuery.SetChangedVersionFilter(ComponentType.ReadWrite<MaterialMeshInfo>());
#endif
        }

        /// <summary>
        /// Called when this system is updated.
        /// </summary>
        protected override void OnUpdate()
        {
            Profiler.BeginSample("RegisterMaterialsAndMeshes");
            Dependency = RegisterMaterialsAndMeshes(Dependency);
            Profiler.EndSample();
        }

        /// <summary>
        /// Called when this system is destroyed.
        /// </summary>
        protected override void OnDestroy()
        {
            if (!EntitiesGraphicsSystem.EntitiesGraphicsEnabled) return;

            var brgRenderArrays = m_BRGRenderMeshArrays.GetValueArray(Allocator.Temp);
            for (int i = 0; i < brgRenderArrays.Length; ++i)
            {
                var brgRenderArray = brgRenderArrays[i];
                UnregisterMaterialsMeshes(brgRenderArray);
                brgRenderArray.UniqueMaterials.Dispose();
                brgRenderArray.UniqueMeshes.Dispose();
                brgRenderArray.MaterialMeshSubMeshes.Dispose();
            }
            m_BRGRenderMeshArrays.Dispose();
        }

        public void GetCachedRenderMeshArrays(out List<RenderMeshArray> renderArrays, out List<int> sharedIndices, out List<int> sharedVersions)
        {
            renderArrays = m_RenderMeshArrays;
            sharedIndices = m_SharedComponentIndices;
            sharedVersions = m_SharedComponentVersions;
        }

        private void UnregisterMaterialsMeshes(in BRGRenderMeshArray brgRenderArray)
        {
            foreach (var id in brgRenderArray.UniqueMaterials)
            {
                m_RendererSystem.UnregisterMaterial(id);
            }

            foreach (var id in brgRenderArray.UniqueMeshes)
            {
                m_RendererSystem.UnregisterMesh(id);
            }
        }

        private void GetFilteredRenderMeshArrays(out List<RenderMeshArray> renderArrays, out List<int> sharedIndices, out List<int> sharedVersions)
        {
            m_RenderMeshArrays.Clear();
            m_SharedComponentIndices.Clear();
            m_SharedComponentVersions.Clear();

            renderArrays = m_RenderMeshArrays;
            sharedIndices = m_SharedComponentIndices;
            sharedVersions = m_SharedComponentVersions;

            EntityManager.GetAllUniqueSharedComponentsManaged<RenderMeshArray>(renderArrays, sharedIndices, sharedVersions);
            //Debug.Log($"BRG update: Found {renderArrays.Count} unique RenderMeshArray components:");

            // Discard null RenderMeshArray components
            var discardedIndices = new NativeList<int>(renderArrays.Count, Allocator.Temp);

            // Reverse iteration to make the index list sorted in decreasing order
            // We need this to safely remove the indices one after the other later
            for (int i = renderArrays.Count - 1; i >= 0; --i)
            {
                var array = renderArrays[i];
                if (array.Materials == null || array.Meshes == null)
                {
                    discardedIndices.Add(i);
                }
            }

            foreach (var i in discardedIndices)
            {
                renderArrays.RemoveAt(i);
                sharedIndices.RemoveAt(i);
                sharedVersions.RemoveAt(i);
            }

            discardedIndices.Dispose();
        }

        private JobHandle RegisterMaterialsAndMeshes(JobHandle inputDeps)
        {
            GetFilteredRenderMeshArrays(out var renderArrays, out var sharedIndices, out var sharedVersions);

            var brgArraysToDispose = new NativeList<BRGRenderMeshArray>(renderArrays.Count, Allocator.Temp);

            // Remove RenderMeshArrays that no longer exist
            var sortedKeys = m_BRGRenderMeshArrays.GetKeyArray(Allocator.Temp);
            sortedKeys.Sort();

            // Single pass O(n) algorithm. Both arrays are guaranteed to be sorted.
            int j = 0;
            for (int i = 0; i < sortedKeys.Length; i++)
            {
                var oldKey = sortedKeys[i];
                while ((j < renderArrays.Count) && (sharedIndices[j] < oldKey))
                {
                    j++;
                }

                bool notFound = j == renderArrays.Count || oldKey != sharedIndices[j];
                if (notFound)
                {
                    var brgRenderArray = m_BRGRenderMeshArrays[oldKey];
                    brgArraysToDispose.Add(brgRenderArray);

                    m_BRGRenderMeshArrays.Remove(oldKey);
                }
            }

            sortedKeys.Dispose();

            // Update/add RenderMeshArrays
            for (int ri = 0; ri < renderArrays.Count; ++ri)
            {
                var renderArray = renderArrays[ri];
                if (renderArray.Materials == null || renderArray.Meshes == null)
                {
                    Debug.LogError("This loop should not process null RenderMeshArray components");
                    continue;
                }

                var sharedIndex = sharedIndices[ri];
                var sharedVersion = sharedVersions[ri];
                var materialCount = renderArray.Materials.Length;
                var meshCount = renderArray.Meshes.Length;
                var matMeshIndexCount = renderArray.MaterialMeshIndices != null ? renderArray.MaterialMeshIndices.Length : 0;
                uint4 hash128 = renderArray.GetHash128();

                bool update = false;
                BRGRenderMeshArray brgRenderArray;
                if (m_BRGRenderMeshArrays.TryGetValue(sharedIndex, out brgRenderArray))
                {
                    // Version change means that the shared component was deleted and another one was created with the same index
                    // It's also possible that the contents changed and the version number did not, so we also compare the 128-bit hash
                    if ((brgRenderArray.Version != sharedVersion) ||
                        math.any(brgRenderArray.Hash128 != hash128))
                    {
                        brgArraysToDispose.Add(brgRenderArray);
                        update = true;

#if DEBUG_LOG_BRG_MATERIAL_MESH
                        Debug.Log($"BRG Material Mesh : RenderMeshArray version change | SharedIndex ({sharedIndex}) | SharedVersion ({brgRenderArray.Version}) -> ({sharedVersion})");
#endif
                    }
                }
                else
                {
                    brgRenderArray = new BRGRenderMeshArray();
                    update = true;

#if DEBUG_LOG_BRG_MATERIAL_MESH
                    Debug.Log($"BRG Material Mesh : New RenderMeshArray found | SharedIndex ({sharedIndex})");
#endif
                }

                if (update)
                {
                    brgRenderArray.Version = sharedVersion;
                    brgRenderArray.Hash128 = hash128;
                    brgRenderArray.UniqueMaterials = new UnsafeList<BatchMaterialID>(materialCount, Allocator.Persistent);
                    brgRenderArray.UniqueMeshes = new UnsafeList<BatchMeshID>(meshCount, Allocator.Persistent);
                    brgRenderArray.MaterialMeshSubMeshes = new UnsafeList<BatchMaterialMeshSubMesh>(matMeshIndexCount, Allocator.Persistent);

                    for (int i = 0; i < materialCount; ++i)
                    {
                        var material = renderArray.Materials[i];
                        var id = m_RendererSystem.RegisterMaterial(material);
                        if (id == BatchMaterialID.Null)
                            Debug.LogWarning($"Registering material {material?.ToString() ?? "null"} at index {i} inside a RenderMeshArray failed.");

                        brgRenderArray.UniqueMaterials.Add(id);
                    }

                    for (int i = 0; i < meshCount; ++i)
                    {
                        var mesh = renderArray.Meshes[i];
                        var id = m_RendererSystem.RegisterMesh(mesh);
                        if (id == BatchMeshID.Null)
                            Debug.LogWarning($"Registering mesh {mesh?.ToString() ?? "null"} at index {i} inside a RenderMeshArray failed.");

                        brgRenderArray.UniqueMeshes.Add(id);
                    }

                    for (int i = 0; i < matMeshIndexCount; ++i)
                    {
                        MaterialMeshIndex matMeshIndex = renderArray.MaterialMeshIndices[i];

                        BatchMaterialID materialID = BatchMaterialID.Null;
                        if (matMeshIndex.MaterialIndex != -1)
                            materialID = brgRenderArray.UniqueMaterials[matMeshIndex.MaterialIndex];

                        BatchMeshID meshID = BatchMeshID.Null;
                        if (matMeshIndex.MeshIndex != -1)
                            meshID = brgRenderArray.UniqueMeshes[matMeshIndex.MeshIndex];

                        brgRenderArray.MaterialMeshSubMeshes.Add(new BatchMaterialMeshSubMesh
                        {
                            Material = materialID,
                            Mesh = meshID,
                            SubMeshIndex = matMeshIndex.SubMeshIndex,
                        });
                    }

                    m_BRGRenderMeshArrays[sharedIndex] = brgRenderArray;
                }
            }

            for (int i = 0; i < brgArraysToDispose.Length; ++i)
            {
                var brgRenderArray = brgArraysToDispose[i];
                UnregisterMaterialsMeshes(brgRenderArray);
                brgRenderArray.UniqueMaterials.Dispose();
                brgRenderArray.UniqueMeshes.Dispose();
                brgRenderArray.MaterialMeshSubMeshes.Dispose();
            }

#if ENABLE_MATERIALMESHINFO_BOUNDS_CHECKING
            // Fire jobs to remap offline->runtime indices
            m_EntitiesWithOutOfBoundsMMI = new NativeList<Entity>(
                m_ChangedMaterialMeshQuery.CalculateEntityCountWithoutFiltering(),
                WorldUpdateAllocator);
            m_BoundsCheckHandle = new BoundsCheckMaterialMeshIndexJob
            {
                RenderMeshArrayHandle = GetSharedComponentTypeHandle<RenderMeshArray>(),
                MaterialMeshInfoHandle = GetComponentTypeHandle<MaterialMeshInfo>(true),
                EntityHandle = GetEntityTypeHandle(),
                BRGRenderMeshArrays = m_BRGRenderMeshArrays,
                EntitiesWithOutOfBoundsMMI = m_EntitiesWithOutOfBoundsMMI.AsParallelWriter(),
            }
            .ScheduleParallel(m_ChangedMaterialMeshQuery, inputDeps);

            return m_BoundsCheckHandle;
#else
            return default;
#endif
        }

        internal void LogBoundsCheckErrorMessages()
        {
#if ENABLE_MATERIALMESHINFO_BOUNDS_CHECKING
            m_BoundsCheckHandle.Complete();

            foreach (Entity e in m_EntitiesWithOutOfBoundsMMI)
            {
                if (!EntityManager.Exists(e))
                    continue;

                var rma = EntityManager.GetSharedComponentManaged<RenderMeshArray>(e);
                var mmi = EntityManager.GetComponentData<MaterialMeshInfo>(e);

                UnityEngine.Object authoring = null;
#if UNITY_EDITOR
                authoring = EntityManager.Debug.GetAuthoringObjectForEntity(e);
#endif
                string entityDebugString = authoring is null
                    ? e.ToString()
                    : authoring.ToString();

                int numMeshes = rma.Meshes?.Length ?? 0;
                int numMaterials = rma.Materials?.Length ?? 0;
                int numMatMeshIndex = rma.MaterialMeshIndices?.Length ?? 0;

                if (mmi.HasMaterialMeshIndexRange)
                {
                    RangeInt matMeshIndexRange = mmi.MaterialMeshIndexRange;

                    if (matMeshIndexRange.end > numMatMeshIndex)
                    {
                        Debug.LogError(
                            $"Entity \"{entityDebugString}\" has an invalid out of bounds MaterialMeshSubmeshes range, and will not render correctly at runtime: {matMeshIndexRange}. Number of MaterialMeshIndex in RenderMeshArray: {numMatMeshIndex}.",
                            authoring);
                    }
                }
                else
                {
                    bool meshValid = mmi.IsRuntimeMesh || mmi.MeshArrayIndex < numMeshes;
                    bool materialValid = mmi.IsRuntimeMaterial || mmi.MaterialArrayIndex < numMaterials;

                    string meshMsg;
                    string materialMsg;

                    if (meshValid)
                    {
                        if (mmi.IsRuntimeMesh)
                        {
                            meshMsg = $"MeshID: {mmi.Mesh} (runtime registered)";
                        }
                        else
                        {
                            Mesh mesh = rma.GetMesh(mmi);
                            meshMsg = $"MeshID: {mmi.Mesh} (array index: {mmi.MeshArrayIndex}, \"{mesh}\")";
                        }
                    }
                    else
                    {
                        meshMsg = $"MeshID: {mmi.Mesh} (invalid out of bounds array index: {mmi.MeshArrayIndex})";
                    }

                    if (materialValid)
                    {
                        if (mmi.IsRuntimeMaterial)
                        {
                            materialMsg = $"MaterialID: {mmi.Material} (runtime registered)";
                        }
                        else
                        {
                            Material material = rma.GetMaterial(mmi);
                            materialMsg = $"MaterialID: {mmi.Material} (array index: {mmi.MaterialArrayIndex}, \"{material}\")";
                        }
                    }
                    else
                    {
                        materialMsg =
                            $"MaterialID: {mmi.Material} (invalid out of bounds array index: {mmi.MaterialArrayIndex})";
                    }

                    Debug.LogError(
                        $"Entity \"{entityDebugString}\" has an invalid out of bounds index to a Mesh or Material, and will not render correctly at runtime. {meshMsg}. Number of Meshes in RenderMeshArray: {numMeshes}. {materialMsg}. Number of Materials in RenderMeshArray: {numMaterials}.",
                        authoring);
                }
            }
#endif
        }
    }

    /// <summary>
    /// Renders all entities that contain both RenderMesh and LocalToWorld components.
    /// </summary>
    //@TODO: Updating always necessary due to empty component group. When Component group and archetype chunks are unified, [RequireMatchingQueriesForUpdate] can be added.
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(UpdatePresentationSystemGroup))]
    [BurstCompile]
    public unsafe partial class EntitiesGraphicsSystem : SystemBase
    {
        /// <summary>
        /// Toggles the activation of EntitiesGraphicsSystem.
        /// </summary>
        /// <remarks>
        /// To disable this system, use the HYBRID_RENDERER_DISABLED define.
        /// </remarks>
#if HYBRID_RENDERER_DISABLED
        public static bool EntitiesGraphicsEnabled => false;
#else
        public static bool EntitiesGraphicsEnabled => EntitiesGraphicsUtils.IsEntitiesGraphicsSupportedOnSystem();
#endif

#if !DISABLE_HYBRID_RENDERER_ERROR_LOADING_SHADER
        private static bool ErrorShaderEnabled => true;
#else
        private static bool ErrorShaderEnabled => false;
#endif

#if UNITY_EDITOR && !DISABLE_HYBRID_RENDERER_ERROR_LOADING_SHADER
        private static bool LoadingShaderEnabled => true;
#else
        private static bool LoadingShaderEnabled => false;
#endif

        private long m_PersistentInstanceDataSize;

        // Store this in a member variable, because culling callback
        // already sees the new value and we want to check against
        // the value that was seen by OnUpdate.
        private uint m_LastSystemVersionAtLastUpdate;

        private EntityQuery m_CullingJobDependencyGroup;
        private EntityQuery m_EntitiesGraphicsRenderedQuery;
        private EntityQuery m_EntitiesGraphicsRenderedQueryRO;
        private EntityQuery m_LodSelectGroup;
        private EntityQuery m_ChangedTransformQuery;
        private EntityQuery m_MetaEntitiesForHybridRenderableChunksQuery;

        const int kInitialMaxBatchCount = 1 * 1024;
        const float kMaxBatchGrowFactor = 2f;
        const int kNumNewChunksPerThread = 1; // TODO: Tune this
        const int kNumScatteredIndicesPerThread = 8; // TODO: Tune this

        const int kMaxChunkMetadata = 1 * 1024 * 1024;
        const ulong kMaxGPUAllocatorMemory = 1024 * 1024 * 1024; // 1GiB of potential memory space
        const long kGPUBufferSizeInitial = 32 * 1024 * 1024;
        const long kGPUBufferSizeMax = 1023 * 1024 * 1024;
        const int kGPUUploaderChunkSize = 4 * 1024 * 1024;

        private JobHandle m_CullingJobDependency;
        private JobHandle m_CullingJobReleaseDependency;
        private JobHandle m_UpdateJobDependency;
        private JobHandle m_LODDependency;
        private JobHandle m_ReleaseDependency;
        private BatchRendererGroup m_BatchRendererGroup;
        private ThreadedBatchContext m_ThreadedBatchContext;

        private GraphicsBuffer m_GPUPersistentInstanceData;
        private GraphicsBufferHandle m_GPUPersistentInstanceBufferHandle;
        private SparseUploader m_GPUUploader;
        private ThreadedSparseUploader m_ThreadedGPUUploader;
        private HeapAllocator m_GPUPersistentAllocator;
        private HeapBlock m_SharedZeroAllocation;

        private HeapAllocator m_ChunkMetadataAllocator;

        private NativeList<BatchInfo> m_BatchInfos;
        private NativeArray<ChunkProperty> m_ChunkProperties;
        private NativeParallelHashSet<int> m_ExistingBatchIndices;
        private ComponentTypeCache m_ComponentTypeCache;

        private SortedSet<int> m_SortedBatchIds;

        private NativeList<ValueBlitDescriptor> m_ValueBlits;

        // These arrays are parallel and allocated up to kMaxBatchCount. They are indexed by batch indices.
        NativeList<byte> m_ForceLowLOD;

        public BatchRendererGroup BatchRendererGroup => m_BatchRendererGroup;

#if UNITY_EDITOR
        float m_CamMoveDistance;
#endif

#if UNITY_EDITOR
        private EntitiesGraphicsPerThreadStats* m_PerThreadStats = null;
        private EntitiesGraphicsStats m_Stats;
        private BatchCullingOutputDebugData m_BatchCullingOutputDebugData;
        public EntitiesGraphicsStats Stats => m_Stats;
        public BatchCullingOutputDebugData BatchCullingOutputDebugData => m_BatchCullingOutputDebugData;
        public bool CollectBatchCullingOutputDebugData { get; set; }
        private void ComputeStats()
        {
            Profiler.BeginSample("ComputeStats");

#if UNITY_2022_2_14F1_OR_NEWER
            int maxThreadCount = JobsUtility.ThreadIndexCount;
#else
            int maxThreadCount = JobsUtility.MaxJobThreadCount;
#endif

            var result = default(EntitiesGraphicsStats);
            for (int i = 0; i < maxThreadCount; ++i)
            {
                ref var s = ref m_PerThreadStats[i];

                result.ChunkTotal                   += s.ChunkTotal;
                result.ChunkCountAnyLod             += s.ChunkCountAnyLod;
                result.ChunkCountInstancesProcessed += s.ChunkCountInstancesProcessed;
                result.ChunkCountFullyIn            += s.ChunkCountFullyIn;
                result.InstanceTests                += s.InstanceTests;
                result.LodTotal                     += s.LodTotal;
                result.LodNoRequirements            += s.LodNoRequirements;
                result.LodChanged                   += s.LodChanged;
                result.LodChunksTested              += s.LodChunksTested;

                result.RenderedInstanceCount        += s.RenderedEntityCount;
                result.DrawCommandCount             += s.DrawCommandCount;
                result.DrawRangeCount               += s.DrawRangeCount;
            }

            result.CameraMoveDistance = m_CamMoveDistance;

            result.BatchCount = m_ExistingBatchIndices.Count();

            var uploaderStats = m_GPUUploader.ComputeStats();
            result.BytesGPUMemoryUsed = m_PersistentInstanceDataSize + uploaderStats.BytesGPUMemoryUsed;
            result.BytesGPUMemoryUploadedCurr = uploaderStats.BytesGPUMemoryUploadedCurr;
            result.BytesGPUMemoryUploadedMax = uploaderStats.BytesGPUMemoryUploadedMax;

            m_Stats = result;

            Profiler.EndSample();
        }

#if URP_10_0_0_OR_NEWER
        private void ValidateUsingURPForwardPlus()
        {
            // If using URP, display and warning indicating that Forward+ is the preferred rendering mode
            RenderPipelineAsset pipelineAsset = GraphicsSettings.renderPipelineAsset;
            if (pipelineAsset is UniversalRenderPipelineAsset)
            {
                UniversalRenderPipelineAsset settings = pipelineAsset as UniversalRenderPipelineAsset;
                var rendererDataListField = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", BindingFlags.NonPublic | BindingFlags.Instance);
                var defaultRendererIndexField = typeof(UniversalRenderPipelineAsset).GetField("m_DefaultRendererIndex", BindingFlags.NonPublic | BindingFlags.Instance);
                if (rendererDataListField != null && defaultRendererIndexField != null)
                {
                    ScriptableRendererData[] rendererDatas = rendererDataListField.GetValue(settings) as ScriptableRendererData[];
                    int defaultRendererDataIndex = (int)defaultRendererIndexField.GetValue(settings);
                    UniversalRendererData universalRendererData = rendererDatas[defaultRendererDataIndex] as UniversalRendererData;
                    var renderingModeField = typeof(UniversalRendererData).GetField("m_RenderingMode", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (renderingModeField != null && universalRendererData != null)
                    {
                        RenderingMode renderingMode = (RenderingMode)renderingModeField.GetValue(universalRendererData);
                        if (renderingMode != RenderingMode.ForwardPlus)
                        {
                            Debug.LogWarning("Entities.Graphics should be used with URP Forward+. Change Rendering Path on " + universalRendererData.name + " for best compatibility.");
                        }
                    }
                }
            }
        }
#endif
#endif

        private bool m_ResetLod;

        LODGroupExtensions.LODParams m_PrevLODParams;
        float3 m_PrevCameraPos;
        float m_PrevLodDistanceScale;

        NativeParallelMultiHashMap<int, MaterialPropertyType> m_NameIDToMaterialProperties;
        NativeParallelHashMap<int, MaterialPropertyType> m_TypeIndexToMaterialProperty;

        static Dictionary<Type, NamedPropertyMapping> s_TypeToPropertyMappings = new Dictionary<Type, NamedPropertyMapping>();

#if DEBUG_PROPERTY_NAMES
        internal static Dictionary<int, string> s_NameIDToName = new Dictionary<int, string>();
        internal static Dictionary<int, string> s_TypeIndexToName = new Dictionary<int, string>();
#endif

#if USE_UNITY_OCCLUSION
        internal OcclusionCulling OcclusionCulling { get; private set; }
#endif

        private bool m_FirstFrameAfterInit;

        private EntitiesGraphicsArchetypes m_GraphicsArchetypes;

        // Burst accessible filter settings for each RenderFilterSettings shared component index
        private NativeParallelHashMap<FilterSettingsIndex, BatchFilterSettings> m_FilterSettings;

#if ENABLE_PICKING
        Material m_PickingMaterial;
#endif

        Material m_LoadingMaterial;
        Material m_ErrorMaterial;

        // Reuse Lists used for GetAllUniqueSharedComponentData to avoid GC allocs every frame
        private List<RenderFilterSettings> m_RenderFilterSettings = new List<RenderFilterSettings>();
        private List<int> m_SharedComponentIndices = new List<int>();
#if UNITY_EDITOR
        private List<EditorRenderData> m_EditorRenderDatas = new List<EditorRenderData>();
        private List<int> m_EditorRenderSharedComponentIndices = new List<int>();
#endif

        private ThreadLocalAllocator m_ThreadLocalAllocators;

        /// <summary>
        /// Called when this system is created.
        /// </summary>
        protected override void OnCreate()
        {
            // If -nographics is enabled, or if there is no compute shader support, disable HR.
            if (!EntitiesGraphicsEnabled)
            {
                Enabled = false;
                Debug.Log("No SRP present, no compute shader support, or running with -nographics. Entities Graphics package disabled");
                return;
            }


#if URP_10_0_0_OR_NEWER && UNITY_EDITOR
            ValidateUsingURPForwardPlus();
#endif

            m_FirstFrameAfterInit = true;

            m_PersistentInstanceDataSize = kGPUBufferSizeInitial;

            //@TODO: Support SetFilter with EntityQueryDesc syntax
            // This component group must include all types that are being used by the culling job
            m_CullingJobDependencyGroup = GetEntityQuery(
                ComponentType.ChunkComponentReadOnly<ChunkWorldRenderBounds>(),
                ComponentType.ReadOnly<LODRange>(),
                ComponentType.ReadOnly<LODWorldReferencePoint>(),
                ComponentType.ReadOnly<WorldRenderBounds>(),
                ComponentType.ReadOnly<ChunkHeader>(),
                ComponentType.ChunkComponentReadOnly<EntitiesGraphicsChunkInfo>()
            );

            m_EntitiesGraphicsRenderedQuery = GetEntityQuery(EntitiesGraphicsUtils.GetEntitiesGraphicsRenderedQueryDesc());
            m_EntitiesGraphicsRenderedQueryRO = GetEntityQuery(EntitiesGraphicsUtils.GetEntitiesGraphicsRenderedQueryDescReadOnly());

            m_LodSelectGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadWrite<EntitiesGraphicsChunkInfo>(),
                    ComponentType.ReadOnly<ChunkHeader>()
                },
            });

            m_ChangedTransformQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<LocalToWorld>(),
                    ComponentType.ChunkComponent<EntitiesGraphicsChunkInfo>(),
                },
            });
            m_ChangedTransformQuery.AddChangedVersionFilter(ComponentType.ReadOnly<LocalToWorld>());
            m_ChangedTransformQuery.AddOrderVersionFilter();

            m_BatchRendererGroup = new BatchRendererGroup(this.OnPerformCulling, IntPtr.Zero);
            // Hybrid Renderer supports all view types
            m_BatchRendererGroup.SetEnabledViewTypes(new BatchCullingViewType[]
            {
                BatchCullingViewType.Camera,
                BatchCullingViewType.Light,
                BatchCullingViewType.Picking,
                BatchCullingViewType.SelectionOutline
            });
            m_ThreadedBatchContext = m_BatchRendererGroup.GetThreadedBatchContext();
            m_ForceLowLOD = NewNativeListResized<byte>(kInitialMaxBatchCount, Allocator.Persistent, NativeArrayOptions.ClearMemory);

            m_ResetLod = true;

            m_GPUPersistentAllocator = new HeapAllocator(kMaxGPUAllocatorMemory, 16);
            m_ChunkMetadataAllocator = new HeapAllocator(kMaxChunkMetadata);

            m_BatchInfos = NewNativeListResized<BatchInfo>(kInitialMaxBatchCount, Allocator.Persistent);
            m_ChunkProperties = new NativeArray<ChunkProperty>(kMaxChunkMetadata, Allocator.Persistent);
            m_ExistingBatchIndices = new NativeParallelHashSet<int>(128, Allocator.Persistent);
            m_ComponentTypeCache = new ComponentTypeCache(128);

            m_ValueBlits = new NativeList<ValueBlitDescriptor>(Allocator.Persistent);

            // Globally allocate a single zero matrix at offset zero, so loads from zero return zero
            m_SharedZeroAllocation = m_GPUPersistentAllocator.Allocate((ulong)sizeof(float4x4));
            Assert.IsTrue(!m_SharedZeroAllocation.Empty, "Allocation of constant-zero data failed");
            // Make sure the global zero is actually zero.
            m_ValueBlits.Add(new ValueBlitDescriptor
            {
                Value = float4x4.zero,
                DestinationOffset = (uint)m_SharedZeroAllocation.begin,
                ValueSizeBytes = (uint)sizeof(float4x4),
                Count = 1,
            });
            Assert.IsTrue(m_SharedZeroAllocation.begin == 0, "Global zero allocation should have zero address");

            ResetIds();

            m_MetaEntitiesForHybridRenderableChunksQuery = GetEntityQuery(
                new EntityQueryDesc
                {
                    All = new[]
                    {
                        ComponentType.ReadWrite<EntitiesGraphicsChunkInfo>(),
                        ComponentType.ReadOnly<ChunkHeader>(),
                    },
                });

#if UNITY_EDITOR
#if UNITY_2022_2_14F1_OR_NEWER
            int maxThreadCount = JobsUtility.ThreadIndexCount;
#else
            int maxThreadCount = JobsUtility.MaxJobThreadCount;
#endif

            m_PerThreadStats = (EntitiesGraphicsPerThreadStats*)Memory.Unmanaged.Allocate(maxThreadCount * sizeof(EntitiesGraphicsPerThreadStats),
                64, Allocator.Persistent);
#endif

            // Collect all components with [MaterialProperty] attribute
            m_NameIDToMaterialProperties = new NativeParallelMultiHashMap<int, MaterialPropertyType>(256, Allocator.Persistent);
            m_TypeIndexToMaterialProperty = new NativeParallelHashMap<int, MaterialPropertyType>(256, Allocator.Persistent);

            m_GraphicsArchetypes = new EntitiesGraphicsArchetypes(256);

            m_FilterSettings = new NativeParallelHashMap<FilterSettingsIndex, BatchFilterSettings>(256, Allocator.Persistent);

            // Some hardcoded mappings to avoid dependencies to Hybrid from DOTS
#if SRP_10_0_0_OR_NEWER
            RegisterMaterialPropertyType<LocalToWorld>("unity_ObjectToWorld", 4 * 4 * 3);
            RegisterMaterialPropertyType<WorldToLocal_Tag>("unity_WorldToObject", overrideTypeSizeGPU: 4 * 4 * 3);
#else
            RegisterMaterialPropertyType<LocalToWorld>("unity_ObjectToWorld", 4 * 4 * 4);
            RegisterMaterialPropertyType<WorldToLocal_Tag>("unity_WorldToObject", 4 * 4 * 4);
#endif

#if ENABLE_PICKING
            RegisterMaterialPropertyType(typeof(Entity), "unity_EntityId");
#endif

            foreach (var typeInfo in TypeManager.AllTypes)
            {
                var type = typeInfo.Type;

                bool isComponent = typeof(IComponentData).IsAssignableFrom(type);
                if (isComponent)
                {
                    var attributes = type.GetCustomAttributes(typeof(MaterialPropertyAttribute), false);
                    if (attributes.Length > 0)
                    {
                        var propertyAttr = (MaterialPropertyAttribute)attributes[0];

                        RegisterMaterialPropertyType(type, propertyAttr.Name, propertyAttr.OverrideSizeGPU);
                    }
                }
            }

            m_GPUPersistentInstanceData = new GraphicsBuffer(
                GraphicsBuffer.Target.Raw,
                GraphicsBuffer.UsageFlags.None,
                (int)m_PersistentInstanceDataSize / 4,
                4);

            m_GPUPersistentInstanceBufferHandle = m_GPUPersistentInstanceData.bufferHandle;

            m_GPUUploader = new SparseUploader(m_GPUPersistentInstanceData, kGPUUploaderChunkSize);

#if USE_UNITY_OCCLUSION
            OcclusionCulling = new OcclusionCulling();
            OcclusionCulling.Create(EntityManager);
#endif

            m_ThreadLocalAllocators = new ThreadLocalAllocator(-1);

            if (ErrorShaderEnabled)
            {
                m_ErrorMaterial = EntitiesGraphicsUtils.LoadErrorMaterial();
                if (m_ErrorMaterial != null)
                {
                    m_BatchRendererGroup.SetErrorMaterial(m_ErrorMaterial);
                }
            }

            if (LoadingShaderEnabled)
            {
                m_LoadingMaterial = EntitiesGraphicsUtils.LoadLoadingMaterial();
                if (m_LoadingMaterial != null)
                {
                    m_BatchRendererGroup.SetLoadingMaterial(m_LoadingMaterial);
                }
            }

#if ENABLE_PICKING
            m_PickingMaterial = EntitiesGraphicsUtils.LoadPickingMaterial();
            if (m_PickingMaterial != null)
            {
                m_BatchRendererGroup.SetPickingMaterial(m_PickingMaterial);
            }
#endif
#if UNITY_EDITOR
            m_BatchCullingOutputDebugData = new BatchCullingOutputDebugData(128);
#endif
        }

        internal static readonly bool UseConstantBuffers = EntitiesGraphicsUtils.UseHybridConstantBufferMode();
        internal static readonly int MaxBytesPerCBuffer = EntitiesGraphicsUtils.MaxBytesPerCBuffer;
        internal static readonly uint BatchAllocationAlignment = (uint)EntitiesGraphicsUtils.BatchAllocationAlignment;

        internal const int kMaxBytesPerBatchRawBuffer = 16 * 1024 * 1024;

        /// <summary>
        /// The maximum GPU buffer size (in bytes) that a batch can access.
        /// </summary>
        public static int MaxBytesPerBatch => UseConstantBuffers
            ? MaxBytesPerCBuffer
            : kMaxBytesPerBatchRawBuffer;

        /// <summary>
        /// Registers a material property type with the given name.
        /// </summary>
        /// <param name="type">The type of material property to register.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="overrideTypeSizeGPU">An optional size of the type on the GPU.</param>
        public static void RegisterMaterialPropertyType(Type type, string propertyName, short overrideTypeSizeGPU = -1)
        {
            Assert.IsTrue(type != null, "type must be non-null");
            Assert.IsTrue(!string.IsNullOrEmpty(propertyName), "Property name must be valid");

            short typeSizeCPU = (short)UnsafeUtility.SizeOf(type);
            if (overrideTypeSizeGPU == -1)
                overrideTypeSizeGPU = typeSizeCPU;

            // For now, we only support overriding one material property with one type.
            // Several types can override one property, but not the other way around.
            // If necessary, this restriction can be lifted in the future.
            if (s_TypeToPropertyMappings.ContainsKey(type))
            {
                string prevPropertyName = s_TypeToPropertyMappings[type].Name;
                Assert.IsTrue(propertyName.Equals(prevPropertyName),
                    $"Attempted to register type {type.Name} with multiple different property names. Registered with \"{propertyName}\", previously registered with \"{prevPropertyName}\".");
            }
            else
            {
                var pm = new NamedPropertyMapping();
                pm.Name = propertyName;
                pm.SizeCPU = typeSizeCPU;
                pm.SizeGPU = overrideTypeSizeGPU;
                s_TypeToPropertyMappings[type] = pm;
            }
        }

        /// <summary>
        /// A templated version of the material type registration method.
        /// </summary>
        /// <typeparam name="T">The type of material property to register.</typeparam>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="overrideTypeSizeGPU">An optional size of the type on the GPU.</param>
        public static void RegisterMaterialPropertyType<T>(string propertyName, short overrideTypeSizeGPU = -1)
            where T : IComponentData
        {
            RegisterMaterialPropertyType(typeof(T), propertyName, overrideTypeSizeGPU);
        }

        private void InitializeMaterialProperties()
        {
            m_NameIDToMaterialProperties.Clear();

            foreach (var kv in s_TypeToPropertyMappings)
            {
                Type type = kv.Key;
                string propertyName = kv.Value.Name;

                short sizeBytesCPU = kv.Value.SizeCPU;
                short sizeBytesGPU = kv.Value.SizeGPU;
                int typeIndex = TypeManager.GetTypeIndex(type);
                int nameID = Shader.PropertyToID(propertyName);

                var materialPropertyType =
                    new MaterialPropertyType
                    {
                        TypeIndex = typeIndex,
                        NameID = nameID,
                        SizeBytesCPU = sizeBytesCPU,
                        SizeBytesGPU = sizeBytesGPU,
                    };

                m_TypeIndexToMaterialProperty.Add(typeIndex, materialPropertyType);
                m_NameIDToMaterialProperties.Add(nameID, materialPropertyType);

#if DEBUG_PROPERTY_NAMES
                s_TypeIndexToName[typeIndex] = type.Name;
                s_NameIDToName[nameID] = propertyName;
#endif

#if DEBUG_LOG_MATERIAL_PROPERTY_TYPES
                Debug.Log($"Type \"{type.Name}\" ({sizeBytesCPU} bytes) overrides material property \"{propertyName}\" (nameID: {nameID}, typeIndex: {typeIndex})");
#endif

                // We cache all IComponentData types that we know are capable of overriding properties
                m_ComponentTypeCache.UseType(typeIndex);
            }
        }

        /// <summary>
        /// Called when this system is destroyed.
        /// </summary>
        protected override void OnDestroy()
        {
            if (!EntitiesGraphicsEnabled) return;
            CompleteJobs(true);
            Dispose();
        }

        private JobHandle UpdateEntitiesGraphicsBatches(JobHandle inputDependencies)
        {
            JobHandle done = default;
            Profiler.BeginSample("UpdateAllBatches");
            if (!m_EntitiesGraphicsRenderedQuery.IsEmptyIgnoreFilter)
            {
                done = UpdateAllBatches(inputDependencies);
            }

            Profiler.EndSample();

            return done;
        }

        private void OnFirstFrame()
        {
            InitializeMaterialProperties();

#if DEBUG_LOG_HYBRID_RENDERER
            var mode = UseConstantBuffers
                ? $"UBO mode (UBO max size: {MaxBytesPerCBuffer}, alignment: {BatchAllocationAlignment}, globals: {m_GlobalWindowSize})"
                : "SSBO mode";
            Debug.Log(
                $"Entities Graphics active, MaterialProperty component type count {m_ComponentTypeCache.UsedTypeCount} / {ComponentTypeCache.BurstCompatibleTypeArray.kMaxTypes}, {mode}");
#endif
        }

        private JobHandle UpdateFilterSettings(JobHandle inputDeps)
        {
            m_RenderFilterSettings.Clear();
            m_SharedComponentIndices.Clear();

            // TODO: Maybe this could be partially jobified?
            EntityManager.GetAllUniqueSharedComponentsManaged(m_RenderFilterSettings, m_SharedComponentIndices);

            m_FilterSettings.Clear();
            for (int i = 0; i < m_SharedComponentIndices.Count; ++i)
            {
                int sharedIndex = m_SharedComponentIndices[i];
                m_FilterSettings[new FilterSettingsIndex(sharedIndex)] = MakeFilterSettings(m_RenderFilterSettings[i]);
            }

#if UNITY_EDITOR
            m_EditorRenderDatas.Clear();
            m_EditorRenderSharedComponentIndices.Clear();

            EntityManager.GetAllUniqueSharedComponentsManaged(m_EditorRenderDatas, m_EditorRenderSharedComponentIndices);
            for (int i = 0; i < m_SharedComponentIndices.Count; ++i)
            {
                int sharedIndex = m_SharedComponentIndices[i];
                var filterSettings = MakeFilterSettings(m_RenderFilterSettings[i]);
                for (int j = 0; j < m_EditorRenderSharedComponentIndices.Count; j++) {
                    int editorIndex = m_EditorRenderSharedComponentIndices[j];
                    filterSettings.sceneCullingMask = m_EditorRenderDatas[j].SceneCullingMask;
                    m_FilterSettings[new FilterSettingsIndex(sharedIndex, editorIndex)] = filterSettings;
                }
            }

            m_EditorRenderDatas.Clear();
            m_EditorRenderSharedComponentIndices.Clear();
#endif

            m_RenderFilterSettings.Clear();
            m_SharedComponentIndices.Clear();

            return new JobHandle();
        }

        private static BatchFilterSettings MakeFilterSettings(RenderFilterSettings filterSettings)
        {
            return new BatchFilterSettings
            {
                layer = (byte) filterSettings.Layer,
                renderingLayerMask = filterSettings.RenderingLayerMask,
                motionMode = filterSettings.MotionMode,
                shadowCastingMode = filterSettings.ShadowCastingMode,
                receiveShadows = filterSettings.ReceiveShadows,
                staticShadowCaster = filterSettings.StaticShadowCaster,
                allDepthSorted = false, // set by culling
            };
        }

        /// <summary>
        /// Called when this system is updated.
        /// </summary>
        protected override void OnUpdate()
        {
            JobHandle inputDeps = Dependency;

            // Make sure any release jobs that have stored pointers in temp allocated
            // memory have finished before we rewind
            m_CullingJobReleaseDependency.Complete();
            m_CullingJobReleaseDependency = default;
            m_ReleaseDependency.Complete();
            m_ReleaseDependency = default;
            m_ThreadLocalAllocators.Rewind();

            m_LastSystemVersionAtLastUpdate = LastSystemVersion;

            if (m_FirstFrameAfterInit)
            {
                OnFirstFrame();
                m_FirstFrameAfterInit = false;
            }

            Profiler.BeginSample("CompleteJobs");
            inputDeps.Complete(); // #todo
            CompleteJobs();
            ResetLod();
            Profiler.EndSample();

#if UNITY_EDITOR
            ComputeStats();
#endif

            Profiler.BeginSample("UpdateFilterSettings");
            var updateFilterSettingsHandle = UpdateFilterSettings(inputDeps);
            Profiler.EndSample();

            inputDeps = JobHandle.CombineDependencies(inputDeps, updateFilterSettingsHandle);

            var done = new JobHandle();
            try
            {
                Profiler.BeginSample("UpdateEntitiesGraphicsBatches");
                done = UpdateEntitiesGraphicsBatches(inputDeps);
                Profiler.EndSample();

                Profiler.BeginSample("EndUpdate");
                EndUpdate();
                Profiler.EndSample();
            }
            finally
            {
                m_GPUUploader.FrameCleanup();
            }

            EntitiesGraphicsEditorTools.EndFrame();

            Dependency = done;
        }

        private void ResetIds()
        {
            m_SortedBatchIds = new SortedSet<int>();
            m_ExistingBatchIndices.Clear();
        }

        private void EnsureHaveSpaceForNewBatch()
        {
            int currentCapacity = m_BatchInfos.Length;
            int neededCapacity = BatchIndexRange;

            if (currentCapacity >= neededCapacity) return;

            Assert.IsTrue(kMaxBatchGrowFactor >= 1f,
                "Grow factor should always be greater or equal to 1");

            var newCapacity = (int)(kMaxBatchGrowFactor * neededCapacity);

            m_ForceLowLOD.Resize(newCapacity, NativeArrayOptions.ClearMemory);
            m_BatchInfos.Resize(newCapacity, NativeArrayOptions.ClearMemory);
        }

        private void AddBatchIndex(int id)
        {
            Assert.IsTrue(!m_SortedBatchIds.Contains(id), "New batch ID already marked as used");
            m_SortedBatchIds.Add(id);
            m_ExistingBatchIndices.Add(id);
            EnsureHaveSpaceForNewBatch();
        }

        private void RemoveBatchIndex(int id)
        {
            if (!m_SortedBatchIds.Contains(id))
                Assert.IsTrue(false, $"Attempted to release an unused id {id}");
            m_SortedBatchIds.Remove(id);
            m_ExistingBatchIndices.Remove(id);
        }

        private int BatchIndexRange => m_SortedBatchIds.Max + 1;

        private void Dispose()
        {
            m_GPUUploader.Dispose();
            m_GPUPersistentInstanceData.Dispose();

#if UNITY_EDITOR
            Memory.Unmanaged.Free(m_PerThreadStats, Allocator.Persistent);
            m_PerThreadStats = null;
#endif

            m_BatchRendererGroup.Dispose();
            m_ThreadedBatchContext.batchRendererGroup = IntPtr.Zero;

            if (ErrorShaderEnabled)
                Material.DestroyImmediate(m_ErrorMaterial);

            if (LoadingShaderEnabled)
                Material.DestroyImmediate(m_LoadingMaterial);

#if ENABLE_PICKING
            Material.DestroyImmediate(m_PickingMaterial);
#endif

            m_ForceLowLOD.Dispose();
            m_ResetLod = true;
            m_NameIDToMaterialProperties.Dispose();
            m_TypeIndexToMaterialProperty.Dispose();
            m_GPUPersistentAllocator.Dispose();
            m_ChunkMetadataAllocator.Dispose();

            m_BatchInfos.Dispose();
            m_ChunkProperties.Dispose();
            m_ExistingBatchIndices.Dispose();
            m_ValueBlits.Dispose();
            m_ComponentTypeCache.Dispose();

            m_SortedBatchIds = null;

#if USE_UNITY_OCCLUSION
            OcclusionCulling.Dispose();
#endif

            m_GraphicsArchetypes.Dispose();

            m_FilterSettings.Dispose();
            m_CullingJobReleaseDependency.Complete();
            m_ReleaseDependency.Complete();
            m_ThreadLocalAllocators.Dispose();
        }

        private void ResetLod()
        {
            m_PrevLODParams = new LODGroupExtensions.LODParams();
            m_ResetLod = true;
        }

        // This function does only return a meaningful IncludeExcludeListFilter object when called from a BRG culling callback.
        static IncludeExcludeListFilter GetPickingIncludeExcludeListFilterForCurrentCullingCallback(EntityManager entityManager, in BatchCullingContext cullingContext)
        {
#if ENABLE_PICKING && !DISABLE_INCLUDE_EXCLUDE_LIST_FILTERING
            PickingIncludeExcludeList includeExcludeList = default;

            if (cullingContext.viewType == BatchCullingViewType.Picking)
            {
                includeExcludeList = HandleUtility.GetPickingIncludeExcludeList(Allocator.Temp);
            }
            else if (cullingContext.viewType == BatchCullingViewType.SelectionOutline)
            {
                includeExcludeList = HandleUtility.GetSelectionOutlineIncludeExcludeList(Allocator.Temp);
            }

            NativeArray<int> emptyArray = new NativeArray<int>(0, Allocator.Temp);

            NativeArray<int> includeEntityIndices = includeExcludeList.IncludeEntities;
            if (cullingContext.viewType == BatchCullingViewType.SelectionOutline)
            {
                // Make sure the include list for the selection outline is never null even if there is nothing in it.
                // Null NativeArray and empty NativeArray are treated as different things when used to construct an IncludeExcludeListFilter object:
                // - Null include list means that nothing is discarded because the filtering is skipped.
                // - Empty include list means that everything is discarded because the filtering is enabled but never passes.
                // With selection outline culling, we want the filtering to happen in any case even if the array contains nothing so that we don't highlight everything in the latter case.
                if (!includeEntityIndices.IsCreated)
                    includeEntityIndices = emptyArray;
            }
            else if (includeEntityIndices.Length == 0)
            {
                includeEntityIndices = default;
            }

            NativeArray<int> excludeEntityIndices = includeExcludeList.ExcludeEntities;
            if (excludeEntityIndices.Length == 0)
                excludeEntityIndices = default;

            IncludeExcludeListFilter includeExcludeListFilter = new IncludeExcludeListFilter(
                entityManager,
                includeEntityIndices,
                excludeEntityIndices,
                Allocator.TempJob);

            includeExcludeList.Dispose();
            emptyArray.Dispose();

            return includeExcludeListFilter;
#else
            return default;
#endif
        }

        private JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext, BatchCullingOutput cullingOutput, IntPtr userContext)
        {
            Profiler.BeginSample("OnPerformCulling");
#if UNITY_EDITOR
            m_BatchCullingOutputDebugData.materialMeshRefToVisibleCountMap.Clear();           
#endif
            IncludeExcludeListFilter includeExcludeListFilter = GetPickingIncludeExcludeListFilterForCurrentCullingCallback(EntityManager, cullingContext);

            // If inclusive filtering is enabled and we know there are no included entities,
            // we can skip all the work because we know that the result will be nothing.
            if (includeExcludeListFilter.IsIncludeEnabled && includeExcludeListFilter.IsIncludeEmpty)
            {
                includeExcludeListFilter.Dispose();
                return m_CullingJobDependency;
            }

            var lodParams = LODGroupExtensions.CalculateLODParams(cullingContext.lodParameters);

            JobHandle cullingDependency;
            var resetLod = m_ResetLod || (!lodParams.Equals(m_PrevLODParams));
            if (resetLod)
            {
                // Depend on all component ata we access + previous jobs since we are writing to a single
                // m_ChunkInstanceLodEnableds array.
                var lodJobDependency = JobHandle.CombineDependencies(m_CullingJobDependency,
                    m_CullingJobDependencyGroup.GetDependency());

                float cameraMoveDistance = math.length(m_PrevCameraPos - lodParams.cameraPos);
                var lodDistanceScaleChanged = lodParams.distanceScale != m_PrevLodDistanceScale;

#if UNITY_EDITOR
                // Record this separately in the editor for stats display
                m_CamMoveDistance = cameraMoveDistance;
#endif

                var selectLodEnabledJob = new SelectLodEnabled
                {
                    ForceLowLOD = m_ForceLowLOD,
                    LODParams = lodParams,
                    LODRanges = GetComponentTypeHandle<LODRange>(true),
                    LODReferencePoints = GetComponentTypeHandle<LODWorldReferencePoint>(true),
                    EntitiesGraphicsChunkInfo = GetComponentTypeHandle<EntitiesGraphicsChunkInfo>(),
                    ChunkHeader = GetComponentTypeHandle<ChunkHeader>(),
                    CameraMoveDistanceFixed16 =
                        Fixed16CamDistance.FromFloatCeil(cameraMoveDistance * lodParams.distanceScale),
                    DistanceScale = lodParams.distanceScale,
                    DistanceScaleChanged = lodDistanceScaleChanged,
                    MaximumLODLevelMask = 1 << QualitySettings.maximumLODLevel,
#if UNITY_EDITOR
                    Stats = m_PerThreadStats,
#endif
                };

                cullingDependency = m_LODDependency = selectLodEnabledJob.ScheduleParallel(m_LodSelectGroup, lodJobDependency);

                m_PrevLODParams = lodParams;
                m_PrevLodDistanceScale = lodParams.distanceScale;
                m_PrevCameraPos = lodParams.cameraPos;
                m_ResetLod = false;
#if UNITY_EDITOR
#if UNITY_2022_2_14F1_OR_NEWER
                int maxThreadCount = JobsUtility.ThreadIndexCount;
#else
                int maxThreadCount = JobsUtility.MaxJobThreadCount;
#endif
                UnsafeUtility.MemClear(m_PerThreadStats, sizeof(EntitiesGraphicsPerThreadStats) * maxThreadCount);
#endif
            }
            else
            {
                // Depend on all component data we access + previous m_LODDependency job
                cullingDependency = JobHandle.CombineDependencies(
                    m_LODDependency,
                    m_CullingJobDependency,
                    m_CullingJobDependencyGroup.GetDependency());
            }

            var visibilityItems = new IndirectList<ChunkVisibilityItem>(
                m_EntitiesGraphicsRenderedQueryRO.CalculateChunkCountWithoutFiltering(),
                m_ThreadLocalAllocators.GeneralAllocator);

            bool cullLightmapShadowCasters = (cullingContext.cullingFlags & BatchCullingFlags.CullLightmappedShadowCasters) != 0;

            var frustumCullingJob = new FrustumCullingJob
            {
                Splits = CullingSplits.Create(&cullingContext, QualitySettings.shadowProjection, m_ThreadLocalAllocators.GeneralAllocator->Handle),
                CullingViewType = cullingContext.viewType,
                EntitiesGraphicsChunkInfo = GetComponentTypeHandle<EntitiesGraphicsChunkInfo>(true),
                ChunkWorldRenderBounds = GetComponentTypeHandle<ChunkWorldRenderBounds>(true),
                BoundsComponent = GetComponentTypeHandle<WorldRenderBounds>(true),
                EntityHandle = GetEntityTypeHandle(),
                IncludeExcludeListFilter = includeExcludeListFilter,
                VisibilityItems = visibilityItems,
                ThreadLocalAllocator = m_ThreadLocalAllocators,
                CullLightmapShadowCasters = cullLightmapShadowCasters,
                LightMaps = GetSharedComponentTypeHandle<LightMaps>(),
#if UNITY_EDITOR
                Stats = m_PerThreadStats,
#endif
            };

            var frustumCullingJobHandle = frustumCullingJob.ScheduleParallel(m_EntitiesGraphicsRenderedQueryRO, cullingDependency);
            var disposeFrustumCullingHandle = frustumCullingJob.IncludeExcludeListFilter.Dispose(frustumCullingJobHandle);
            DidScheduleCullingJob(frustumCullingJobHandle);

#if USE_UNITY_OCCLUSION
            var occlusionCullingDependency = OcclusionCulling.Cull(EntityManager, cullingContext, m_CullingJobDependency, visibilityItems
#if UNITY_EDITOR
                    , m_PerThreadStats
#endif
                    );
            DidScheduleCullingJob(occlusionCullingDependency);
#endif

            // TODO: Dynamically estimate this based on past frames
            int binCountEstimate = 1;
            var chunkDrawCommandOutput = new ChunkDrawCommandOutput(
                binCountEstimate,
                m_ThreadLocalAllocators,
                cullingOutput);

            // To be able to access the material/mesh IDs, we need access to the registered material/mesh
            // arrays. If we can't get them, then we simply skip in those cases.
            var brgRenderMeshArrays =
                World.GetExistingSystemManaged<RegisterMaterialsAndMeshesSystem>()?.BRGRenderMeshArrays
                ?? new NativeParallelHashMap<int, BRGRenderMeshArray>();

            var emitDrawCommandsJob = new EmitDrawCommandsJob
            {
                VisibilityItems = visibilityItems,
                EntitiesGraphicsChunkInfo = GetComponentTypeHandle<EntitiesGraphicsChunkInfo>(true),
                MaterialMeshInfo = GetComponentTypeHandle<MaterialMeshInfo>(true),
                LocalToWorld = GetComponentTypeHandle<LocalToWorld>(true),
                DepthSorted = GetComponentTypeHandle<DepthSorted_Tag>(true),
                DeformedMeshIndex = GetComponentTypeHandle<DeformedMeshIndex>(true),
                RenderFilterSettings = GetSharedComponentTypeHandle<RenderFilterSettings>(),
                FilterSettings = m_FilterSettings,
                CullingLayerMask = cullingContext.cullingLayerMask,
                LightMaps = GetSharedComponentTypeHandle<LightMaps>(),
                RenderMeshArray = GetSharedComponentTypeHandle<RenderMeshArray>(),
                RendererRangeHandle = GetSharedComponentTypeHandle<RendererRange>(),
                BRGRenderMeshArrays = brgRenderMeshArrays,
#if UNITY_EDITOR
                EditorDataComponentHandle = GetSharedComponentTypeHandle<EditorRenderData>(),
#endif
                DrawCommandOutput = chunkDrawCommandOutput,
                SceneCullingMask = cullingContext.sceneCullingMask,
                CameraPosition = lodParams.cameraPos,
                LastSystemVersion = m_LastSystemVersionAtLastUpdate,

                ProfilerEmitChunk = new ProfilerMarker("EmitChunk"),
            };

            var allocateWorkItemsJob = new AllocateWorkItemsJob
            {
                DrawCommandOutput = chunkDrawCommandOutput,
            };

            var collectWorkItemsJob = new CollectWorkItemsJob
            {
                DrawCommandOutput = chunkDrawCommandOutput,
                ProfileCollect = new ProfilerMarker("Collect"),
                ProfileWrite = new ProfilerMarker("Write"),
            };

            var flushWorkItemsJob = new FlushWorkItemsJob
            {
                DrawCommandOutput = chunkDrawCommandOutput,
            };

            var allocateInstancesJob = new AllocateInstancesJob
            {
                DrawCommandOutput = chunkDrawCommandOutput,
            };

            var allocateDrawCommandsJob = new AllocateDrawCommandsJob
            {
                DrawCommandOutput = chunkDrawCommandOutput
            };

            var expandInstancesJob = new ExpandVisibleInstancesJob
            {
                DrawCommandOutput = chunkDrawCommandOutput,
            };

            var generateDrawCommandsJob = new GenerateDrawCommandsJob
            {
                DrawCommandOutput = chunkDrawCommandOutput,
#if UNITY_EDITOR
                Stats = m_PerThreadStats,
                ViewType = cullingContext.viewType,
#endif
            };

            var generateDrawRangesJob = new GenerateDrawRangesJob
            {
                DrawCommandOutput = chunkDrawCommandOutput,
                FilterSettings = m_FilterSettings,
#if UNITY_EDITOR
                Stats = m_PerThreadStats,
#endif
            };

            var emitDrawCommandsDependency = emitDrawCommandsJob.ScheduleWithIndirectList(visibilityItems, 1, m_CullingJobDependency);

            var collectGlobalBinsDependency =
                chunkDrawCommandOutput.BinCollector.ScheduleFinalize(emitDrawCommandsDependency);
            var sortBinsDependency = DrawBinSort.ScheduleBinSort(
                m_ThreadLocalAllocators.GeneralAllocator,
                chunkDrawCommandOutput.SortedBins,
                chunkDrawCommandOutput.UnsortedBins,
                collectGlobalBinsDependency);

            var allocateWorkItemsDependency = allocateWorkItemsJob.Schedule(collectGlobalBinsDependency);
            var collectWorkItemsDependency = collectWorkItemsJob.ScheduleWithIndirectList(
                chunkDrawCommandOutput.UnsortedBins, 1, allocateWorkItemsDependency);

            var flushWorkItemsDependency =
                flushWorkItemsJob.Schedule(ChunkDrawCommandOutput.NumThreads, 1, collectWorkItemsDependency);

            var allocateInstancesDependency = allocateInstancesJob.Schedule(flushWorkItemsDependency);

            var allocateDrawCommandsDependency = allocateDrawCommandsJob.Schedule(
                JobHandle.CombineDependencies(sortBinsDependency, flushWorkItemsDependency));

            var allocationsDependency = JobHandle.CombineDependencies(
                allocateInstancesDependency,
                allocateDrawCommandsDependency);

            var expandInstancesDependency = expandInstancesJob.ScheduleWithIndirectList(
                chunkDrawCommandOutput.WorkItems,
                1,
                allocateInstancesDependency);
            var generateDrawCommandsDependency = generateDrawCommandsJob.ScheduleWithIndirectList(
                chunkDrawCommandOutput.SortedBins,
                1,
                allocationsDependency);
            var generateDrawRangesDependency = generateDrawRangesJob.Schedule(allocateDrawCommandsDependency);

            var expansionDependency = JobHandle.CombineDependencies(
                expandInstancesDependency,
                generateDrawCommandsDependency,
                generateDrawRangesDependency);

#if DEBUG_VALIDATE_DRAW_COMMAND_SORT
            expansionDependency = new DebugValidateSortJob
            {
                DrawCommandOutput = chunkDrawCommandOutput,
            }.Schedule(expansionDependency);
#endif

#if DEBUG_LOG_DRAW_COMMANDS || DEBUG_LOG_DRAW_COMMANDS_VERBOSE
            DebugDrawCommands(expansionDependency, cullingOutput);
#endif
#if UNITY_EDITOR
            if (CollectBatchCullingOutputDebugData) {
                m_BatchCullingOutputDebugData.FillBatchCullingDebugData(cullingOutput, m_BatchRendererGroup, expansionDependency);
            }
#endif
            m_CullingJobReleaseDependency = JobHandle.CombineDependencies(
                m_CullingJobReleaseDependency,
                disposeFrustumCullingHandle,
                chunkDrawCommandOutput.Dispose(expansionDependency));

            DidScheduleCullingJob(emitDrawCommandsDependency);
            DidScheduleCullingJob(expansionDependency);

            Profiler.EndSample();
            return m_CullingJobDependency;
        }

        private void DebugDrawCommands(JobHandle drawCommandsDependency, BatchCullingOutput cullingOutput)
        {
            drawCommandsDependency.Complete();

            var drawCommands = cullingOutput.drawCommands[0];

            Debug.Log($"Draw Command summary: visibleInstanceCount: {drawCommands.visibleInstanceCount} drawCommandCount: {drawCommands.drawCommandCount} drawRangeCount: {drawCommands.drawRangeCount}");

#if DEBUG_LOG_DRAW_COMMANDS_VERBOSE
            bool verbose = true;
#else
            bool verbose = false;
#endif
            if (verbose)
            {
                for (int i = 0; i < drawCommands.drawCommandCount; ++i)
                {
                    var cmd = drawCommands.drawCommands[i];
                    DrawCommandSettings settings = new DrawCommandSettings
                    {
                        BatchID = cmd.batchID,
                        MaterialID = cmd.materialID,
                        MeshID = cmd.meshID,
                        SubMeshIndex = cmd.submeshIndex,
                        Flags = cmd.flags,
                    };
                    Debug.Log($"Draw Command #{i}: {settings} visibleOffset: {cmd.visibleOffset} visibleCount: {cmd.visibleCount}");
                    StringBuilder sb = new StringBuilder((int)cmd.visibleCount * 30);
                    bool hasSortingPosition = settings.HasSortingPosition;
                    for (int j = 0; j < cmd.visibleCount; ++j)
                    {
                        sb.Append(drawCommands.visibleInstances[cmd.visibleOffset + j]);
                        if (hasSortingPosition)
                            sb.AppendFormat(" ({0:F3} {1:F3} {2:F3})",
                                drawCommands.instanceSortingPositions[cmd.sortingPosition + 0],
                                drawCommands.instanceSortingPositions[cmd.sortingPosition + 1],
                                drawCommands.instanceSortingPositions[cmd.sortingPosition + 2]);
                        sb.Append(", ");
                    }
                    Debug.Log($"Draw Command #{i} instances: [{sb}]");
                }
            }
        }

        private JobHandle UpdateAllBatches(JobHandle inputDependencies)
        {
            Profiler.BeginSample("GetComponentTypes");
#if UNITY_2022_2_14F1_OR_NEWER
            int maxThreadCount = JobsUtility.ThreadIndexCount;
#else
            int maxThreadCount = JobsUtility.MaxJobThreadCount;
#endif


            var threadLocalAABBs = new NativeArray<ThreadLocalAABB>(
                maxThreadCount,
                Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory);
            var zeroAABBJob = new ZeroThreadLocalAABBJob
            {
                ThreadLocalAABBs = threadLocalAABBs,
            }.Schedule(threadLocalAABBs.Length, 16);
            ThreadLocalAABB.AssertCacheLineSize();

            var entitiesGraphicsRenderedChunkType= GetComponentTypeHandle<EntitiesGraphicsChunkInfo>(false);
            var entitiesGraphicsRenderedChunkTypeRO = GetComponentTypeHandle<EntitiesGraphicsChunkInfo>(true);
            var chunkHeadersRO = GetComponentTypeHandle<ChunkHeader>(true);
            var chunkWorldRenderBoundsRO = GetComponentTypeHandle<ChunkWorldRenderBounds>(true);
            var localToWorldsRO = GetComponentTypeHandle<LocalToWorld>(true);
            var lodRangesRO = GetComponentTypeHandle<LODRange>(true);
            var materialMeshInfosRO = GetComponentTypeHandle<MaterialMeshInfo>(true);

            m_ComponentTypeCache.FetchTypeHandles(this);

            Profiler.EndSample();

            var numNewChunksArray = new NativeArray<int>(1, Allocator.TempJob);
            int totalChunks = m_EntitiesGraphicsRenderedQuery.CalculateChunkCountWithoutFiltering();
            var newChunks = new NativeArray<ArchetypeChunk>(
                totalChunks,
                Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory);

            var classifyNewChunksJob = new ClassifyNewChunksJob
                {
                    EntitiesGraphicsChunkInfo = entitiesGraphicsRenderedChunkTypeRO,
                    ChunkHeader = chunkHeadersRO,
                    NumNewChunks = numNewChunksArray,
                    NewChunks = newChunks
                }
                .ScheduleParallel(m_MetaEntitiesForHybridRenderableChunksQuery, inputDependencies);

            JobHandle entitiesGraphicsCompleted = new JobHandle();

            const int kNumBitsPerLong = sizeof(long) * 8;
            var unreferencedBatchIndices = new NativeArray<long>(
                (BatchIndexRange + kNumBitsPerLong) / kNumBitsPerLong,
                Allocator.TempJob,
                NativeArrayOptions.ClearMemory);

            JobHandle initializedUnreferenced = default;
            var existingKeys = m_ExistingBatchIndices.ToNativeArray(Allocator.TempJob);
            initializedUnreferenced = new InitializeUnreferencedIndicesScatterJob
            {
                ExistingBatchIndices = existingKeys,
                UnreferencedBatchIndices = unreferencedBatchIndices,
            }.Schedule(existingKeys.Length, kNumScatteredIndicesPerThread);

            const int kNumDisposeJobHandles = 5;
            int numDisposeJobHandles = 0;
            var disposeJobHandles = new NativeArray<JobHandle>(kNumDisposeJobHandles, Allocator.Temp);
            disposeJobHandles[numDisposeJobHandles++] = existingKeys.Dispose(initializedUnreferenced);

            inputDependencies = JobHandle.CombineDependencies(inputDependencies, initializedUnreferenced, zeroAABBJob);

            // Conservative estimate is that every known type is in every chunk. There will be
            // at most one operation per type per chunk, which will be either an actual
            // chunk data upload, or a default value blit (a single type should not have both).
            int conservativeMaximumGpuUploads = totalChunks * m_ComponentTypeCache.UsedTypeCount;
            var gpuUploadOperations = new NativeArray<GpuUploadOperation>(
                conservativeMaximumGpuUploads,
                Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory);
            var numGpuUploadOperationsArray = new NativeArray<int>(
                1,
                Allocator.TempJob,
                NativeArrayOptions.ClearMemory);

            uint lastSystemVersion = LastSystemVersion;

            if (EntitiesGraphicsEditorTools.DebugSettings.ForceInstanceDataUpload)
            {
                Debug.Log("Reuploading all Entities Graphics instance data to GPU");
                lastSystemVersion = 0;
            }

            classifyNewChunksJob.Complete();
            int numNewChunks = numNewChunksArray[0];

            var maxBatchCount = math.max(kInitialMaxBatchCount, BatchIndexRange + numNewChunks);

            // Integer division with round up
            var maxBatchLongCount = (maxBatchCount + kNumBitsPerLong - 1) / kNumBitsPerLong;

            var entitiesGraphicsChunkUpdater = new EntitiesGraphicsChunkUpdater
            {
                ComponentTypes = m_ComponentTypeCache.ToBurstCompatible(Allocator.TempJob),
                UnreferencedBatchIndices = unreferencedBatchIndices,
                ChunkProperties = m_ChunkProperties,
                LastSystemVersion = lastSystemVersion,

                GpuUploadOperations = gpuUploadOperations,
                NumGpuUploadOperations = numGpuUploadOperationsArray,

                LocalToWorldType = TypeManager.GetTypeIndex<LocalToWorld>(),
                WorldToLocalType = TypeManager.GetTypeIndex<WorldToLocal_Tag>(),
                PrevLocalToWorldType = TypeManager.GetTypeIndex<BuiltinMaterialPropertyUnity_MatrixPreviousM>(),
                PrevWorldToLocalType = TypeManager.GetTypeIndex<BuiltinMaterialPropertyUnity_MatrixPreviousMI_Tag>(),

                ThreadLocalAABBs = threadLocalAABBs,
                ThreadIndex = 0, // set by the job system

#if PROFILE_BURST_JOB_INTERNALS
                ProfileAddUpload = new ProfilerMarker("AddUpload"),
#endif
            };

            var updateOldJob = new UpdateOldEntitiesGraphicsChunksJob
            {
                EntitiesGraphicsChunkInfo = entitiesGraphicsRenderedChunkType,
                ChunkWorldRenderBounds = chunkWorldRenderBoundsRO,
                ChunkHeader = chunkHeadersRO,
                LocalToWorld = localToWorldsRO,
                LodRange = lodRangesRO,
                MaterialMeshInfo = materialMeshInfosRO,
                EntitiesGraphicsChunkUpdater = entitiesGraphicsChunkUpdater,
            };

            JobHandle updateOldDependencies = inputDependencies;

            // We need to wait for the job to complete here so we can process the new chunks
            updateOldJob.ScheduleParallel(m_MetaEntitiesForHybridRenderableChunksQuery, updateOldDependencies).Complete();

            // Garbage collect deleted batches before adding new ones to minimize peak memory use.
            Profiler.BeginSample("GarbageCollectUnreferencedBatches");
            int numRemoved = GarbageCollectUnreferencedBatches(unreferencedBatchIndices);
            Profiler.EndSample();

            if (numNewChunks > 0)
            {
                Profiler.BeginSample("AddNewChunks");
                int numValidNewChunks = AddNewChunks(newChunks.GetSubArray(0, numNewChunks));
                Profiler.EndSample();

                var updateNewChunksJob = new UpdateNewEntitiesGraphicsChunksJob
                {
                    NewChunks = newChunks,
                    EntitiesGraphicsChunkInfo = entitiesGraphicsRenderedChunkTypeRO,
                    ChunkWorldRenderBounds = chunkWorldRenderBoundsRO,
                    EntitiesGraphicsChunkUpdater = entitiesGraphicsChunkUpdater,
                };

#if DEBUG_LOG_INVALID_CHUNKS
                if (numValidNewChunks != numNewChunks)
                    Debug.Log($"Tried to add {numNewChunks} new chunks, but only {numValidNewChunks} were valid, {numNewChunks - numValidNewChunks} were invalid");
#endif

                entitiesGraphicsCompleted = updateNewChunksJob.Schedule(numValidNewChunks, kNumNewChunksPerThread);
            }

            disposeJobHandles[numDisposeJobHandles++] = entitiesGraphicsChunkUpdater.ComponentTypes.Dispose(entitiesGraphicsCompleted);
            disposeJobHandles[numDisposeJobHandles++] = newChunks.Dispose(entitiesGraphicsCompleted);
            disposeJobHandles[numDisposeJobHandles++] = numNewChunksArray.Dispose(entitiesGraphicsCompleted);

            var drawCommandFlagsUpdated = new UpdateDrawCommandFlagsJob
            {
                LocalToWorld = GetComponentTypeHandle<LocalToWorld>(true),
                RenderFilterSettings = GetSharedComponentTypeHandle<RenderFilterSettings>(),
                EntitiesGraphicsChunkInfo = GetComponentTypeHandle<EntitiesGraphicsChunkInfo>(),
                FilterSettings = m_FilterSettings,
                DefaultFilterSettings = MakeFilterSettings(RenderFilterSettings.Default),
#if UNITY_EDITOR
                EditorDataComponentHandle = GetSharedComponentTypeHandle<EditorRenderData>(),
#endif
            }.ScheduleParallel(m_ChangedTransformQuery, entitiesGraphicsCompleted);
            DidScheduleUpdateJob(drawCommandFlagsUpdated);

            // TODO: Need to wait for new chunk updating to complete, so there are no more jobs writing to the bitfields.
            entitiesGraphicsCompleted.Complete();

            int numGpuUploadOperations = numGpuUploadOperationsArray[0];
            Assert.IsTrue(numGpuUploadOperations <= gpuUploadOperations.Length, "Maximum GPU upload operation count exceeded");

            ComputeUploadSizeRequirements(
                numGpuUploadOperations, gpuUploadOperations,
                out int numOperations, out int totalUploadBytes, out int biggestUploadBytes);

#if DEBUG_LOG_UPLOADS
            if (numOperations > 0)
            {
                Debug.Log($"GPU upload operations: {numOperations}, GPU upload bytes: {totalUploadBytes}");
            }
#endif
            Profiler.BeginSample("StartUpdate");
            StartUpdate(numOperations, totalUploadBytes, biggestUploadBytes);
            Profiler.EndSample();

            var uploadsExecuted = new ExecuteGpuUploads
            {
                GpuUploadOperations = gpuUploadOperations,
                ThreadedSparseUploader = m_ThreadedGPUUploader,
            }.Schedule(numGpuUploadOperations, 1);
            numGpuUploadOperationsArray.Dispose();
            disposeJobHandles[numDisposeJobHandles++] = gpuUploadOperations.Dispose(uploadsExecuted);

            Profiler.BeginSample("UploadAllBlits");
            UploadAllBlits();
            Profiler.EndSample();

#if DEBUG_LOG_CHUNK_CHANGES
            if (numNewChunks > 0 || numRemoved > 0)
                Debug.Log($"Chunks changed, new chunks: {numNewChunks}, removed batches: {numRemoved}, batch count: {m_ExistingBatchBatchIndices.Count()}, chunk count: {m_MetaEntitiesForHybridRenderableChunks.CalculateEntityCount()}");
#endif

            Profiler.BeginSample("UpdateGlobalAABB");
            UpdateGlobalAABB(threadLocalAABBs);
            Profiler.EndSample();

            unreferencedBatchIndices.Dispose();

            uploadsExecuted.Complete();

            // Also add a dependency to the dispose jobs to ensure they will always be waited on
            m_ReleaseDependency = JobHandle.CombineDependencies(
                    disposeJobHandles.Slice(0, numDisposeJobHandles));
            JobHandle outputDeps = JobHandle.CombineDependencies(
                uploadsExecuted,
                drawCommandFlagsUpdated,
                m_ReleaseDependency);

            return outputDeps;
        }

        private void UpdateGlobalAABB(NativeArray<ThreadLocalAABB> threadLocalAABBs)
        {
            MinMaxAABB aabb = MinMaxAABB.Empty;

            for (int i = 0; i < threadLocalAABBs.Length; ++i)
                aabb.Encapsulate(threadLocalAABBs[i].AABB);

#if DEBUG_LOG_GLOBAL_AABB
            Debug.Log($"Global AABB min: {aabb.Min} max: {aabb.Max}");
#endif

            var centerExtentsAABB = (AABB) aabb;
            m_BatchRendererGroup.SetGlobalBounds(new Bounds(centerExtentsAABB.Center, centerExtentsAABB.Extents));

            threadLocalAABBs.Dispose();
        }

        private void ComputeUploadSizeRequirements(
            int numGpuUploadOperations, NativeArray<GpuUploadOperation> gpuUploadOperations,
            out int numOperations, out int totalUploadBytes, out int biggestUploadBytes)
        {
            numOperations = numGpuUploadOperations + m_ValueBlits.Length;
            totalUploadBytes = 0;
            biggestUploadBytes = 0;

            for (int i = 0; i < numGpuUploadOperations; ++i)
            {
                var numBytes = gpuUploadOperations[i].BytesRequiredInUploadBuffer;
                totalUploadBytes += numBytes;
                biggestUploadBytes = math.max(biggestUploadBytes, numBytes);
            }

            for (int i = 0; i < m_ValueBlits.Length; ++i)
            {
                var numBytes = m_ValueBlits[i].BytesRequiredInUploadBuffer;
                totalUploadBytes += numBytes;
                biggestUploadBytes = math.max(biggestUploadBytes, numBytes);
            }
        }

        private int GarbageCollectUnreferencedBatches(NativeArray<long> unreferencedBatchIndices)
        {
            int numRemoved = 0;

            int firstInQw = 0;
            for (int i = 0; i < unreferencedBatchIndices.Length; ++i)
            {
                long qw = unreferencedBatchIndices[i];
                while (qw != 0)
                {
                    int setBit = math.tzcnt(qw);
                    long mask = ~(1L << setBit);
                    int batchIndex = firstInQw + setBit;

                    RemoveBatch(batchIndex);
                    ++numRemoved;

                    qw &= mask;
                }

                firstInQw += (int)AtomicHelpers.kNumBitsInLong;
            }

#if DEBUG_LOG_GARBAGE_COLLECTION
            Debug.Log($"GarbageCollectUnreferencedBatches(removed: {numRemoved})");
#endif

            return numRemoved;
        }

        private void RemoveBatch(int batchIndex)
        {
            var batchInfo = m_BatchInfos[batchIndex];
            m_BatchInfos[batchIndex] = default;

#if DEBUG_LOG_BATCH_DELETION
            Debug.Log($"RemoveBatch({batchIndex})");
#endif

            RemoveBatchIndex(batchIndex);

            if (!batchInfo.GPUMemoryAllocation.Empty)
            {
                m_GPUPersistentAllocator.Release(batchInfo.GPUMemoryAllocation);
#if DEBUG_LOG_MEMORY_USAGE
                Debug.Log($"RELEASE; {batchInfo.GPUMemoryAllocation.Length}");
#endif
            }

            var metadataAllocation = batchInfo.ChunkMetadataAllocation;
            if (!metadataAllocation.Empty)
            {
                for (ulong j = metadataAllocation.begin; j < metadataAllocation.end; ++j)
                    m_ChunkProperties[(int)j] = default;

                m_ChunkMetadataAllocator.Release(metadataAllocation);
            }
        }

        static int NumInstancesInChunk(ArchetypeChunk chunk) => chunk.Capacity;

        [BurstCompile]
        static void CreateBatchCreateInfo(
            ref BatchCreateInfoFactory batchCreateInfoFactory,
            ref NativeArray<ArchetypeChunk> newChunks,
            ref NativeArray<BatchCreateInfo> sortedNewChunks,
            out MaterialPropertyType failureProperty
        )
        {
            failureProperty = default;
            failureProperty.TypeIndex = -1;
            for (int i = 0; i < newChunks.Length; ++i)
            {
                sortedNewChunks[i] = batchCreateInfoFactory.Create(newChunks[i], ref failureProperty);
                if (failureProperty.TypeIndex >= 0)
                {
                    return;
                }
            }
            sortedNewChunks.Sort();
        }

        private int AddNewChunks(NativeArray<ArchetypeChunk> newChunks)
        {
            int numValidNewChunks = 0;

            Assert.IsTrue(newChunks.Length > 0, "Attempted to add new chunks, but list of new chunks was empty");

            var batchCreationTypeHandles = new BatchCreationTypeHandles(this);

            // Sort new chunks by RenderMesh so we can put
            // all compatible chunks inside one batch.
            var batchCreateInfoFactory = new BatchCreateInfoFactory
            {
                GraphicsArchetypes = m_GraphicsArchetypes,
                TypeIndexToMaterialProperty = m_TypeIndexToMaterialProperty,
            };

            var sortedNewChunks = new NativeArray<BatchCreateInfo>(newChunks.Length, Allocator.Temp);
            CreateBatchCreateInfo(ref batchCreateInfoFactory, ref newChunks, ref sortedNewChunks, out var failureProperty);
            if (failureProperty.TypeIndex >= 0)
            {
                Assert.IsTrue(false, $"TypeIndex mismatch between key and stored property, Type: {failureProperty.TypeName} ({failureProperty.TypeIndex:x8}), Property: {failureProperty.PropertyName} ({failureProperty.NameID:x8})");
            }

            int batchBegin = 0;
            int numInstances = NumInstancesInChunk(sortedNewChunks[0].Chunk);
            int maxEntitiesPerBatch = m_GraphicsArchetypes
                .GetGraphicsArchetype(sortedNewChunks[0].GraphicsArchetypeIndex)
                .MaxEntitiesPerBatch;

            for (int i = 1; i <= sortedNewChunks.Length; ++i)
            {
                int instancesInChunk = 0;
                bool breakBatch = false;

                if (i < sortedNewChunks.Length)
                {
                    var cur = sortedNewChunks[i];
                    breakBatch = !sortedNewChunks[batchBegin].Equals(cur);
                    instancesInChunk = NumInstancesInChunk(cur.Chunk);
                }
                else
                {
                    breakBatch = true;
                }

                if (numInstances + instancesInChunk > maxEntitiesPerBatch)
                    breakBatch = true;

                if (breakBatch)
                {
                    int numChunks = i - batchBegin;

                    bool valid = AddNewBatch(
                        batchCreationTypeHandles,
                        sortedNewChunks.GetSubArray(batchBegin, numChunks),
                        numInstances);

                    // As soon as we encounter an invalid chunk, we know that all the rest are invalid
                    // too.
                    if (valid)
                        numValidNewChunks += numChunks;
                    else
                        return numValidNewChunks;

                    batchBegin = i;
                    numInstances = instancesInChunk;

                    if (batchBegin < sortedNewChunks.Length)
                        maxEntitiesPerBatch = m_GraphicsArchetypes
                            .GetGraphicsArchetype(sortedNewChunks[batchBegin].GraphicsArchetypeIndex)
                            .MaxEntitiesPerBatch;
                }
                else
                {
                    numInstances += instancesInChunk;
                }
            }

            sortedNewChunks.Dispose();

            return numValidNewChunks;
        }

        private static int NextAlignedBy16(int size)
        {
            return ((size + 15) >> 4) << 4;
        }

        internal static MetadataValue CreateMetadataValue(int nameID, int gpuAddress, bool isOverridden)
        {
            const uint kPerInstanceDataBit = 0x80000000;

            return new MetadataValue
            {
                NameID = nameID,
                Value = (uint) gpuAddress
                        | (isOverridden ? kPerInstanceDataBit : 0),
            };
        }

        private bool AddNewBatch(
            BatchCreationTypeHandles typeHandles,
            NativeArray<BatchCreateInfo> batchChunks,
            int numInstances)
        {
            var graphicsArchetype = m_GraphicsArchetypes.GetGraphicsArchetype(batchChunks[0].GraphicsArchetypeIndex);

            var overrides = graphicsArchetype.PropertyComponents;
            var overrideSizes = new NativeArray<int>(overrides.Length, Allocator.Temp);

            int numProperties = overrides.Length;

            Assert.IsTrue(numProperties > 0, "No overridden properties, expected at least one");
            Assert.IsTrue(numInstances > 0, "No instances, expected at least one");
            Assert.IsTrue(batchChunks.Length > 0, "No chunks, expected at least one");

            int batchSizeBytes = 0;
            // Every chunk has the same graphics archetype, so each requires the same amount
            // of component metadata structs.
            int batchTotalChunkMetadata = numProperties * batchChunks.Length;

            for (int i = 0; i < overrides.Length; ++i)
            {
                // For each component, allocate a contiguous range that's aligned by 16.
                int sizeBytesComponent = NextAlignedBy16(overrides[i].SizeBytesGPU * numInstances);
                overrideSizes[i] = sizeBytesComponent;
                batchSizeBytes += sizeBytesComponent;
            }

            BatchInfo batchInfo = default;

            // If allocations fail, bail out early to avoid rendering artifacts.
            batchInfo.ChunkMetadataAllocation = m_ChunkMetadataAllocator.Allocate((ulong)batchTotalChunkMetadata);
            if (batchInfo.ChunkMetadataAllocation.Empty)
            {
                Assert.IsTrue(false, $"Out of memory in the Entities Graphics chunk metadata buffer. Attempted to allocate {batchTotalChunkMetadata} elements, buffer size: {m_ChunkMetadataAllocator.Size}, free size left: {m_ChunkMetadataAllocator.FreeSpace}.");
                return false;
            }

            batchInfo.GPUMemoryAllocation = m_GPUPersistentAllocator.Allocate((ulong)batchSizeBytes, BatchAllocationAlignment);
            if (batchInfo.GPUMemoryAllocation.Empty)
            {
                Assert.IsTrue(false, $"Out of memory in the Entities Graphics GPU instance data buffer. Attempted to allocate {batchSizeBytes}, buffer size: {m_GPUPersistentAllocator.Size}, free size left: {m_GPUPersistentAllocator.FreeSpace}.");
                return false;
            }

            // Physical offset inside the buffer, always the same on all platforms.
            int allocationBegin = (int)batchInfo.GPUMemoryAllocation.begin;

            // Metadata offset depends on whether a raw buffer or cbuffer is used.
            // Raw buffers index from start of buffer, cbuffers index from start of allocation.
            uint bindOffset = UseConstantBuffers
                ? (uint)allocationBegin
                : 0;
            uint bindWindowSize = UseConstantBuffers
                ? (uint)MaxBytesPerBatch
                : 0;

            // Compute where each individual property SoA stream starts
            var overrideStreamBegin = new NativeArray<int>(overrides.Length, Allocator.Temp);
            overrideStreamBegin[0] = allocationBegin;
            for (int i = 1; i < numProperties; ++i)
                overrideStreamBegin[i] = overrideStreamBegin[i - 1] + overrideSizes[i - 1];

            int numMetadata = numProperties;
            var overrideMetadata = new NativeArray<MetadataValue>(numMetadata, Allocator.Temp);

            int metadataIndex = 0;
            for (int i = 0; i < numProperties; ++i)
            {
                int gpuAddress = overrideStreamBegin[i] - (int)bindOffset;
                overrideMetadata[metadataIndex] = CreateMetadataValue(overrides[i].NameID, gpuAddress, true);
                ++metadataIndex;

#if DEBUG_LOG_PROPERTY_ALLOCATIONS
                Debug.Log($"Property Allocation: Property: {NameIDFormatted(overrides[i].NameID)} Type: {TypeIndexFormatted(overrides[i].TypeIndex)} Metadata: {overrideMetadata[i].Value:x8} Allocation: {overrideStreamBegin[i]}");
#endif
            }

            var batchID = m_ThreadedBatchContext.AddBatch(overrideMetadata, m_GPUPersistentInstanceBufferHandle,
                bindOffset, bindWindowSize);
            int batchIndex = (int)batchID.value;

#if DEBUG_LOG_BATCH_CREATION
            Debug.Log($"Created new batch, ID: {batchIndex}, chunks: {batchChunks.Length}, properties: {numProperties}, instances: {numInstances}, size: {batchSizeBytes}, buffer {m_GPUPersistentInstanceBufferHandle.value} (size {m_GPUPersistentInstanceData.count * m_GPUPersistentInstanceData.stride} bytes)");
#endif
            Assert.IsTrue(batchIndex!=0, "Failed to add new BatchRendererGroup batch.");

            AddBatchIndex(batchIndex);
            m_BatchInfos[batchIndex] = batchInfo;

            // Configure chunk components for each chunk
            var args = new SetBatchChunkDataArgs
            {
                BatchChunks = batchChunks,
                BatchIndex = batchIndex,
                ChunkProperties = m_ChunkProperties,
                EntityManager = EntityManager,
                NumProperties = numProperties,
                TypeHandles = typeHandles,
                ChunkMetadataBegin = (int)batchInfo.ChunkMetadataAllocation.begin,
                ChunkOffsetInBatch = 0,
                OverrideStreamBegin = overrideStreamBegin
            };
            SetBatchChunkData(ref args, ref overrides);

            Assert.IsTrue(args.ChunkOffsetInBatch == numInstances, "Batch instance count mismatch");

            return true;
        }

        struct SetBatchChunkDataArgs
        {
            public int ChunkMetadataBegin;
            public int ChunkOffsetInBatch;
            public NativeArray<BatchCreateInfo> BatchChunks;
            public int BatchIndex;
            public int NumProperties;
            public BatchCreationTypeHandles TypeHandles;
            public EntityManager EntityManager;
            public NativeArray<ChunkProperty> ChunkProperties;
            public NativeArray<int> OverrideStreamBegin;
        }

        [BurstCompile]
        static void SetBatchChunkData(ref SetBatchChunkDataArgs args, ref UnsafeList<ArchetypePropertyOverride> overrides)
        {
            var batchChunks = args.BatchChunks;
            int numProperties = args.NumProperties;
            var overrideStreamBegin = args.OverrideStreamBegin;
            int chunkOffsetInBatch = args.ChunkOffsetInBatch;
            int chunkMetadataBegin = args.ChunkMetadataBegin;
            for (int i = 0; i < batchChunks.Length; ++i)
            {
                var chunk = batchChunks[i].Chunk;
                var entitiesGraphicsChunkInfo = new EntitiesGraphicsChunkInfo
                {
                    Valid = true,
                    BatchIndex = args.BatchIndex,
                    ChunkTypesBegin = chunkMetadataBegin,
                    ChunkTypesEnd = chunkMetadataBegin + numProperties,
                    CullingData = new EntitiesGraphicsChunkCullingData
                    {
                        Flags = ComputeCullingFlags(chunk, args.TypeHandles),
                        InstanceLodEnableds = default,
                        ChunkOffsetInBatch = chunkOffsetInBatch,
                    },
                };

                args.EntityManager.SetChunkComponentData(chunk, entitiesGraphicsChunkInfo);
                for (int j = 0; j < numProperties; ++j)
                {
                    var propertyOverride = overrides[j];
                    var chunkProperty = new ChunkProperty
                    {
                        ComponentTypeIndex = propertyOverride.TypeIndex,
                        GPUDataBegin = overrideStreamBegin[j] + chunkOffsetInBatch * propertyOverride.SizeBytesGPU,
                        ValueSizeBytesCPU = propertyOverride.SizeBytesCPU,
                        ValueSizeBytesGPU = propertyOverride.SizeBytesGPU,
                    };

                    args.ChunkProperties[chunkMetadataBegin + j] = chunkProperty;
                }

                chunkOffsetInBatch += NumInstancesInChunk(chunk);
                chunkMetadataBegin += numProperties;
            }

            args.ChunkOffsetInBatch = chunkOffsetInBatch;
            args.ChunkMetadataBegin = chunkMetadataBegin;
        }

        static byte ComputeCullingFlags(ArchetypeChunk chunk, BatchCreationTypeHandles typeHandles)
        {
            bool hasLodData = chunk.Has(ref typeHandles.LODRange);

            // TODO: Do we need non-per-instance culling anymore? It seems to always be added
            // for converted objects, and doesn't seem to be removed ever, so the only way to
            // not have it is to manually remove it or create entities from scratch.
            bool hasPerInstanceCulling = !hasLodData || chunk.Has(ref typeHandles.PerInstanceCulling);

            byte flags = 0;

            if (hasLodData) flags |= EntitiesGraphicsChunkCullingData.kFlagHasLodData;
            if (hasPerInstanceCulling) flags |= EntitiesGraphicsChunkCullingData.kFlagInstanceCulling;

            return flags;
        }

        private void UploadAllBlits()
        {
            UploadBlitJob uploadJob = new UploadBlitJob()
            {
                BlitList = m_ValueBlits,
                ThreadedSparseUploader = m_ThreadedGPUUploader
            };

            JobHandle handle = uploadJob.Schedule(m_ValueBlits.Length, 1);
            handle.Complete();

            m_ValueBlits.Clear();
        }

        private void CompleteJobs(bool completeEverything = false)
        {
            m_CullingJobDependency.Complete();
            m_CullingJobDependencyGroup.CompleteDependency();
            m_CullingJobReleaseDependency.Complete();
            m_ReleaseDependency.Complete();

            // TODO: This might not be necessary, remove?
            if (completeEverything)
            {
                m_EntitiesGraphicsRenderedQuery.CompleteDependency();
                m_LodSelectGroup.CompleteDependency();
                m_ChangedTransformQuery.CompleteDependency();
            }

            m_UpdateJobDependency.Complete();
            m_UpdateJobDependency = new JobHandle();
        }

        private void DidScheduleCullingJob(JobHandle job)
        {
            m_CullingJobDependency = JobHandle.CombineDependencies(job, m_CullingJobDependency);
            m_CullingJobDependencyGroup.AddDependency(job);
        }

        private void DidScheduleUpdateJob(JobHandle job)
        {
            m_UpdateJobDependency = JobHandle.CombineDependencies(job, m_UpdateJobDependency);
        }

        private void StartUpdate(int numOperations, int totalUploadBytes, int biggestUploadBytes)
        {
            var persistentBytes = m_GPUPersistentAllocator.OnePastHighestUsedAddress;
            if (persistentBytes > (ulong)m_PersistentInstanceDataSize)
            {
                while ((ulong)m_PersistentInstanceDataSize < persistentBytes)
                {
                    m_PersistentInstanceDataSize *= 2;
                }

                if (m_PersistentInstanceDataSize > kGPUBufferSizeMax)
                {
                    m_PersistentInstanceDataSize = kGPUBufferSizeMax; // Some backends fails at loading 1024 MiB, but 1023 is fine... This should ideally be a device cap.
                }

                if(persistentBytes > kGPUBufferSizeMax)
                    Debug.LogError("Entities Graphics: Current loaded scenes need more than 1GiB of persistent GPU memory. This is more than some GPU backends can allocate. Try to reduce amount of loaded data.");

                var newBuffer = new GraphicsBuffer(
                    GraphicsBuffer.Target.Raw,
                    GraphicsBuffer.UsageFlags.None,
                    (int)m_PersistentInstanceDataSize / 4,
                    4);
                m_GPUUploader.ReplaceBuffer(newBuffer, true);

                m_GPUPersistentInstanceBufferHandle = newBuffer.bufferHandle;

                UpdateBatchBufferHandles();

                if(m_GPUPersistentInstanceData != null)
                    m_GPUPersistentInstanceData.Dispose();
                m_GPUPersistentInstanceData = newBuffer;
            }

            m_ThreadedGPUUploader =
                m_GPUUploader.Begin(totalUploadBytes, biggestUploadBytes, numOperations);
        }

        private void UpdateBatchBufferHandles()
        {
            foreach (var b in m_ExistingBatchIndices)
            {
                m_BatchRendererGroup.SetBatchBuffer(new BatchID { value = (uint)b }, m_GPUPersistentInstanceBufferHandle);
            }
        }

#if DEBUG_LOG_MEMORY_USAGE
        private static ulong PrevUsedSpace = 0;
#endif

        private void EndUpdate()
        {
            if (m_ThreadedGPUUploader.IsValid)
                m_GPUUploader.EndAndCommit(m_ThreadedGPUUploader);

            // Set the uploader struct to null to ensure that any calls
            // to EndAndCommit are made with a struct returned from Begin()
            // on the same frame. This is important in case Begin() is skipped
            // on a frame.
            m_ThreadedGPUUploader = default;

#if DEBUG_LOG_MEMORY_USAGE
            if (m_GPUPersistentAllocator.UsedSpace != PrevUsedSpace)
            {
                Debug.Log($"GPU memory: {m_GPUPersistentAllocator.UsedSpace / 1024.0 / 1024.0:F4} / {m_GPUPersistentAllocator.Size / 1024.0 / 1024.0:F4}");
                PrevUsedSpace = m_GPUPersistentAllocator.UsedSpace;
            }
#endif

#if ENABLE_MATERIALMESHINFO_BOUNDS_CHECKING
            World.GetExistingSystemManaged<RegisterMaterialsAndMeshesSystem>()?.LogBoundsCheckErrorMessages();
#endif
        }

        internal static NativeList<T> NewNativeListResized<T>(int length, Allocator allocator, NativeArrayOptions resizeOptions = NativeArrayOptions.ClearMemory) where T : unmanaged
        {
            var list = new NativeList<T>(length, allocator);
            list.Resize(length, resizeOptions);

            return list;
        }

        /// <summary>
        /// Registers a material with the Entities Graphics System.
        /// </summary>
        /// <param name="material">The material instance to register</param>
        /// <returns>Returns the batch material ID</returns>
        public BatchMaterialID RegisterMaterial(Material material) => m_BatchRendererGroup.RegisterMaterial(material);

        /// <summary>
        /// Registers a mesh with the Entities Graphics System.
        /// </summary>
        /// <param name="mesh">Mesh instance to register</param>
        /// <returns>Returns the batch mesh ID</returns>
        public BatchMeshID RegisterMesh(Mesh mesh) => m_BatchRendererGroup.RegisterMesh(mesh);

        /// <summary>
        /// Unregisters a material from the Entities Graphics System.
        /// </summary>
        /// <param name="material">Material ID received from <see cref="RegisterMaterial"/></param>
        public void UnregisterMaterial(BatchMaterialID material) => m_BatchRendererGroup.UnregisterMaterial(material);

        /// <summary>
        /// Unregisters a mesh from the Entities Graphics System.
        /// </summary>
        /// <param name="mesh">A mesh ID received from <see cref="RegisterMesh"/>.</param>
        public void UnregisterMesh(BatchMeshID mesh) => m_BatchRendererGroup.UnregisterMesh(mesh);

        /// <summary>
        /// Returns the <see cref="Mesh"/> that corresponds to the given registered mesh ID, or <c>null</c> if no such mesh exists.
        /// </summary>
        /// <param name="mesh">A mesh ID received from <see cref="RegisterMesh"/>.</param>
        /// <returns>The <see cref="Mesh"/> object corresponding to the given mesh ID if the ID is valid, or <c>null</c> if it's not valid.</returns>
        public Mesh GetMesh(BatchMeshID mesh) => m_BatchRendererGroup.GetRegisteredMesh(mesh);

        /// <summary>
        /// Returns the <see cref="Material"/> that corresponds to the given registered material ID, or <c>null</c> if no such material exists.
        /// </summary>
        /// <param name="material">A material ID received from <see cref="RegisterMaterial"/>.</param>
        /// <returns>The <see cref="Material"/> object corresponding to the given material ID if the ID is valid, or <c>null</c> if it's not valid.</returns>
        public Material GetMaterial(BatchMaterialID material) => m_BatchRendererGroup.GetRegisteredMaterial(material);

        /// <summary>
        /// Converts a type index into a type name.
        /// </summary>
        /// <param name="typeIndex">The type index to convert.</param>
        /// <returns>The name of the type for given type index.</returns>
        internal static string TypeIndexToName(int typeIndex)
        {
#if DEBUG_PROPERTY_NAMES
            if (s_TypeIndexToName.TryGetValue(typeIndex, out var name))
                return name;
            else
                return "<unknown type>";
#else
            return null;
#endif
        }

        /// <summary>
        /// Converts a name ID to a name.
        /// </summary>
        /// <param name="nameID"></param>
        /// <returns>The name for the given name ID.</returns>
        internal static string NameIDToName(int nameID)
        {
#if DEBUG_PROPERTY_NAMES
            if (s_NameIDToName.TryGetValue(nameID, out var name))
                return name;
            else
                return "<unknown property>";
#else
            return null;
#endif
        }

        internal static string TypeIndexFormatted(int typeIndex)
        {
            return $"{TypeIndexToName(typeIndex)} ({typeIndex:x8})";
        }

        /// <summary>
        /// Converts a name ID to a formatted name.
        /// </summary>
        /// <param name="nameID"></param>
        /// <returns>The formatted name for the given name ID.</returns>
        internal static string NameIDFormatted(int nameID)
        {
            return $"{NameIDToName(nameID)} ({nameID:x8})";
        }
    }
}

