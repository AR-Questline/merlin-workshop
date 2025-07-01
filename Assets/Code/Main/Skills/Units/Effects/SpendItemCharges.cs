using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.VisualScripts.Units;
using Awaken.Utility.Debugging;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class SpendItemCharges : ARUnit, ISkillUnit {
        protected override void Definition() {
            var chargesSpent = InlineARValueInput("chargesSpent", 1);

            DefineSimpleAction(
                flow => {
                    Skill skill = this.Skill(flow);
                    if (skill.SourceItem == null) {
                        Log.Minor?.Error("Skill has no source item");
                        return;
                    }
                    if (!skill.SourceItem.HasCharges) {
                        Log.Minor?.Error("Source item has no charges: " + skill.SourceItem.ContextID);
                        return;
                    }

                    skill.SourceItem.Element<IItemWithCharges>().SpendCharges(chargesSpent.Value(flow));
                });
        }
    }
}
