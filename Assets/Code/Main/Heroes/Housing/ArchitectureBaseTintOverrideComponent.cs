using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Housing {
    [MaterialProperty("_Base_Tint", 4 * 4)]
    public struct ArchitectureBaseTintOverrideComponent : IComponentData {
        [UnityEngine.Scripting.Preserve] public Color value;
    }
}