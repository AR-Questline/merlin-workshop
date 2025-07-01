using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace Awaken.ECS.DrakeRenderer.Components.MaterialOverrideComponents {
    [MaterialProperty("_EmissiveColor", 4 * 4)]
    public struct EmissiveColorPropertyOverrideComponent : IComponentData {
        [UnityEngine.Scripting.Preserve] public Color value;
    }
}