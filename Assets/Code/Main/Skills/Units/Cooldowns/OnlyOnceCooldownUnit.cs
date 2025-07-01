using Awaken.TG.Main.Heroes.Statuses.Duration;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Cooldowns {
    [UnitCategory("AR/Skills/Costs")]
    [TypeIcon(typeof(IDuration))]
    [UnityEngine.Scripting.Preserve]
    public class OnlyOnceCooldownUnit : Unit, ISkillUnit {
        protected override void Definition() {
            ValueOutput("cooldown", _ => new OnlyOnceCooldownUnit());
        }
    }
}