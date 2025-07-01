using System.Linq;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Tooltips {
    [UnitCategory("AR/Skills/Tooltips")]
    [TypeIcon(typeof(FlowGraph))]
    public class TooltipUnit : MultiInputUnit<object>, ISkillUnit {
        [Serialize, Inspectable, UnitHeaderInspectable]
        public string tooltip;

        [UnityEngine.Scripting.Preserve] ValueOutput _output;

        protected override void Definition() {
            base.Definition();

            _output = ValueOutput("tooltip", Tooltip);
        }

        string Tooltip(Flow flow) {
            return string.Format(tooltip, multiInputs.Select(flow.GetValue<object>).ToArray());
        }

        protected override int minInputCount => 0;
    }
}