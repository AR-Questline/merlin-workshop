using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Assertions;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.Rendering
{
    /// <summary>
    /// A struct storing material and mesh array indices.
    /// </summary>
    [Serializable]
    public struct MaterialMeshIndex : IEquatable<MaterialMeshIndex>
    {
        public int MaterialIndex;
        public int MeshIndex;
        public int SubMeshIndex;

        public bool Equals(MaterialMeshIndex other) {
            return MaterialIndex == other.MaterialIndex && MeshIndex == other.MeshIndex && SubMeshIndex == other.SubMeshIndex;
        }

        public override bool Equals(object obj) {
            return obj is MaterialMeshIndex other && Equals(other);
        }

        public override int GetHashCode() {
            unchecked {
                int hashCode = MaterialIndex;
                hashCode = (hashCode * 397) ^ MeshIndex;
                hashCode = (hashCode * 397) ^ SubMeshIndex;
                return hashCode;
            }
        }

        public static bool operator ==(MaterialMeshIndex left, MaterialMeshIndex right) {
            return left.Equals(right);
        }

        public static bool operator !=(MaterialMeshIndex left, MaterialMeshIndex right) {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// Represents which materials and meshes to use to render an entity.
    /// </summary>
    /// <remarks>
    /// This struct supports both a serializable static encoding in which case Material and Mesh are
    /// array indices to some array (typically a RenderMeshArray), and direct use of
    /// runtime BatchRendererGroup BatchMaterialID / BatchMeshID values.
    /// </remarks>
    public struct MaterialMeshInfo : IComponentData, IEnableableComponent, IWithDebugText
    {
        /// <summary>
        /// The material ID.
        /// </summary>
        /// <remarks>
        /// The material ID can be one of the following:
        ///
        /// * A literal Material ID received from the RegisterMaterial API, encoded as a positive integer.
        /// * An array index to the RenderMeshArray shared component of the entity, encoded as a negative integer.
        ///
        /// Use the literal Material ID to change the material at runtime.
        /// Use the array index to store the material ID to disk during entity baking.
        /// </remarks>
        public int Material;

        /// <summary>
        /// The mesh ID.
        /// </summary>
        public int Mesh;

        /// <summary>
        /// The bit packed sub-mesh related data.
        /// </summary>
        private uint SubMeshInfo;

        /// <summary>
        /// The sub-mesh ID.
        /// </summary>
        public sbyte SubMesh
        {
            get => ExtractSubMeshIndex(SubMeshInfo);
            set => SubMeshInfo = BuildSubMeshInfoFromSubMeshIndex(value);
        }

        public uint PlainSubMeshInfo
        {
            get => SubMeshInfo;
        }

        /// <summary>
        /// The MaterialMeshIndex range.
        /// </summary>
        public RangeInt MaterialMeshIndexRange => new RangeInt
        {
            start = ExtractMaterialMeshIndexRangeStart(SubMeshInfo),
            length = ExtractMaterialMeshIndexRangeLength(SubMeshInfo),
        };

        /// <summary>
        /// True if the MaterialMeshInfo is using a MaterialMeshIndex range.
        /// </summary>
        public bool HasMaterialMeshIndexRange => HasMaterialMeshIndexRangeBit(SubMeshInfo);

        public string DebugText {
            get {
                if (HasMaterialMeshIndexRange) {
                    var range = MaterialMeshIndexRange;
                    return $"Range: [{range.start} - {range.end}) Length: {range.length}";
                } else {
                    return $"Material: {Material}, Mesh: {Mesh}, SubMesh: {SubMesh}";
                }
            }
        }

        /// <summary>
        /// The sub-mesh ID.
        /// </summary>
        [Obsolete("Use SubMesh instead. (UnityUpgradable) -> SubMesh", true)]
        public sbyte Submesh { get => SubMesh; set => SubMesh = value; }

        /// <summary>
        /// Converts the given array index (typically the index inside RenderMeshArray) into
        /// a negative number that denotes that array position.
        /// </summary>
        /// <param name="index">The index to convert.</param>
        /// <returns>Returns the converted index.</returns>
        public static int ArrayIndexToStaticIndex(int index) => (index < 0)
            ? index
            : (-index - 1);

        /// <summary>
        /// Converts the given static index (a negative value) to a valid array index.
        /// </summary>
        /// <param name="staticIndex">The index to convert.</param>
        /// <returns>Returns the converted index.</returns>
        public static int StaticIndexToArrayIndex(int staticIndex) => math.abs(staticIndex) - 1;

        /// <summary>
        /// Creates an instance of MaterialMeshInfo from material and mesh/sub-mesh indices in the corresponding RenderMeshArray.
        /// </summary>
        /// <param name="materialIndexInRenderMeshArray">The material index in <see cref="RenderMeshArray.Materials"/>.</param>
        /// <param name="meshIndexInRenderMeshArray">The mesh index in <see cref="RenderMeshArray.Meshes"/>.</param>
        /// <param name="submeshIndex">An optional submesh ID.</param>
        /// <returns>Returns the MaterialMeshInfo instance that contains the material and mesh indices.</returns>
        public static MaterialMeshInfo FromRenderMeshArrayIndices(
            int materialIndexInRenderMeshArray,
            int meshIndexInRenderMeshArray,
            sbyte submeshIndex = 0)
        {
            return new MaterialMeshInfo(
                ArrayIndexToStaticIndex(materialIndexInRenderMeshArray),
                ArrayIndexToStaticIndex(meshIndexInRenderMeshArray),
                BuildSubMeshInfoFromSubMeshIndex(submeshIndex));
        }

        /// <summary>
        /// Creates an instance of MaterialMeshInfo from a range of material/mesh/submesh index in the corresponding RenderMeshArray.
        /// </summary>
        /// <param name="rangeStart">The first index of the range in <see cref="RenderMeshArray.MaterialMeshIndices"/>.</param>
        /// <param name="rangeLength">The length of the range in <see cref="RenderMeshArray.MaterialMeshIndices"/>.</param>
        /// <returns></returns>
        public static MaterialMeshInfo FromMaterialMeshIndexRange(int rangeStart, int rangeLength)
        {
            return new MaterialMeshInfo(0, 0, BuildSubMeshInfoFromMaterialMeshRange(rangeStart, rangeLength));
        }

        public MaterialMeshInfo(int material, int mesh, uint subMeshInfo)
        {
            Material = material;
            Mesh = mesh;
            SubMeshInfo = subMeshInfo;
        }

        /// <summary>
        /// Creates an instance of MaterialMeshInfo from material and mesh/sub-mesh IDs registered with <see cref="EntitiesGraphicsSystem"/>
        /// </summary>
        /// <param name="materialID">The material ID from <see cref="EntitiesGraphicsSystem.RegisterMaterial"/>.</param>
        /// <param name="meshID">The mesh ID from <see cref="EntitiesGraphicsSystem.RegisterMesh"/>.</param>
        /// <param name="submeshIndex">An optional submesh ID.</param>
        public MaterialMeshInfo(BatchMaterialID materialID, BatchMeshID meshID, sbyte submeshIndex = 0)
            : this((int)materialID.value, (int)meshID.value, BuildSubMeshInfoFromSubMeshIndex(submeshIndex))
        {}

        /// <summary>
        /// The mesh ID property.
        /// </summary>
        public BatchMeshID MeshID
        {
            get
            {
                Assert.IsTrue(IsRuntimeMesh);
                return new BatchMeshID { value = (uint)Mesh };
            }

            set => Mesh = (int) value.value;
        }

        /// <summary>
        /// The material ID property.
        /// </summary>
        public BatchMaterialID MaterialID
        {
            get
            {
                Assert.IsTrue(IsRuntimeMaterial);
                return new BatchMaterialID() { value = (uint)Material };
            }

            set => Material = (int) value.value;
        }

        internal bool IsRuntimeMaterial => Material >= 0;

        public bool IsRuntimeMesh => Mesh >= 0;

        public int MeshArrayIndex
        {
            get => IsRuntimeMesh ? -1 : StaticIndexToArrayIndex(Mesh);
            set => Mesh = ArrayIndexToStaticIndex(value);
        }

        public int MaterialArrayIndex
        {
            get => IsRuntimeMaterial ? -1 : StaticIndexToArrayIndex(Material);
            set => Material = ArrayIndexToStaticIndex(value);
        }

        static uint BuildSubMeshInfoFromSubMeshIndex(sbyte subMeshIndex)
        {
            return (uint)subMeshIndex;
        }

        static uint BuildSubMeshInfoFromMaterialMeshRange(int rangeStartIndex, int rangeLength)
        {
            // Bit packing layout
            // ====================================
            // 20 bits : Range start index.
            // 7 bits : Range length.
            // 4 bits (unused) : Could be used for LOD in the future?
            // 1 bit : True when using material mesh index range, otherwise false.

            Assert.IsTrue(rangeStartIndex < (1 << 20));
            Assert.IsTrue(rangeLength < (1 << 7));

            uint rangeStartIndexU32 = (uint)rangeStartIndex;
            uint rangeLengthU32 = (uint)rangeLength;

            uint rangeStartIndexMask = rangeStartIndexU32 & 0x000fffff;
            uint rangeLengthMask = (rangeLengthU32 << 20) & 0x07f00000;
            uint infoMask = 0x80000000;

            return rangeStartIndexMask | rangeLengthMask | infoMask;
        }

        static sbyte ExtractSubMeshIndex(uint subMeshInfo)
        {
            Assert.IsTrue(!HasMaterialMeshIndexRangeBit(subMeshInfo));
            return (sbyte)(subMeshInfo & 0xff);
        }

        static int ExtractMaterialMeshIndexRangeStart(uint subMeshInfo)
        {
            Assert.IsTrue(HasMaterialMeshIndexRangeBit(subMeshInfo));
            return (int)(subMeshInfo & 0xfffff);
        }

        static int ExtractMaterialMeshIndexRangeLength(uint subMeshInfo)
        {
            Assert.IsTrue(HasMaterialMeshIndexRangeBit(subMeshInfo));
            return (int)((subMeshInfo >> 20) & 0x7f);
        }

        static bool HasMaterialMeshIndexRangeBit(uint subMeshInfo)
        {
            return (subMeshInfo & 0x80000000) != 0;
        }
    }

    internal struct AssetHash
    {
        public static void UpdateAsset(ref xxHash3.StreamingState hash, UnityEngine.Object asset)
        {
            // In the editor we can compute a stable serializable hash using an asset GUID
#if UNITY_EDITOR
            bool success = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long localId);
            hash.Update(success);
            if (!success)
            {
                hash.Update(asset.GetInstanceID());
                return;
            }
            var guidBytes = Encoding.UTF8.GetBytes(guid);

            hash.Update(guidBytes.Length);
            for (int j = 0; j < guidBytes.Length; ++j)
                hash.Update(guidBytes[j]);
            hash.Update(localId);
#else
            // In standalone, we have to resort to using the instance ID which is not serializable,
            // but should be usable in the context of this execution.
            hash.Update(asset.GetInstanceID());
#endif
        }
    }

    /// <summary>
    /// A shared component that contains meshes and materials.
    /// </summary>
    public struct RenderMeshArray : ISharedComponentData, IEquatable<RenderMeshArray>
    {
        [SerializeField] private Material[] m_Materials;
        [SerializeField] private Mesh[] m_Meshes;
        [SerializeField] private MaterialMeshIndex[] m_MaterialMeshIndices;

        // Memoize the expensive 128-bit hash
        [SerializeField] private uint4 m_Hash128;

        /// <summary>
        /// Constructs an instance of RenderMeshArray from an array of materials and an array of meshes.
        /// </summary>
        /// <param name="materials">The array of materials to use in the RenderMeshArray.</param>
        /// <param name="meshes">The array of meshes to use in the RenderMeshArray.</param>
        /// <param name="materialMeshIndices">The array of MaterialMeshIndex to use in the RenderMeshArray.</param>
        public RenderMeshArray(Material[] materials, Mesh[] meshes, MaterialMeshIndex[] materialMeshIndices = null)
        {
            m_Meshes = meshes;
            m_Materials = materials;
            m_MaterialMeshIndices = materialMeshIndices;
            m_Hash128 = uint4.zero;
            ResetHash128();
        }

        /// <summary>
        /// Accessor property for the MaterialMeshIndex array.
        /// </summary>
        public MaterialMeshIndex[] MaterialMeshIndices
        {
            readonly get => m_MaterialMeshIndices;
            set
            {
                m_Hash128 = uint4.zero;
                m_MaterialMeshIndices = value;
            }
        }

        /// <summary>
        /// Accessor property for the meshes array.
        /// </summary>
        public Mesh[] Meshes
        {
            readonly get => m_Meshes;
            set
            {
                m_Hash128 = uint4.zero;
                m_Meshes = value;
            }
        }

        /// <summary>
        /// Accessor property for the materials array.
        /// </summary>
        public Material[] Materials
        {
            get => m_Materials;
            set
            {
                m_Hash128 = uint4.zero;
                m_Materials = value;
            }
        }

        internal Mesh GetMeshWithStaticIndex(int staticMeshIndex)
        {
            Assert.IsTrue(staticMeshIndex <= 0, "Mesh index must be a static index (non-positive)");

            if (staticMeshIndex >= 0)
                return null;

            return m_Meshes[MaterialMeshInfo.StaticIndexToArrayIndex(staticMeshIndex)];
        }

        internal Material GetMaterialWithStaticIndex(int staticMaterialIndex)
        {
            Assert.IsTrue(staticMaterialIndex <= 0, "Material index must be a static index (non-positive)");

            if (staticMaterialIndex >= 0)
                return null;

            return m_Materials[MaterialMeshInfo.StaticIndexToArrayIndex(staticMaterialIndex)];
        }

        /// <summary>
        /// Returns a 128-bit hash that (almost) uniquely identifies the contents of the component.
        /// </summary>
        /// <remarks>
        /// This is useful to help make comparisons between RenderMeshArray instances less resource intensive.
        /// </remarks>
        /// <returns>Returns the 128-bit hash value.</returns>
        public uint4 GetHash128()
        {
            return m_Hash128;
        }

        /// <summary>
        /// Recalculates the 128-bit hash value of the component.
        /// </summary>
        public void ResetHash128()
        {
            m_Hash128 = ComputeHash128();
        }

        /// <summary>
        /// Calculates and returns the 128-bit hash value of the component contents.
        /// </summary>
        /// <remarks>
        /// This is equivalent to calling <see cref="ResetHash128"/> and then <see cref="GetHash128"/>.
        /// </remarks>
        /// <returns>Returns the calculated 128-bit hash value.</returns>
        public uint4 ComputeHash128()
        {
            var hash = new xxHash3.StreamingState(false);

            int numMeshes = m_Meshes?.Length ?? 0;
            int numMaterials = m_Materials?.Length ?? 0;
            int numMatMeshIndices = m_MaterialMeshIndices?.Length ?? 0;

            hash.Update(numMeshes);
            hash.Update(numMaterials);
            hash.Update(numMatMeshIndices);

            for (int i = 0; i < numMeshes; ++i)
                AssetHash.UpdateAsset(ref hash, m_Meshes[i]);

            for (int i = 0; i < numMaterials; ++i)
                AssetHash.UpdateAsset(ref hash, m_Materials[i]);

            for (int i = 0; i < numMatMeshIndices; ++i)
            {
                MaterialMeshIndex matMeshIndex = m_MaterialMeshIndices[i];
                hash.Update(matMeshIndex.MaterialIndex);
                hash.Update(matMeshIndex.MeshIndex);
                hash.Update(matMeshIndex.SubMeshIndex);
            }

            uint4 H = hash.DigestHash128();

            // Make sure the hash is never exactly zero, to keep zero as a null value
            if (math.all(H == uint4.zero))
                return new uint4(1, 0, 0, 0);

            return H;
        }

        /// <summary>
        /// Combines a list of RenderMeshes into one RenderMeshArray.
        /// </summary>
        /// <param name="renderMeshes">The list of RenderMesh instances to combine.</param>
        /// <returns>Returns a RenderMeshArray instance that contains all of the meshes and materials. The <see cref="RenderMeshArray.MaterialMeshIndices"/> field is left to null.</returns>
        public static RenderMeshArray CombineRenderMeshes(List<RenderMesh> renderMeshes)
        {
            var meshes = new Dictionary<Mesh, bool>(renderMeshes.Count);
            var materials = new Dictionary<Material, bool>(renderMeshes.Count);

            foreach (var renderMesh in renderMeshes)
            {
                meshes[renderMesh.mesh] = true;

                if (renderMesh.materials != null)
                {
                    foreach (var material in renderMesh.materials)
                    {
                        if (material != null)
                            materials[material] = true;
                    }
                }
            }

            return new RenderMeshArray(materials.Keys.ToArray(), meshes.Keys.ToArray());
        }

        /// <summary>
        /// Combines a list of RenderMeshArrays into one RenderMeshArray.
        /// </summary>
        /// <param name="renderMeshArrays">The list of RenderMeshArray instances to combine.</param>
        /// <returns>Returns a RenderMeshArray instance that contains all of the meshes and materials. The <see cref="RenderMeshArray.MaterialMeshIndices"/> field is left to null.</returns>
        public static RenderMeshArray CombineRenderMeshArrays(List<RenderMeshArray> renderMeshArrays)
        {
            int totalMeshes = 0;
            int totalMaterials = 0;

            foreach (var rma in renderMeshArrays)
            {
                totalMeshes += rma.Meshes?.Length ?? 0;
                totalMaterials += rma.Meshes?.Length ?? 0;
            }

            var meshes = new Dictionary<Mesh, bool>(totalMeshes);
            var materials = new Dictionary<Material, bool>(totalMaterials);

            foreach (var rma in renderMeshArrays)
            {
                foreach (var mesh in rma.Meshes)
                {
                    if (mesh != null)
                        meshes[mesh] = true;
                }

                foreach (var material in rma.Materials)
                {
                    if (material != null)
                        materials[material] = true;
                }
            }

            return new RenderMeshArray(materials.Keys.ToArray(), meshes.Keys.ToArray());
        }

        /// <summary>
        /// Creates the new instance of the RenderMeshArray from given mesh and material lists, removing duplicate entries.
        /// </summary>
        /// <param name="materialsWithDuplicates">The list of the materials.</param>
        /// <param name="meshesWithDuplicates">The list of the meshes.</param>
        /// <returns>Returns a RenderMeshArray instance that contains all off the meshes and materials, and with no duplicates. The <see cref="RenderMeshArray.MaterialMeshIndices"/> field is left to null.</returns>
        public static RenderMeshArray CreateWithDeduplication(
            List<Material> materialsWithDuplicates, List<Mesh> meshesWithDuplicates)
        {
            var meshes = new Dictionary<Mesh, bool>(meshesWithDuplicates.Count);
            var materials = new Dictionary<Material, bool>(materialsWithDuplicates.Count);

            foreach (var mat in materialsWithDuplicates)
                materials[mat] = true;

            foreach (var mesh in meshesWithDuplicates)
                meshes[mesh] = true;

            return new RenderMeshArray(materials.Keys.ToArray(), meshes.Keys.ToArray());
        }

        /// <summary>
        /// Gets the material for given MaterialMeshInfo.
        /// </summary>
        /// <param name="materialMeshInfo">The MaterialMeshInfo to use.</param>
        /// <returns>Returns the associated material instance, or null if the material is runtime.</returns>
        public Material GetMaterial(MaterialMeshInfo materialMeshInfo)
        {
            // When using an index range, just return the first material of the range
            if (materialMeshInfo.HasMaterialMeshIndexRange)
            {
                RangeInt range = materialMeshInfo.MaterialMeshIndexRange;
                Assert.IsTrue(range.length > 0);

                int firstMaterialIndex = MaterialMeshIndices[range.start].MaterialIndex;
                return Materials[firstMaterialIndex];
            }
            else
            {
                if (materialMeshInfo.IsRuntimeMaterial)
                    return null;
                return Materials[materialMeshInfo.MaterialArrayIndex];
            }
        }

        /// <summary>
        /// Gets the materials for given MaterialMeshInfo.
        /// </summary>
        /// <param name="materialMeshInfo">The MaterialMeshInfo to use.</param>
        /// <returns>Returns the associated material instances, or null if the material is runtime.</returns>
        public List<Material> GetMaterials(MaterialMeshInfo materialMeshInfo)
        {
            if (materialMeshInfo.HasMaterialMeshIndexRange)
            {
                RangeInt range = materialMeshInfo.MaterialMeshIndexRange;
                Assert.IsTrue(range.length > 0);

                var materials = new List<Material>(range.length);

                for (int i = range.start; i < range.end; i++)
                {
                    int materialIndex = MaterialMeshIndices[i].MaterialIndex;
                    materials.Add(Materials[materialIndex]);
                }

                return materials;
            }
            else
            {
                if (materialMeshInfo.IsRuntimeMaterial)
                    return null;
                var material = Materials[materialMeshInfo.MaterialArrayIndex];
                return new List<Material> { material };
            }
        }

        /// <summary>
        /// Gets the mesh for given MaterialMeshInfo.
        /// </summary>
        /// <param name="materialMeshInfo">The MaterialMeshInfo to use.</param>
        /// <returns>Returns the associated Mesh instance or null if the mesh is runtime.</returns>
        public readonly Mesh GetMesh(MaterialMeshInfo materialMeshInfo)
        {
            // When using an index range, just return the first mesh of the range
            if (materialMeshInfo.HasMaterialMeshIndexRange)
            {
                RangeInt range = materialMeshInfo.MaterialMeshIndexRange;
                Assert.IsTrue(range.length > 0);

                int firstMeshIndex = MaterialMeshIndices[range.start].MeshIndex;
                return Meshes[firstMeshIndex];
            }
            else
            {
                return materialMeshInfo.IsRuntimeMesh ? null : Meshes[materialMeshInfo.MeshArrayIndex];
            }
        }

        /// <summary>
        /// Determines whether two object instances are equal based on their hashes.
        /// </summary>
        /// <param name="other">The object to compare with the current object.</param>
        /// <returns>Returns true if the specified object is equal to the current object. Otherwise, returns false.</returns>
        public bool Equals(RenderMeshArray other)
        {
            return math.all(GetHash128() == other.GetHash128());
        }

        /// <summary>
        /// Determines whether two object instances are equal based on their hashes.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>Returns true if the specified object is equal to the current object. Otherwise, returns false.</returns>
        public override bool Equals(object obj)
        {
            return obj is RenderMeshArray other && Equals(other);
        }

        /// <summary>
        /// Calculates the hash code for this object.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return (int) GetHash128().x;
        }

        /// <summary>
        /// The equality operator == returns true if its operands are equal, false otherwise.
        /// </summary>
        /// <param name="left">The left instance to compare.</param>
        /// <param name="right">The right instance to compare.</param>
        /// <returns>True if left and right instances are equal and false otherwise.</returns>
        public static bool operator ==(RenderMeshArray left, RenderMeshArray right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// The not equality operator != returns false if its operands are equal, true otherwise.
        /// </summary>
        /// <param name="left">The left instance to compare.</param>
        /// <param name="right">The right instance to compare.</param>
        /// <returns>False if left and right instances are equal and true otherwise.</returns>
        public static bool operator !=(RenderMeshArray left, RenderMeshArray right)
        {
            return !left.Equals(right);
        }
    }
}
