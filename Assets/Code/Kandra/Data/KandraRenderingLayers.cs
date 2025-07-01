using System;

namespace Awaken.Kandra.Data {
    [Flags]
    public enum KandraRenderingLayers : uint {
        [UnityEngine.Scripting.Preserve] Default = 1 << 0,
        [UnityEngine.Scripting.Preserve] UI = 1 << 1,
        [UnityEngine.Scripting.Preserve] EnvironmentUI = 1 << 2,
        [UnityEngine.Scripting.Preserve] LightLayer3 = 1 << 3,
        [UnityEngine.Scripting.Preserve] LightLayer4 = 1 << 4,
        [UnityEngine.Scripting.Preserve] LightLayer5 = 1 << 5,
        [UnityEngine.Scripting.Preserve] LightLayer6 = 1 << 6,
        [UnityEngine.Scripting.Preserve] LightLayer7 = 1 << 7,
        [UnityEngine.Scripting.Preserve] DecalLayerDefault = 1 << 8,
        [UnityEngine.Scripting.Preserve] DecalLayer1 = 1 << 9,
        [UnityEngine.Scripting.Preserve] DecalLayer2 = 1 << 10,
        [UnityEngine.Scripting.Preserve] DecalLayer3 = 1 << 11,
        [UnityEngine.Scripting.Preserve] DecalLayer4 = 1 << 12,
        [UnityEngine.Scripting.Preserve] DecalLayer5 = 1 << 13,
        [UnityEngine.Scripting.Preserve] DecalLayerItems = 1 << 14,
        [UnityEngine.Scripting.Preserve] DecalLayerAI = 1 << 15,
    }
}