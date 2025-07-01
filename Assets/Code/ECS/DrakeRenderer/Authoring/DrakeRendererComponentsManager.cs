using Awaken.ECS.DrakeRenderer.Components;
using Awaken.ECS.Mipmaps.Components;
using Awaken.Utility.Collections;
using Awaken.Utility.Graphics.Mipmaps;
using Awaken.Utility.LowLevel.Collections;
using Unity.Collections;
using Unity.Rendering;
using UnityEngine.Rendering;
using UniversalProfiling;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    public class DrakeRendererComponentsManager {
        const int PreAllocSize = 512;

        static readonly UniversalProfilerMarker LoadMeshMarker = new("DrakeRendererComponentsManager.UpdateLoadings");

        readonly DrakeRendererLoadingManager _loadingManager;

        UnsafeBitmask _meshesToLoad = new(PreAllocSize, Allocator.Domain);
        UnsafeBitmask _materialsToLoad = new(PreAllocSize, Allocator.Domain);

        // We don't "release" memory as this is persistent thru the whole game, so there is no point in releasing it
        UnsafeArray<BatchMeshID> _loadedMeshes = new(PreAllocSize, Allocator.Domain);
        UnsafeArray<float> _uvDistributions = new(PreAllocSize, Allocator.Domain);
        UnsafeArray<BatchMaterialID> _loadedMaterials = new(PreAllocSize, Allocator.Domain);
        UnsafeArray<MipmapsStreamingMasterMaterials.MaterialId> _materialIndices;

        EntitiesGraphicsSystem _entitiesGraphicsSystem;

        public DrakeRendererComponentsManager(DrakeRendererLoadingManager loadingManager, EntitiesGraphicsSystem entitiesGraphicsSystem) {
            _loadingManager = loadingManager;
            _entitiesGraphicsSystem = entitiesGraphicsSystem;
            _materialIndices = new UnsafeArray<MipmapsStreamingMasterMaterials.MaterialId>(PreAllocSize, Allocator.Domain);
            _materialIndices.Fill(MipmapsStreamingMasterMaterials.MaterialId.Invalid);
        }

        public void UpdateLoadings() {
            LoadMeshMarker.Begin();

            if (!_meshesToLoad.AnySet() && !_materialsToLoad.AnySet()) {
                LoadMeshMarker.End();
                return;
            }

            foreach (var meshIndex in _meshesToLoad.EnumerateOnes()) {
                if (_loadingManager.TryGetLoadedMesh((ushort)meshIndex, out var mesh)) {
                    _meshesToLoad.Down(meshIndex);
                    _loadedMeshes[meshIndex] = _entitiesGraphicsSystem.RegisterMesh(mesh);
                    _uvDistributions[meshIndex] = mesh.GetUVDistributionMetric(0);
                }
            }

            foreach (var materialIndex in _materialsToLoad.EnumerateOnes()) {
                if (_loadingManager.TryGetLoadedMaterial((ushort)materialIndex, out var runtimeKey, out var material)) {
                    _loadingManager.CallMaterialLoaded(runtimeKey, material);
                    _materialsToLoad.Down(materialIndex);
                    _loadedMaterials[materialIndex] = _entitiesGraphicsSystem.RegisterMaterial(material);
                    _materialIndices[materialIndex] = MipmapsStreamingMasterMaterials.Instance.AddMaterial(material);
                }
            }
            LoadMeshMarker.End();
        }

        public void Register(in DrakeMeshMaterialComponent meshMaterial) {
            var newMeshSize = (uint)meshMaterial.meshIndex + 1;
            if (_loadedMeshes.Length < newMeshSize) {
                _loadedMeshes.Resize(newMeshSize);
                _uvDistributions.Resize(newMeshSize);
            }

            var newMaterialSize = (uint)meshMaterial.materialIndex + 1;
            if (_loadedMaterials.Length < newMaterialSize) {
                _loadedMaterials.Resize(newMaterialSize);
                _materialIndices.Resize(newMaterialSize, MipmapsStreamingMasterMaterials.MaterialId.Invalid);
            }

            _meshesToLoad.EnsureIndex(meshMaterial.meshIndex);
            _materialsToLoad.EnsureIndex(meshMaterial.materialIndex);
        }

        public bool TryGetMaterialMesh(in DrakeMeshMaterialComponent drakeMeshMaterial,
            out MaterialMeshInfo materialMeshInfo, out MipmapsMaterialComponent mipmapsMaterialComponent,
            out UVDistributionMetricComponent uvDistributionMetricComponent) {
            materialMeshInfo = new MaterialMeshInfo(_loadedMaterials[drakeMeshMaterial.materialIndex], _loadedMeshes[drakeMeshMaterial.meshIndex], drakeMeshMaterial.submesh);
            mipmapsMaterialComponent = new MipmapsMaterialComponent(_materialIndices[drakeMeshMaterial.materialIndex]);
            uvDistributionMetricComponent = new UVDistributionMetricComponent(_uvDistributions[drakeMeshMaterial.meshIndex]);
            return materialMeshInfo.MeshID != BatchMeshID.Null && materialMeshInfo.MaterialID != BatchMaterialID.Null;
        }

        public void StartLoading(in DrakeMeshMaterialComponent meshMaterial) {
            if (_loadingManager.StartLoadingMesh(meshMaterial.meshIndex)) {
                _meshesToLoad.Up(meshMaterial.meshIndex);
            }

            if (_loadingManager.StartLoadingMaterial(meshMaterial.materialIndex)) {
                _materialsToLoad.Up(meshMaterial.materialIndex);
            }
        }

        public void Unload(in DrakeMeshMaterialComponent meshMaterial, bool assumeMaterialIsLoaded) {
            if (_loadingManager.WillReleaseMesh(meshMaterial.meshIndex)) {
                if (_loadedMeshes[meshMaterial.meshIndex] != BatchMeshID.Null) {
                    _entitiesGraphicsSystem.UnregisterMesh(_loadedMeshes[meshMaterial.meshIndex]);
                    _loadedMeshes[meshMaterial.meshIndex] = BatchMeshID.Null;
                }
                _meshesToLoad.Down(meshMaterial.meshIndex);
            }
            _loadingManager.UnloadMesh(meshMaterial.meshIndex);
            var materialIndex = meshMaterial.materialIndex;
            if (_loadingManager.WillReleaseMaterial(materialIndex)) {
                if (_loadedMaterials[materialIndex] != BatchMaterialID.Null) {
                    _entitiesGraphicsSystem.UnregisterMaterial(_loadedMaterials[materialIndex]);
                    _loadedMaterials[materialIndex] = BatchMaterialID.Null;
                }
                var materialId = _materialIndices[materialIndex];
                if (assumeMaterialIsLoaded | materialId != MipmapsStreamingMasterMaterials.MaterialId.Invalid) {
                    MipmapsStreamingMasterMaterials.Instance.RemoveMaterial(materialId);
                }
                _materialsToLoad.Down(materialIndex);
                _materialIndices[materialIndex] = MipmapsStreamingMasterMaterials.MaterialId.Invalid;
            }
            _loadingManager.UnloadMaterial(meshMaterial.materialIndex);
        }

        public void MarkLoadingRuntimeMaterial(ushort materialIndex) {
            _materialsToLoad.Up(materialIndex);
        }

        public void UnloadRuntimeMaterial(ushort materialIndex) {
            if (!_loadingManager.MaterialReleased(materialIndex)) {
                return;
            }
            if (_loadedMaterials[materialIndex] != BatchMaterialID.Null) {
                _entitiesGraphicsSystem.UnregisterMaterial(_loadedMaterials[materialIndex]);
                _loadedMaterials[materialIndex] = BatchMaterialID.Null;
            }
            var materialId = _materialIndices[materialIndex];
            if (materialId != MipmapsStreamingMasterMaterials.MaterialId.Invalid) {
                MipmapsStreamingMasterMaterials.Instance.RemoveMaterial(materialId);
                _materialIndices[materialIndex] = MipmapsStreamingMasterMaterials.MaterialId.Invalid;
            }
            _materialsToLoad.Down(materialIndex);
        }
        
        public Unmanaged GetUnmanaged() {
            return new Unmanaged(_loadedMeshes, _uvDistributions, _loadedMaterials, _materialIndices);
        }

        public struct Unmanaged {
            UnsafeArray<BatchMeshID>.Span _loadedMeshes;
            UnsafeArray<float>.Span _uvDistributions;
            UnsafeArray<BatchMaterialID>.Span _loadedMaterials;
            UnsafeArray<MipmapsStreamingMasterMaterials.MaterialId>.Span _materialIndices;

            public Unmanaged(UnsafeArray<BatchMeshID>.Span loadedMeshes, UnsafeArray<float>.Span uvDistributions, UnsafeArray<BatchMaterialID>.Span loadedMaterials, UnsafeArray<MipmapsStreamingMasterMaterials.MaterialId>.Span materialIndices) {
                _loadedMeshes = loadedMeshes;
                _uvDistributions = uvDistributions;
                _loadedMaterials = loadedMaterials;
                _materialIndices = materialIndices;
            }

            public bool TryGetMaterialMesh(in DrakeMeshMaterialComponent drakeMeshMaterial,
                out MaterialMeshInfo materialMeshInfo, out MipmapsMaterialComponent mipmapsMaterialComponent,
                out UVDistributionMetricComponent uvDistributionMetricComponent) {
                materialMeshInfo = new MaterialMeshInfo(_loadedMaterials[drakeMeshMaterial.materialIndex], _loadedMeshes[drakeMeshMaterial.meshIndex], drakeMeshMaterial.submesh);
                mipmapsMaterialComponent = new MipmapsMaterialComponent(_materialIndices[drakeMeshMaterial.materialIndex]);
                uvDistributionMetricComponent = new UVDistributionMetricComponent(_uvDistributions[drakeMeshMaterial.meshIndex]);
                return materialMeshInfo.MeshID != BatchMeshID.Null && materialMeshInfo.MaterialID != BatchMaterialID.Null;
            }
        }
    }
}
