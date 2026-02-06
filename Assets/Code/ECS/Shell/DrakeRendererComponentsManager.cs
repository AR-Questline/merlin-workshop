using System;
using Awaken.ECS.DrakeRenderer.Components;
using Awaken.ECS.Mipmaps.Components;
using Awaken.Utility.Graphics.Mipmaps;
using Awaken.Utility.LowLevel.Collections;
using Unity.Rendering;
using UnityEngine.Rendering;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    public class DrakeRendererComponentsManager {
        public DrakeRendererComponentsManager(DrakeRendererLoadingManager loadingManager,
            EntitiesGraphicsSystem entitiesGraphicsSystem) {
            throw new NotImplementedException();
        }

        public void UpdateLoadings() {
            throw new NotImplementedException();
        }

        public void Register(in DrakeMeshMaterialComponent meshMaterial) {
            throw new NotImplementedException();
        }

        public bool TryGetMaterialMesh(in DrakeMeshMaterialComponent drakeMeshMaterial,
            out MaterialMeshInfo materialMeshInfo, out MipmapsMaterialComponent mipmapsMaterialComponent,
            out UVDistributionMetricComponent uvDistributionMetricComponent) {
            throw new NotImplementedException();
        }

        public void StartLoading(in DrakeMeshMaterialComponent meshMaterial) {
            throw new NotImplementedException();
        }

        public void Unload(in DrakeMeshMaterialComponent meshMaterial, bool assumeMaterialIsLoaded) {
            throw new NotImplementedException();
        }

        public void MarkLoadingRuntimeMaterial(ushort materialIndex) {
            throw new NotImplementedException();
        }

        public void UnloadRuntimeMaterial(ushort materialIndex) {
            throw new NotImplementedException();
        }

        public Unmanaged GetUnmanaged() {
            throw new NotImplementedException();
        }

        public struct Unmanaged {
            UnsafeArray<BatchMeshID>.Span _loadedMeshes;
            UnsafeArray<float>.Span _uvDistributions;
            UnsafeArray<BatchMaterialID>.Span _loadedMaterials;
            UnsafeArray<MipmapsStreamingMasterMaterials.MaterialId>.Span _materialIndices;

            public Unmanaged(UnsafeArray<BatchMeshID>.Span loadedMeshes, UnsafeArray<float>.Span uvDistributions,
                UnsafeArray<BatchMaterialID>.Span loadedMaterials,
                UnsafeArray<MipmapsStreamingMasterMaterials.MaterialId>.Span materialIndices) {
                throw new NotImplementedException();
            }

            public bool TryGetMaterialMesh(in DrakeMeshMaterialComponent drakeMeshMaterial,
                out MaterialMeshInfo materialMeshInfo, out MipmapsMaterialComponent mipmapsMaterialComponent,
                out UVDistributionMetricComponent uvDistributionMetricComponent) {
                throw new NotImplementedException();
            }
        }
    }
}