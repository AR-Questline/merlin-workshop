using Awaken.TG.Main.Skills.Units.Passives;

namespace Awaken.TG.Main.Skills.Units.Talents {
    [UnityEngine.Scripting.Preserve]
    public class DisableBowMovementPenalties : HeroLogicModifierUnit {
        protected override void SetActive(bool enable) {
            LogicModifiers.DisableBowPullMovementPenalties.Set(enable);
        }
    }
}