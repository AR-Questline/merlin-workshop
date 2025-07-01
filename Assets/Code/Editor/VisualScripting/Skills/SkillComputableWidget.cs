using Awaken.TG.Main.Skills.Units.Variables;
using Unity.VisualScripting;

namespace Awaken.TG.Editor.VisualScripting.Skills {
    [Widget(typeof(SkillComputableUnit))]
    public class SkillComputableWidget : UnitWidget<SkillComputableUnit> {
        public SkillComputableWidget(FlowCanvas canvas, SkillComputableUnit unit) : base(canvas, unit) { }

        protected override NodeColorMix baseColor => NodeColor.Green;
    }
}