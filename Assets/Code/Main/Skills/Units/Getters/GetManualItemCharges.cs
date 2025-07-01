using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.VisualScripts.Units;

namespace Awaken.TG.Main.Skills.Units.Getters {
    [UnityEngine.Scripting.Preserve]
    public class GetManualItemCharges : ARUnit, ISkillUnit {
        protected override void Definition() {
            ValueOutput<ManualItemCharges>("manualItemCharges", flow => {
                Skill skill = this.Skill(flow);
                if (skill.SourceItem == null) {
                    return null;
                }
                
                return skill.SourceItem.TryGetElement<ManualItemCharges>();
            });
        }
    }
}