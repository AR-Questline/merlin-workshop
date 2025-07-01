using Awaken.TG.Main.Skills.Units.Passives;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Talents {
    [UnitCategory("AR/Skills/Talents")]
    [UnityEngine.Scripting.Preserve]
    public class UnlockSpectralWeaponsPenetrateShieldsUnit : TalentUnlockUnit {
        protected override void SetActive(bool enable) {
            Development.SetActiveSpectralWeaponsPenetrateShields(enable);
        }
    }
}