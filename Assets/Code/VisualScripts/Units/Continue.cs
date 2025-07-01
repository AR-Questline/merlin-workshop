using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units {
    [UnitCategory("Control")]
    [TypeIcon(typeof(If))]
    public class Continue : Unit{
        ControlInput enter;
        protected override void Definition() {
            enter = ControlInput(nameof(enter), f => null);
        }
    }
}