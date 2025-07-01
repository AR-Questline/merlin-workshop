using Awaken.TG.Main.Skills.Passives;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Passives {
    [UnitCategory("AR/Skills/Passives")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class StatusSpreadUnit : PassiveSpawnerUnit {
        ARValueInput<float> _chance;
        ARValueInput<float> _radius;
        
        protected override void Definition() {
            _chance = InlineARValueInput("chance", 0f);
            _radius = InlineARValueInput("radius", 1f);
        }

        protected override IPassiveEffect Passive(Skill skill, Flow flow) {
            var chance = _chance.Value(flow);
            var radius = _radius.Value(flow);

            return new PassiveStatusSpread(chance, radius);
        }
    }
}