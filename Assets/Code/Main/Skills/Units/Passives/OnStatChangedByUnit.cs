using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.VisualScripts.Units.Utils;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Passives {
    [UnitCategory("AR/Skills/Passives")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class OnStatChangedByUnit : PassiveListenerUnit {

        ValueInput _statType;
        ValueOutput _changeValue;
        
        protected override void Definition() {
            base.Definition();
            _statType = ValueInput<StatType>("type");
            _changeValue = ValueOutput<float>("changeValue");
        }

        protected override IEnumerable<IEventListener> Listeners(Skill skill, Flow flow) {
            var reference = flow.stack.AsReference();
            var statType = flow.GetValue<StatType>(_statType);
            var stat = SkillRole.RetrieveStatFrom(skill, statType);
            yield return stat.Owner.ListenTo(Stat.Events.StatChangedBy(statType), s => {
                var f = AutoDisposableFlow.New(reference);
                f.flow.SetValue(_changeValue, s.value);
                Trigger(f);
            }, skill);
        }
    }
}