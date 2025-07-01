using Unity.Entities;

namespace Awaken.ECS.DrakeRenderer.Components {
    public struct DrakeEntityPrefab : IBufferElementData {
        [UnityEngine.Scripting.Preserve]
        public Entity value;
    }
}