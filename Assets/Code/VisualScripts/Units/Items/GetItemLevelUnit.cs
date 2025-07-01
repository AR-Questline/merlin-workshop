using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Skills;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Items {
    [UnitCategory("AR/Skills/Items")]
    [TypeIcon(typeof(FlowGraph))]
    [UnitTitle("Get Item Level")]
    public class GetItemLevelUnit : Unit, ISkillUnit {
        protected override void Definition() {
            ValueOutput("level", flow => Get(this, flow));
        }

        public static int Get(ISkillUnit unit, Flow flow) {
            ItemEffects itemEffects = unit.Skill(flow).ParentModel as ItemEffects;
            Item item = itemEffects?.Item;
            return item?.Level.ModifiedInt ?? 0;
        }
    }
}