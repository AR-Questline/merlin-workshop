namespace Awaken.ECS.DrakeRenderer.Authoring {
    public interface IDrakeLODBakingModificationStep {
        /// <summary>
        /// Will be called on every MonoBehaviour implementing <see cref="IDrakeLODBakingModificationStep"/>
        /// and placed on the same GameObject with <see cref="DrakeLodGroup"/>.
        /// </summary>
        void ModifyDrakeLODGroup(DrakeLodGroup drakeLodGroup);
    }
}