using System;
using Awaken.ECS.Authoring;
using Awaken.ECS.DrakeRenderer.Components;
using Unity.Entities;
using UnityEngine;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    public class DrakeAnimatedPropertiesOverrideController : MonoBehaviour, IDrakeMeshRendererBakingModificationStep,
        IDrakeMeshRendererBakingStep {
        public void StartForward() {
            throw new NotImplementedException();
        }

        public void StartBackward() {
            throw new NotImplementedException();
        }

        public void SetInstant() {
            throw new NotImplementedException();
        }

        public void Stop() {
            throw new NotImplementedException();
        }

        public void ModifyDrakeMeshRenderer(DrakeMeshRenderer drakeMeshRenderer) {
            throw new NotImplementedException();
        }

        public void AddComponentsDrakeRendererEntity(DrakeMeshRenderer drakeMeshRenderer, Entity lodGroupEntity,
            in LodGroupSerializableData lodGroupData, in DrakeMeshMaterialComponent drakeMeshMaterialComponent,
            Entity entity, ref EntityCommandBuffer ecb) {
            throw new NotImplementedException();
        }
    }
}