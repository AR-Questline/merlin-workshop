using System;

namespace Awaken.TG.Main.Fights {
    [Flags]
    public enum WeaponDependentDamageType : byte {
        None = 0,
        Melee = 1 << 0,
        Ranged = 1 << 1,
        Magic = 1 << 2,
        All = Melee | Ranged | Magic
    }
}