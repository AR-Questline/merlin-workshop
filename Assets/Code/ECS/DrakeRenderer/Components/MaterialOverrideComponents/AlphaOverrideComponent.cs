using Unity.Entities;
using Unity.Rendering;

namespace Awaken.ECS.DrakeRenderer.Components.MaterialOverrideComponents {
    [MaterialProperty("_Alpha", 4)]
    public struct AlphaOverrideComponent : IComponentData {
        [UnityEngine.Scripting.Preserve] public float value;
    }
}