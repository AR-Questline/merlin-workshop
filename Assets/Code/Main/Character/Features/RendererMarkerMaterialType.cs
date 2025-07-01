using System;

namespace Awaken.TG.Main.Character.Features {
    [Flags]
    public enum RendererMarkerMaterialType : byte {
        [UnityEngine.Scripting.Preserve] Ignore = 0,
        [UnityEngine.Scripting.Preserve] Skin = 1 << 0,
        [UnityEngine.Scripting.Preserve] Head = 1 << 1,
        [UnityEngine.Scripting.Preserve] Body = 1 << 2,
        [UnityEngine.Scripting.Preserve] Face = 1 << 3,
        [UnityEngine.Scripting.Preserve] Eyebrows = 1 << 4,
        [UnityEngine.Scripting.Preserve] Eyes = 1 << 5,
        [UnityEngine.Scripting.Preserve] Teeth = 1 << 6,
    }
}
