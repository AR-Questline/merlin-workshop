using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Skills.Passives;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Passives {
    [UnitCategory("AR/Skills/Passives")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class StatOverTimeUnit : PassiveSpawnerUnit {
        ARValueInput<Stat> _stat;
        ARValueInput<float> _changePerSecond;
        ARValueInput<Stat> _statModifier;
        
        protected override void Definition() {
            _stat = RequiredARValueInput<Stat>("stat");
            _changePerSecond = InlineARValueInput("Change Per Sec", 0f);
            _statModifier = OptionalARValueInput<Stat>("modifier stat");
        }

        protected override IPassiveEffect Passive(Skill skill, Flow flow) {
            var stat = _stat.Value(flow);
            if (stat == null) return null;
            
            var changePerSecond = _changePerSecond.Value(flow);
            var statModifier = _statModifier.Value(flow);
            
            return PassiveStatOverTime.Create(stat, changePerSecond, statModifier);
        }
    }
}