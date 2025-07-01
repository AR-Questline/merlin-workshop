using Awaken.TG.Main.AI;
using Awaken.TG.Main.AI.SummonsAndAllies;
using Awaken.TG.Main.Character;
using Awaken.TG.MVC;
using Awaken.TG.VisualScripts.Units;
using Awaken.Utility.Debugging;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class SetSummonTargetUnit : ARUnit, ISkillUnit {
        protected override void Definition() {
            var targetInput = FallbackARValueInput<ICharacter>("target", _ => null);
            var targetOverridePriority = FallbackARValueInput("targetOverridePriority", _ => 0);

            DefineSimpleAction("Enter", "Exit", flow => {
                ICharacter target = targetInput.Value(flow);
                if (target == null) {
                    Log.Minor?.Error($"Tried to set target for allies owned by {this.Skill(flow)} but target is null");
                    return;
                }

                int priority = targetOverridePriority.Value(flow);
                var item = this.Skill(flow).SourceItem;
                foreach (var heroSummon in World.All<NpcHeroSummon>()) {
                    if (heroSummon.Item == item) {
                        HeroSummonTargetOverride.AddSummonTargetOverrideElement(heroSummon, target, priority);
                    }
                }
            });
        }
    }
}