using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class RemoveSlowDownTimeUnit : ARUnit, ISkillUnit {
        string ID(Flow flow) => this.Skill(flow).ID + ApplySlowDownTimeUnit.SlowDownTimeUnitID;

        protected override void Definition() {
            DefineSimpleAction("Enter", "Exit", Enter);
        }
        
        void Enter(Flow flow) {
            TimeDependent timeDependent = this.Skill(flow).Owner.GetTimeDependent();
            timeDependent?.RemoveTimeModifiersFor(ID(flow));
        }
    }
}