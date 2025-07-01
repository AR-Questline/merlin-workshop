using Unity.Entities;
using Unity.Rendering;

namespace Awaken.ECS.DrakeRenderer.Components.MaterialOverrideComponents {
    [MaterialProperty("_Transition", 4)]
    public struct TransitionOverrideComponent : IComponentData {
        public float value;
    }
}