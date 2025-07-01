using Awaken.TG.VisualScripts.Units;
using Awaken.TG.VisualScripts.Units.Items;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Variables {
    [UnitCategory("AR/Skills/Variables")]
    [TypeIcon(typeof(FlowGraph))]
    [UnitTitle("Skill Computable With Gain")]
    [UnityEngine.Scripting.Preserve]
    public class SkillComputableWithGainUnit : SkillComputableUnit {
        InlineValueInput<float> _gainPerLevel;

        protected override void Definition() {
            base.Definition();
            _gainPerLevel = InlineARValueInput("Gain", 0f);
        }

        protected override float GetValue(Flow flow) {
            float gain = _gainPerLevel.Value(flow) * GetItemLevelUnit.Get(this, flow);
            return base.GetValue(flow) + gain;
        }
    }
}