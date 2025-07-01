using Awaken.TG.VisualScripts.Units.General;
using Unity.VisualScripting;

namespace Awaken.TG.Editor.VisualScripting.General {
    [Widget(typeof(SafeGetVariable))]
    public class SafeGetVariableWidget : UnitWidget<SafeGetVariable> {
        public SafeGetVariableWidget(FlowCanvas canvas, SafeGetVariable unit) : base(canvas, unit) { }
        
        protected override NodeColorMix baseColor => NodeColorMix.TealReadable;
    }
}