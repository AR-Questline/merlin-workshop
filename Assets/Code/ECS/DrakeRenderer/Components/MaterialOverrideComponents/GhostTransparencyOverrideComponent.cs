using Unity.Entities;
using Unity.Rendering;

namespace Awaken.ECS.DrakeRenderer.Components.MaterialOverrideComponents {
    [MaterialProperty("_Ghost_Transparency", 4)]
    public struct GhostTransparencyOverrideComponent : IComponentData {
        [UnityEngine.Scripting.Preserve] public float value;
    }
}