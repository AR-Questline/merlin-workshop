using Unity.Entities;
using Unity.Rendering;

namespace Awaken.ECS.DrakeRenderer.Components.MaterialOverrideComponents {
    [MaterialProperty("_Animation", 4)]
    public struct AnimationOverrideComponent : IComponentData {
        [UnityEngine.Scripting.Preserve] public float value;
    }
}