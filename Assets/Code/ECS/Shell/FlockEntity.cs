using System;
using Awaken.ECS.Authoring;
using Awaken.ECS.DrakeRenderer;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.ECS.DrakeRenderer.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.ECS.Flocks.Authorings {
    public abstract class FlockEntity : MonoBehaviour, IDrakeMeshRendererBakingStep,
        IDrakeMeshRendererBakingModificationStep, IDrakeLODBakingModificationStep, IDrakeLODFinishBakingListener {
        public Entity Entity { get; private set; }
        
        public void SetRandomScale(float2 minMaxScale) {
            throw new NotImplementedException();
        }

        public virtual void SetupFromFlockGroup(Entity flockEntity,
            DrakeVisualEntitiesTransform drakeVisualEntitiesTransform, FlockGroup flockGroup,
            EntityManager entityManager) {
            throw new NotImplementedException();
        }

        public void AddComponentsDrakeRendererEntity(DrakeMeshRenderer drakeMeshRenderer, Entity lodGroupEntity,
            in LodGroupSerializableData lodGroupData,
            in DrakeMeshMaterialComponent drakeMeshMaterialComponent, Entity entity, ref EntityCommandBuffer ecb) {
            throw new NotImplementedException();
        }

        public void ModifyDrakeLODGroup(DrakeLodGroup drakeLodGroup) {
            throw new NotImplementedException();
        }

        public void ModifyDrakeMeshRenderer(DrakeMeshRenderer drakeMeshRenderer) {
            throw new NotImplementedException();
        }

        public void OnDrakeLodGroupBakingFinished() {
            throw new NotImplementedException();
        }

        public abstract ComponentType[] GetEntityComponentTypes();
    }
}