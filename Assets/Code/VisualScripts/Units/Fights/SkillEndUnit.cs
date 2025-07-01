using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Magic;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Skills.Units;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Fights {
    [UnityEngine.Scripting.Preserve]
    public class SkillEndUnit : ARUnit, ISkillUnit {
        [Serialize, Inspectable, UnitHeaderInspectable]
        public EndType End { get; set; } = EndType.Perform;
        
        protected override void Definition() {
            var inItem = FallbackARValueInput("Item", flow => GetSkillItemUnit.GetSkillItem(flow, this));
            FallbackValueInput<MagicEndState> performState = null;
            if (End == EndType.Perform) {
                performState = FallbackARValueInput("PerformState", _ => MagicEndState.MagicEnd);
            }
            
            DefineSimpleAction("Enter", "Exit", flow => {
                Item item = inItem.Value(flow);
                if (item == null) {
                    Log.Minor?.Error("SkillEndUnit failed! Item is null", this.Skill(flow)?.Graph);
                    return;
                }
                
                switch (End) {
                    case EndType.Perform:
                        var state = performState?.Value(flow) ?? MagicEndState.MagicEnd;
                        item.Trigger(MagicFSM.Events.EndCasting, state);
                        break;
                    case EndType.Cancel:
                        item.Trigger(MagicFSM.Events.CancelCasting, true);
                        break;
                    case EndType.PerformMidCast:
                        item.Trigger(MagicFSM.Events.PerformMidCast, true);
                        break;
                }
            });
        }
        
        public enum EndType : byte {
            Perform = 1,
            Cancel = 2,
            PerformMidCast = 3,
        }
    }
}