using Awaken.ECS.Authoring;
using Awaken.ECS.DrakeRenderer.Components;
using Unity.Entities;
using UnityEngine;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    public class ForceDrakeLoad : MonoBehaviour, IDrakeMeshRendererBakingStep {
        public void AddComponentsDrakeRendererEntity(DrakeMeshRenderer drakeMeshRenderer, Entity lodGroupEntity,
            in LodGroupSerializableData lodGroupData, in DrakeMeshMaterialComponent drakeMeshMaterialComponent, Entity entity,
            ref EntityCommandBuffer ecb) {
            ecb.AddComponent<DrakeRendererLoadRequestTag>(entity);
        }
    }
}