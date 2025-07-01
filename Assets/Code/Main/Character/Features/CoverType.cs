using System;

namespace Awaken.TG.Main.Character.Features {
    [Flags]
    public enum CoverType : byte {
        None = 0,
        ShortHair = 1 << 0,
        LongHair = 1 << 1,
        Beard = 1 << 2,
        Torso = 1 << 3,
        Legs = 1 << 4,
        FullFaceCovered = 1 << 5,
        
        ShortAndLongHair = ShortHair | LongHair,
        Head = ShortAndLongHair | Beard,
        [UnityEngine.Scripting.Preserve] All = Head | Torso | Legs | FullFaceCovered,
    }
}