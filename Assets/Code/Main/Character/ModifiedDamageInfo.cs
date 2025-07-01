using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.DamageInfo;

namespace Awaken.TG.Main.Character {
    public struct ModifiedDamageInfo {
        [UnityEngine.Scripting.Preserve] public Damage Damage { [UnityEngine.Scripting.Preserve] get; }
        [UnityEngine.Scripting.Preserve] public DamageModifiersInfo Modifiers { [UnityEngine.Scripting.Preserve] get; }
        
        public ModifiedDamageInfo(Damage damage, DamageModifiersInfo modifiers) {
            Damage = damage;
            Modifiers = modifiers;
        }
    }
}