using Awaken.TG.Main.Heroes.Items;
using Awaken.Utility.Debugging;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units {
    [UnitCategory("AR/Skills")]
    [TypeIcon(typeof(FlowGraph))]
    public class GetSkillItemUnit : Unit, ISkillUnit {
        protected override void Definition() {
            ValueOutput("item", Item);
        }

        Item Item(Flow flow) {
            return GetSkillItem(flow, this);
        }
        
        public static Item GetSkillItem(Flow flow, ISkillUnit skillUnit) {
            var skill = skillUnit.Skill(flow);
            if (skill?.ParentModel is IItemSkillOwner itemSkillOwner) {
                return itemSkillOwner.Item;
            } else {
                Log.Important?.Error($"{skill} is not item's skill", skill.Graph);
                return null;
            }
        }
    }
}