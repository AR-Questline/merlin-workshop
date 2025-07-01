using Awaken.ECS.Authoring;
using Awaken.ECS.DrakeRenderer.Components;
using Unity.Entities;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    public interface IDrakeMeshRendererBakingStep {
        /// <summary>
        /// Will be called on every MonoBehaviour implementing <see cref="IDrakeMeshRendererBakingStep"/>
        /// and placed on the same GameObject with <see cref="DrakeMeshRenderer"/>.
        /// Each material in <see cref="DrakeMeshRenderer"/> gets its own entity so this method is called
        /// for every material in <see cref="DrakeRendererManager"/>
        /// </summary>
        void AddComponentsDrakeRendererEntity(DrakeMeshRenderer drakeMeshRenderer, Entity lodGroupEntity,
            in LodGroupSerializableData lodGroupData, in DrakeMeshMaterialComponent drakeMeshMaterialComponent,
            Entity entity, ref EntityCommandBuffer ecb);
    }
}