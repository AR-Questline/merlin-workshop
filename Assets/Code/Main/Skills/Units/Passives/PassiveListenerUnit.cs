using System.Collections.Generic;
using System.Linq;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.VisualScripts.Units.Utils;
using Awaken.Utility.Collections;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Passives {
    public abstract class PassiveListenerUnit : PassiveUnit, IGraphElementWithData {

        ControlOutput _trigger;

        protected override void Definition() {
            _trigger = ControlOutput("trigger");
        }

        protected void Trigger(AutoDisposableFlow flow) {
            SafeGraph.Run(flow, _trigger);
        }
        protected void Trigger(GraphPointer pointer) {
            var reference = pointer.GetElementData<Data>(this).reference;
            var flow = AutoDisposableFlow.New(reference);
            SafeGraph.Run(flow, _trigger);
        }

        public override void Enable(Skill skill, Flow flow) {
            var data = flow.stack.GetElementData<Data>(this);
            data.listeners = Listeners(skill, flow).ToArray();
            data.reference = flow.stack.AsReference();
        }
        public override void Disable(Skill skill, Flow flow) {
            var data = flow.stack.GetElementData<Data>(this);
            if (data.listeners != null) {
                data.listeners.ForEach(l => World.EventSystem.RemoveListener(l));
                data.listeners = null;
                data.reference = null;
            }
        }
        protected abstract IEnumerable<IEventListener> Listeners(Skill skill, Flow flow);

        IGraphElementData IGraphElementWithData.CreateData() {
            return new Data();
        }

        class Data : IGraphElementData {
            public IEventListener[] listeners;
            public GraphReference reference;
        }
    }
}