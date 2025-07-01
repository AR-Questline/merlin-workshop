using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class StopHighlightingDeadBodies : ARUnit, ISkillUnit {
        protected override void Definition() {
            DefineSimpleAction("Enter", "Exit", f => {
                this.Skill(f).RemoveElementsOfType<DeadBodiesHighlight>();
            });
        }
    }
}