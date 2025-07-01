using Unity.Entities;
using Unity.Rendering;

namespace Awaken.ECS.DrakeRenderer.Components.MaterialOverrideComponents {
    [MaterialProperty("_DissolveTransition", 4)]
    public struct DissolveTransitionOverrideComponent : IComponentData {
        [UnityEngine.Scripting.Preserve] public float value;
    }
}