using Awaken.ECS.Authoring;
using Awaken.ECS.DrakeRenderer.Components;
using Awaken.ECS.Mipmaps.Components;
using Unity.Entities;
using UnityEngine;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    public class ForceDrakeMipmapsFull : MonoBehaviour, IDrakeMeshRendererBakingStep {
        public void AddComponentsDrakeRendererEntity(DrakeMeshRenderer drakeMeshRenderer, Entity lodGroupEntity,
            in LodGroupSerializableData lodGroupData, in DrakeMeshMaterialComponent drakeMeshMaterialComponent, Entity entity,
            ref EntityCommandBuffer ecb) {
            ecb.AddComponent<SkipMipmapsFactorCalculationTag>(entity);
            ecb.SetComponent(entity, new MipmapsFactorComponent());
        }
    }
}
