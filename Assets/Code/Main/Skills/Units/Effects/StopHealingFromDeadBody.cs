using Awaken.TG.VisualScripts.Units;

namespace Awaken.TG.Main.Skills.Units.Effects {
    /// <summary>
    /// This unit is used stricly by VisualScrpting with skill that is responsible for healing player from dead body. Hence the name.
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public class StopHealingFromDeadBody : ARUnit, ISkillUnit {
        protected override void Definition() {
            var character = FallbackARValueInput("character", flow => this.Skill(flow).Owner);
            DefineSimpleAction(flow => {
                HealFromDeadBodies healFromDeadBodies = character.Value(flow).Elements<HealFromDeadBodies>()
                    .FirstOrDefault(h => h.Skill == this.Skill(flow));
                healFromDeadBodies?.Discard();
            });
        }
    }
}