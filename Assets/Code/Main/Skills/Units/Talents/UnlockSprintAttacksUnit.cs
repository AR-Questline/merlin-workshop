using Awaken.TG.Main.Skills.Units.Passives;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Talents {
    [UnitCategory("AR/Skills/Talents")]
    [UnityEngine.Scripting.Preserve]
    public class UnlockSprintAttacksUnit : TalentUnlockUnit {
        protected override void SetActive(bool enable) {
            Development.SetActiveSprintAttacksUnlock(enable);
        }
    }
}