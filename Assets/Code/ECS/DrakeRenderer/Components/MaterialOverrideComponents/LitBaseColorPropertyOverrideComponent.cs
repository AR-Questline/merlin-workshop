using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace Awaken.ECS.DrakeRenderer.Components.MaterialOverrideComponents {
    [MaterialProperty("_BaseColor", 4 * 4)]
    public struct LitBaseColorPropertyOverrideComponent : IComponentData {
        [UnityEngine.Scripting.Preserve] public Color value;
    }
}