using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class StartHighlightingDeadBodies : ARUnit, ISkillUnit {
        FallbackValueInput<float> _range;
        
        protected override void Definition() {
            _range = FallbackARValueInput("range", _ => 10f);
            
            DefineSimpleAction("Enter", "Exit", f => {
                this.Skill(f).AddElement(new DeadBodiesHighlight(_range.Value(f)));
            });
        }
    }
}