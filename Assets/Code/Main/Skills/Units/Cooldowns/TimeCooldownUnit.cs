using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Skills.Cooldowns;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Cooldowns {
    [UnitCategory("AR/Skills/Costs")]
    [TypeIcon(typeof(IDuration))]
    public class TimeCooldownUnit : Unit, ISkillUnit {

        ValueInput _time;
        [UnityEngine.Scripting.Preserve] ValueOutput _cooldown;
        
        protected override void Definition() {
            _time = ValueInput("time", 1f);
            _cooldown = ValueOutput("cooldown", Cooldown);
        }

        ISkillCooldown Cooldown(Flow flow) {
            return new SkillTimeCooldown(flow.GetValue<float>(_time));
        }
    }
}