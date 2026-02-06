using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.VisualScripts.Units.Utils;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel.Collections;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Passives {
    public abstract class PassiveListenerUnit : PassiveUnit, IGraphElementWithData {

        ControlOutput _trigger;

        protected override void Definition() {
            _trigger = ControlOutput("trigger");
        }

        protected void Trigger(AutoDisposableFlow flow, uint id) {
            var data = flow.flow.stack.GetElementData<Data>(this);
            if (data.listenersFlowInProgress.HasOne(id)) {
                Log.Minor?.Warning($"Tried to re-enter passive listener {flow.flow.stack.AsReference().serializedObject}. This is not allowed and will be ignored.");
                return;
            }
            
            data.listenersFlowInProgress[id] = true;
            SafeGraph.Run(flow, _trigger);
            if (data.listenersFlowInProgress.IsCreated) { // Check if disable was triggered during the flow run
                data.listenersFlowInProgress[id] = false;
            }
        }
        protected void Trigger(GraphPointer pointer, uint id) {
            var reference = pointer.GetElementData<Data>(this).reference;
            var flow = AutoDisposableFlow.New(reference);
            Trigger(flow, id);
        }

        public override void Enable(Skill skill, Flow flow) {
            var data = flow.stack.GetElementData<Data>(this);
            data.listeners = Listeners(skill, flow).ToArray();
            data.listenersFlowInProgress = new UnsafeBitmask((uint) data.listeners.Length, ARAlloc.Persistent);
            data.reference = flow.stack.AsReference();
        }
        
        public override void Disable(Skill skill, Flow flow) {
            var data = flow.stack.GetElementData<Data>(this);
            if (data.listeners != null) {
                data.listeners.ForEach(l => World.EventSystem.RemoveListener(l));
                data.listeners = null;
                data.listenersFlowInProgress.Dispose();
                data.reference = null;
            }
        }
        protected abstract IEnumerable<IEventListener> Listeners(Skill skill, Flow flow);

        IGraphElementData IGraphElementWithData.CreateData() {
            return new Data();
        }

        class Data : IGraphElementData {
            public IEventListener[] listeners;
            public UnsafeBitmask listenersFlowInProgress;
            public GraphReference reference;
        }
    }
}