using Awaken.ECS.Authoring;
using Awaken.ECS.DrakeRenderer.Components;
using Unity.Entities;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    public interface IDrakeMeshRendererBakingStep {
        void AddComponentsDrakeRendererEntity(DrakeMeshRenderer drakeMeshRenderer, Entity lodGroupEntity,
            in LodGroupSerializableData lodGroupData, in DrakeMeshMaterialComponent drakeMeshMaterialComponent,
            Entity entity, ref EntityCommandBuffer ecb);
    }
}