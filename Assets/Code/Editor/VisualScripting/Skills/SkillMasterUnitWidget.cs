using Awaken.TG.Main.Skills.Units.Masters;
using Unity.VisualScripting;
using System.Linq;
using Awaken.TG.VisualScripts.Units.Utils;

namespace Awaken.TG.Editor.VisualScripting.Skills {
    [Widget(typeof(SkillMasterUnit))]
    public class SkillMasterUnitWidget : UnitWidget<SkillMasterUnit> {
        public SkillMasterUnitWidget(FlowCanvas canvas, SkillMasterUnit unit) : base(canvas, unit) { }

        protected override NodeColorMix baseColor => NodeColor.Teal;
        
        protected override void CacheDefinition() {
            inputs.Clear();
            outputs.Clear();
            ports.Clear();
            inputs.AddRange(unit.inputs.Select(port => canvas.Widget<IUnitPortWidget>(port)));
            outputs.AddRange(((ICustomOutputOrderUnit)unit).CheckedOrdererOutputs.Select(port => canvas.Widget<IUnitPortWidget>(port)));
            ports.AddRange(inputs);
            ports.AddRange(outputs);
        
            Reposition();
        }
    }
}