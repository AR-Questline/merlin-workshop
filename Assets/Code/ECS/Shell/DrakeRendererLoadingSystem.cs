using System;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.ECS.DrakeRenderer.Components;
using Unity.Collections;
using Unity.Entities;

namespace Awaken.ECS.DrakeRenderer.Systems {
    public partial class DrakeRendererLoadingSystem : SystemBase {
        public bool IsLoadingAnyEntities;
        public ref readonly ComponentTypeSet ReleaseResourcesRemoveSet => throw new NotImplementedException();

        protected override void OnCreate() {
            throw new NotImplementedException();
        }

        protected override void OnUpdate() {
            throw new NotImplementedException();
        }

        partial struct CheckAndAssignLoadingRenderersResourcesJob : IJobEntity {
            public DrakeRendererManager.Unmanaged unmanaged;
            public EntityCommandBuffer ecb;
            public NativeList<Entity> passedEntities;

            public void Execute(Entity entity, in DrakeMeshMaterialComponent meshMaterialComponent) {
                throw new NotImplementedException();
            }
        }
    }
}