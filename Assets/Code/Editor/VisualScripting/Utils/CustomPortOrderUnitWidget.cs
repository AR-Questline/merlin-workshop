using System.Linq;
using Awaken.TG.VisualScripts.Units.Utils;
using Unity.VisualScripting;

namespace Awaken.TG.Editor.VisualScripting.Utils {
    [Widget(typeof(ICustomPortOrderUnit))]
    public class CustomPortOrderUnitWidget : UnitWidget<ICustomPortOrderUnit> {
        public CustomPortOrderUnitWidget(FlowCanvas canvas, ICustomPortOrderUnit unit) : base(canvas, unit) { }
        
        protected override void CacheDefinition() {
            inputs.Clear();
            outputs.Clear();
            ports.Clear();
            inputs.AddRange(unit.CheckedOrderedInputs.Select(port => canvas.Widget<IUnitPortWidget>(port)));
            outputs.AddRange(unit.CheckedOrdererOutputs.Select(port => canvas.Widget<IUnitPortWidget>(port)));
            ports.AddRange(inputs);
            ports.AddRange(outputs);

            Reposition();
        }
    }
    
    [Widget(typeof(ICustomInputOrderUnit))]
    public class CustomInputOrderUnitWidget : UnitWidget<ICustomInputOrderUnit> {
        public CustomInputOrderUnitWidget(FlowCanvas canvas, ICustomInputOrderUnit unit) : base(canvas, unit) { }
        
        protected override void CacheDefinition() {
            inputs.Clear();
            outputs.Clear();
            ports.Clear();
            inputs.AddRange(unit.CheckedOrderedInputs.Select(port => canvas.Widget<IUnitPortWidget>(port)));
            outputs.AddRange(unit.outputs.Select(port => canvas.Widget<IUnitPortWidget>(port)));
            ports.AddRange(inputs);
            ports.AddRange(outputs);

            Reposition();
        }
    }
    
    [Widget(typeof(ICustomOutputOrderUnit))]
    public class CustomOutputOrderUnitWidget : UnitWidget<ICustomOutputOrderUnit> {
        public CustomOutputOrderUnitWidget(FlowCanvas canvas, ICustomOutputOrderUnit unit) : base(canvas, unit) { }

        protected override void CacheDefinition() {
            inputs.Clear();
            outputs.Clear();
            ports.Clear();
            inputs.AddRange(unit.inputs.Select(port => canvas.Widget<IUnitPortWidget>(port)));
            outputs.AddRange(unit.CheckedOrdererOutputs.Select(port => canvas.Widget<IUnitPortWidget>(port)));
            ports.AddRange(inputs);
            ports.AddRange(outputs);
        
            Reposition();
        }
    }
}