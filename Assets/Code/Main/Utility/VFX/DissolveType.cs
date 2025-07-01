using System;

namespace Awaken.TG.Main.Utility.VFX {
    [Flags]
    public enum DissolveType {
        Cloth = 1 << 1,
        Weapon = 1 << 2,
        All = Cloth | Weapon
    }
}