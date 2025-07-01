using System.Collections.Generic;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.VisualScripts.Units.Utils;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Passives {
    public abstract class PassiveListenerWithPayloadUnit<TSource, TPayload> : PassiveListenerUnit where TSource : class, IModel {

        ValueOutput _data;

        protected override void Definition() {
            base.Definition();
            _data = ValueOutput<TPayload>(DataName);
        }

        protected override IEnumerable<IEventListener> Listeners(Skill skill, Flow flow) {
            var reference = flow.stack.AsReference();
            yield return Source(skill, flow).ListenTo(Event(skill, flow), payload => Trigger(reference, payload), skill);
        }

        protected virtual string DataName => nameof(TPayload);
        protected abstract TSource Source(Skill skill, Flow flow);
        protected abstract Event<TSource, TPayload> Event(Skill skill, Flow flow);

        protected virtual void Trigger(GraphReference reference, TPayload payload) {
            var flow = AutoDisposableFlow.New(reference);
            flow.flow.SetValue(_data, payload);
            Trigger(flow);
        }
    }
}