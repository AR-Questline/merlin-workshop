using Awaken.TG.Main.Skills.Units.Passives;
using Unity.VisualScripting;

namespace Awaken.TG.Editor.VisualScripting.Skills {
    [Widget(typeof(IPassiveUnit))]
    public class PassiveUnitWidget : UnitWidget<IPassiveUnit> {
        public PassiveUnitWidget(FlowCanvas canvas, IPassiveUnit unit) : base(canvas, unit) { }

        protected override NodeColorMix baseColor => new() {
            gray = 0.45F,
            teal = 1F,
        };
    }
}