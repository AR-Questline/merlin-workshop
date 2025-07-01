namespace Awaken.ECS.DrakeRenderer.Authoring {
    public interface IDrakeMeshRendererBakingModificationStep {
        /// <summary>
        /// Will be called on every MonoBehaviour implementing <see cref="IDrakeMeshRendererBakingModificationStep"/>
        /// and placed on the same GameObject with <see cref="DrakeMeshRenderer"/>.
        /// </summary>
        void ModifyDrakeMeshRenderer(DrakeMeshRenderer drakeMeshRenderer);
    }
}