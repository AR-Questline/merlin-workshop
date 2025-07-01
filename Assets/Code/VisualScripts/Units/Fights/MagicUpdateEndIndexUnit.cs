using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Magic;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Skills.Units;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;

namespace Awaken.TG.VisualScripts.Units.Fights {
    [UnityEngine.Scripting.Preserve]
    public class MagicUpdateEndIndexUnit : ARUnit, ISkillUnit{
        protected override void Definition() {
            var inItem = FallbackARValueInput("Item", flow => GetSkillItemUnit.GetSkillItem(flow, this));
            var performState = FallbackARValueInput("PerformState", _ => MagicEndState.MagicEnd);

            DefineSimpleAction("Enter", "Exit", flow => {
                Item item = inItem.Value(flow);
                if (item == null) {
                    Log.Minor?.Error("MagicUpdateEndIndexUnit failed! Item is null", this.Skill(flow)?.Graph);
                    return;
                }
                item.Trigger(MagicFSM.Events.OverrideEndCastingIndex, performState.Value(flow));
            });
        }
    }
}