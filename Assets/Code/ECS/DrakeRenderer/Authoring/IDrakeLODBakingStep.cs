using Unity.Entities;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    public interface IDrakeLODBakingStep {
        /// <summary>
        /// Will be called on every MonoBehaviour implementing <see cref="IDrakeMeshRendererBakingStep"/>
        /// and placed on the same GameObject with <see cref="DrakeLodGroup"/>.
        /// </summary>
        void AddDrakeEntityComponents(DrakeLodGroup drakeLodGroup, Entity entity, ref EntityCommandBuffer ecb);
    }
}