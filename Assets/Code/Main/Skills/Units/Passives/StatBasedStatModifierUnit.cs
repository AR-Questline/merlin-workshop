using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Skills.Passives;
using Awaken.TG.Main.Utility.Skills;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Passives {
    [UnitCategory("AR/Skills/Passives")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class StatBasedStatModifierUnit : StatModifierUnit {
        ValueInput _statToListenTo;

        protected override void Definition() {
            _statToListenTo = ValueInput<Stat>("statToListenTo");
            _stat = ValueInput<Stat>("stat");
            _value = ValueInput<float>("value");
        }
        
        protected override IPassiveEffect Passive(Skill skill, Flow flow) {
            var stat = flow.GetValue<Stat>(_stat);
            if (stat == null) return null;
            var value = flow.GetValue<float>(_value);
            return new PassiveStatModifierWithListener(stat, tweakType, value);
        }

        public override void Enable(Skill skill, Flow flow) {
            base.Enable(skill, flow);
            var statToListenTo = flow.GetValue<Stat>(_statToListenTo);
            var data = flow.stack.GetElementData<Data>(this);
            var machine = ((ScriptMachineWithSkill)flow.stack.machine).GetReference().AsReference();
            if (data.passive is PassiveStatModifierWithListener statModifier) {
                statModifier.listener = statToListenTo.Owner.ListenTo(Stat.Events.StatChanged(statToListenTo.Type), _ => { Refresh(skill, Flow.New(machine)); }, skill);
            }
        }
        
        public override void Disable(Skill skill, Flow flow) {
            var data = flow.stack.GetElementData<Data>(this);
            if (data.passive is PassiveStatModifierWithListener statModifier) {
                World.EventSystem.TryDisposeListener(ref statModifier.listener);
            }
            base.Disable(skill, flow);
        }
    }
}