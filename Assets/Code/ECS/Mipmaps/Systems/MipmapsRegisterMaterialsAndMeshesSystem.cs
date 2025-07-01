using Awaken.Utility.Collections;
using Awaken.Utility.Graphics.Mipmaps;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace Awaken.ECS.Mipmaps.Systems {
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [CreateAfter(typeof(RegisterMaterialsAndMeshesSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial class MipmapsRegisterMaterialsAndMeshesSystem : SystemBase {
        RegisterMaterialsAndMeshesSystem _registerMaterialsAndMeshesSystem;
        public UnsafeParallelHashMap<int, MipmapsRenderArray> mipmapsRenderArrays;

        protected override void OnCreate() {
            _registerMaterialsAndMeshesSystem = World.GetExistingSystemManaged<RegisterMaterialsAndMeshesSystem>();
            mipmapsRenderArrays = new UnsafeParallelHashMap<int, MipmapsRenderArray>(512, Allocator.Persistent);
        }

        protected override void OnDestroy() {
            var brgRenderArrays = mipmapsRenderArrays.GetValueArray(ARAlloc.Temp);
            for (int i = 0; i < brgRenderArrays.Length; ++i) {
                var brgRenderArray = brgRenderArrays[i];

                foreach (var materialId in brgRenderArray.materialIds) {
                    MipmapsStreamingMasterMaterials.Instance.RemoveMaterial(materialId);
                }
                brgRenderArray.materialIds.Dispose();
                brgRenderArray.reciprocalUvDistribution.Dispose();
                brgRenderArray.rangesAccess.Dispose();
            }
            brgRenderArrays.Dispose();
            mipmapsRenderArrays.Dispose();
        }

        protected override void OnUpdate() {
            _registerMaterialsAndMeshesSystem.GetCachedRenderMeshArrays(
                out var renderArrays, out var sharedIndices, out var sharedVersions);

            var brgArraysToDispose = new NativeList<MipmapsRenderArray>(renderArrays.Count, ARAlloc.Temp);

            // Remove RenderMeshArrays that no longer exist
            var sortedKeys = mipmapsRenderArrays.GetKeyArray(ARAlloc.Temp);
            sortedKeys.Sort();

            // Single pass O(n) algorithm. Both arrays are guaranteed to be sorted.
            int j = 0;
            for (int i = 0; i < sortedKeys.Length; i++) {
                var oldKey = sortedKeys[i];
                while (j < renderArrays.Count && sharedIndices[j] < oldKey) {
                    j++;
                }

                bool notFound = j == renderArrays.Count || oldKey != sharedIndices[j];
                if (notFound) {
                    var brgRenderArray = mipmapsRenderArrays[oldKey];
                    brgArraysToDispose.Add(brgRenderArray);

                    mipmapsRenderArrays.Remove(oldKey);
                }
            }
            sortedKeys.Dispose();

            // Update/add RenderMeshArrays
            for (int ri = 0; ri < renderArrays.Count; ++ri) {
                var renderArray = renderArrays[ri];

                var sharedIndex = sharedIndices[ri];
                var sharedVersion = sharedVersions[ri];
                var materialCount = renderArray.Materials.Length;
                var meshCount = renderArray.Meshes.Length;
                var matMeshIndexCount = renderArray.MaterialMeshIndices?.Length ?? 0;
                uint4 hash128 = renderArray.GetHash128();

                bool update = false;
                if (mipmapsRenderArrays.TryGetValue(sharedIndex, out var mipmapsRenderArray)) {
                    // Version change means that the shared component was deleted and another one was created with the same index
                    // It's also possible that the contents changed and the version number did not, so we also compare the 128-bit hash
                    if ((mipmapsRenderArray.version != sharedVersion) ||
                        math.any(mipmapsRenderArray.hash128 != hash128)) {
                        brgArraysToDispose.Add(mipmapsRenderArray);
                        update = true;
                    }
                } else {
                    mipmapsRenderArray = new MipmapsRenderArray();
                    update = true;
                }

                if (update) {
                    mipmapsRenderArray.version = sharedVersion;
                    mipmapsRenderArray.hash128 = hash128;
                    mipmapsRenderArray.materialIds = new UnsafeList<MipmapsStreamingMasterMaterials.MaterialId>(materialCount, Allocator.Persistent);
                    mipmapsRenderArray.reciprocalUvDistribution = new UnsafeList<float>(meshCount, Allocator.Persistent);
                    mipmapsRenderArray.rangesAccess = new UnsafeList<RangeAccessData>(matMeshIndexCount, Allocator.Persistent);

                    for (int i = 0; i < materialCount; ++i) {
                        var material = renderArray.Materials[i];
                        var id = MipmapsStreamingMasterMaterials.Instance.AddMaterial(material);
                        mipmapsRenderArray.materialIds.Add(id);
                    }

                    for (int i = 0; i < meshCount; ++i) {
                        var mesh = renderArray.Meshes[i];
                        var reciprocalUvDistribution = math.rcp(mesh.GetUVDistributionMetric(0));
                        mipmapsRenderArray.reciprocalUvDistribution.Add(reciprocalUvDistribution);
                    }

                    for (int i = 0; i < matMeshIndexCount; ++i) {
                        MaterialMeshIndex matMeshIndex = renderArray.MaterialMeshIndices[i];

                        var materialIndex = matMeshIndex.MaterialIndex;
                        int meshIndex = matMeshIndex.MeshIndex;

                        mipmapsRenderArray.rangesAccess.Add(new RangeAccessData {
                            materialIndex = materialIndex,
                            meshIndex = meshIndex,
                        });
                    }

                    mipmapsRenderArrays[sharedIndex] = mipmapsRenderArray;
                }
            }

            for (int i = 0; i < brgArraysToDispose.Length; ++i) {
                var brgRenderArray = brgArraysToDispose[i];
                foreach (var materialId in brgRenderArray.materialIds) {
                    MipmapsStreamingMasterMaterials.Instance.RemoveMaterial(materialId);
                }
                brgRenderArray.materialIds.Dispose();
                brgRenderArray.reciprocalUvDistribution.Dispose();
                brgRenderArray.rangesAccess.Dispose();
            }

            brgArraysToDispose.Dispose();
        }

        public struct MipmapsRenderArray {
            public int version;
            public UnsafeList<MipmapsStreamingMasterMaterials.MaterialId> materialIds;
            public UnsafeList<float> reciprocalUvDistribution;
            public UnsafeList<RangeAccessData> rangesAccess;
            public uint4 hash128;
        }

        public struct RangeAccessData {
            public int materialIndex;
            public int meshIndex;
        }
    }
}
