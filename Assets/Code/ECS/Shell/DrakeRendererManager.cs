using System;
using Awaken.ECS.Authoring;
using Awaken.ECS.DrakeRenderer.Components;
using Awaken.ECS.Mipmaps.Components;
using Awaken.ECS.Utils;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine.SceneManagement;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    public class DrakeRendererManager : ISystemWithLifetime {
        public static DrakeRendererManager Instance { get; private set; }
        public DrakeRendererLoadingManager LoadingManager => throw new NotImplementedException();
        public DrakeRendererComponentsManager ComponentsManager => throw new NotImplementedException();
        public DrakeRendererEntitiesManager EntitiesManager => throw new NotImplementedException();

        DrakeRendererManager(EntityManager entityManager) {
            throw new NotImplementedException();
        }

        public static void Create() {
            throw new NotImplementedException();
        }

        public static void InitializationUpdate() {
            throw new NotImplementedException();
        }

        public void Register(DrakeLodGroup drakeLodGroup, Scene scene) {
            throw new NotImplementedException();
        }

        public void Register(DrakeLodGroup drakeLodGroup, Scene scene, EntityCommandBuffer ecb,
            DynamicBuffer<Entity> addedEntitiesFromEcb = default) {
            throw new NotImplementedException();
        }

        public void Register(DrakeMeshRenderer drakeMeshRenderer, Scene scene,
            Entity lodGroupEntity = default, in LodGroupSerializableData lodGroupData = default,
            bool? staticOverride = null, int? originalId = null) {
            throw new NotImplementedException();
        }

        public void StartLoading(in DrakeMeshMaterialComponent drakeMeshMaterial) {
            throw new NotImplementedException();
        }

        public bool TryGetMaterialMesh(in DrakeMeshMaterialComponent drakeMeshMaterial,
            out MaterialMeshInfo materialMeshInfo, out MipmapsMaterialComponent mipmapsMaterialComponent,
            out UVDistributionMetricComponent uvDistributionMetricComponent) {
            throw new NotImplementedException();
        }

        public void Unload(DrakeMeshMaterialComponent drakeMeshMaterial, bool assumeMaterialIsLoaded = true) {
            throw new NotImplementedException();
        }

        public void InvalidateEcb() {
            throw new NotImplementedException();
        }

        public Unmanaged GetUnmanaged() {
            throw new NotImplementedException();
        }

        public static SystemRelatedLifeTime<DrakeRendererManager>.IdComponent GetSceneIdComponent(Scene scene) {
            throw new NotImplementedException();
        }

        public struct Unmanaged {
            DrakeRendererComponentsManager.Unmanaged _componentsManager;

            public Unmanaged(DrakeRendererComponentsManager.Unmanaged componentsManager) {
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