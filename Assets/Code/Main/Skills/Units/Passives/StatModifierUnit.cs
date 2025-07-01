using System;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Tweaks;
using Awaken.TG.Main.Skills.Passives;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Passives {
    [UnitCategory("AR/Skills/Passives")]
    [TypeIcon(typeof(FlowGraph))]
    public class StatModifierUnit : PassiveSpawnerUnit {

        [Serialize, Inspectable, UnitHeaderInspectable]
        public TweakPriority tweakType;

        protected ValueInput _stat;
        protected ValueInput _value;
        
        protected override void Definition() {
            var refresh = ControlInput("Refresh", flow => {
                Refresh(this.Skill(flow), flow);
                return null;
            });
            _stat = ValueInput<Stat>("stat");
            _value = ValueInput<float>("value");
        }

        protected override IPassiveEffect Passive(Skill skill, Flow flow) {
            var stat = flow.GetValue<Stat>(_stat);
            if (stat == null) return null;
            var value = flow.GetValue<float>(_value);
            return new PassiveStatModifier(stat, tweakType, value);
        }

        protected override bool IsModified(IPassiveEffect currentPassive, Flow flow, out IPassiveEffect newPassive) {
            if (currentPassive is not PassiveStatModifier statModifier) {
                newPassive = null;
                return false;
            }
            
            var newValue = flow.GetValue<float>(_value);
            var newStat = flow.GetValue<Stat>(_stat);
            if (Math.Abs(statModifier.Value - newValue) < 0.01f && statModifier.Stat == newStat) {
                newPassive = null;
                return false;
            }

            newPassive = new PassiveStatModifier(newStat, tweakType, newValue);
            return true;
        }
    }
}