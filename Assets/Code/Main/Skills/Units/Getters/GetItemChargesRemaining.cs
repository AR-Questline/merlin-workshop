using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Getters {
    [UnitCategory("AR/Skills/Getters")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class GetItemChargesRemaining : ARUnit, ISkillUnit {
        protected override void Definition() {
            ValueOutput<bool>("hasCharges", flow => {
                Skill skill = this.Skill(flow);
                if (skill.SourceItem == null) {
                    return false;
                }
                return skill.SourceItem.HasCharges;
            });
            
            ValueOutput<int>("chargesRemaining", flow => {
                Skill skill = this.Skill(flow);
                if (skill.SourceItem == null) {
                    return -1;
                }
                if (!skill.SourceItem.HasCharges) {
                    return -1;
                }

                return skill.SourceItem.Element<IItemWithCharges>().ChargesRemaining;
            });
        }
    }
}