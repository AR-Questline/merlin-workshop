using Unity.Entities;
using Unity.Rendering;

namespace Awaken.ECS.DrakeRenderer.Components.MaterialOverrideComponents {
    [MaterialProperty("_EmissionIntensity", 4)]
    public struct EmissionIntensityOverrideComponent : IComponentData {
        [UnityEngine.Scripting.Preserve] public float value;
    }
}