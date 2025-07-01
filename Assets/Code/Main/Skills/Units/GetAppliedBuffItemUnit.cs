using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Buffs;
using Awaken.Utility.Debugging;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units {
    [UnitCategory("AR/Skills")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class GetAppliedBuffItemUnit : Unit, ISkillUnit {
        protected override void Definition() {
            ValueOutput("weapon", Item);
        }

        Item Item(Flow flow) {
            var skill = this.Skill(flow);
            if (skill.ParentModel is AppliedItemBuff appliedItemBuff) {
                return appliedItemBuff.ParentModel;
            } else {
                Log.Important?.Error($"{skill} is not attached to AppliedItemBuff", skill.Graph);
                return null;
            }
        }
    }
}