using System;

namespace Awaken.TG.Main.Heroes.Statuses {
    [Serializable]
    public enum StatusDamageType {
        [UnityEngine.Scripting.Preserve] Default = 0,
        [UnityEngine.Scripting.Preserve] Burn = 1,
        [UnityEngine.Scripting.Preserve] Breath = 2,
        [UnityEngine.Scripting.Preserve] Poison = 3,
        [UnityEngine.Scripting.Preserve] Bleed = 4,
    }
}