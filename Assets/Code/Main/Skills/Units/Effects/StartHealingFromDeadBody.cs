using Awaken.TG.VisualScripts.Units;

namespace Awaken.TG.Main.Skills.Units.Effects {
    /// <summary>
    /// This unit is used stricly by VisualScrpting with skill that is responsible for healing player from dead body. Hence the name.
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public class StartHealingFromDeadBody : ARUnit, ISkillUnit {
        protected override void Definition() {
            var character = FallbackARValueInput("character", flow => this.Skill(flow).Owner);
            var manaCostPerTick = FallbackARValueInput("manaCostPerTick", _ => 5f);
            var range = FallbackARValueInput("range", _ => 5f);
            DefineSimpleAction(flow => {
                character.Value(flow).AddElement(new HealFromDeadBodies(this.Skill(flow), manaCostPerTick.Value(flow), range.Value(flow)));
            });
        }
    }
}