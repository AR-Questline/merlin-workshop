using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Utility.Skills;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.VisualScripts.Units.Listeners.Contexts;
using Awaken.TG.VisualScripts.Units.Listeners.Events;
using Awaken.Utility.Debugging;
using Unity.VisualScripting;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.VisualScripts.Units.Listeners {
    [UnitCategory("AR/General/Events/Listeners")]
    [TypeIcon(typeof(CustomEvent))]
    public abstract class GraphListener : ARUnit, IGraphElementWithData {
        ARValueInput<IListenerContext> _context;
        ARValueInput<IGraphEvent> _event;

        public override bool isControlRoot => true;

        protected override void Definition() {
            _context = FallbackARValueInput("context", DefaultContext);
            _event = RequiredARValueInput<IGraphEvent>("event");
        }

        protected void StartListening(GraphStack stack) {
            var data = stack.GetElementData<Data>(this);
            if (data.listeners.Any()) {
                Log.Important?.Error("Cannot start listening while other listening is in progress", stack.self);
            } else {
                using var flow = Flow.New(stack.ToReference());
                data.listeners.AddRange(_event.Value(flow).CreateListeners(_context.Value(flow), stack));
            }
        }

        protected void StopListening(GraphStack stack) {
            var data = stack.GetElementData<Data>(this);
            foreach (var listener in data.listeners) {
                World.EventSystem.RemoveListener(listener);
            }
            data.listeners.Clear();
        }

        public bool IsListening(GraphPointer pointer) {
            return pointer.GetElementData<Data>(this).listeners != null;
        }

        IListenerContext DefaultContext(Flow flow) {
            return flow.stack.machine switch {
                ScriptMachineWithSkill withSkill => new SkillContext(withSkill.Owner),
                _ => MissingContext(flow)
            };
            
            static IListenerContext MissingContext(Flow flow) {
                Log.Important?.Error("Missing Context!", flow.stack.self);
                throw new Exception("That listener must have specific context. See error above.");
            }
        }

        public IGraphElementData CreateData() => new Data();
        class Data : IGraphElementData {
            public List<IEventListener> listeners = new();
        }
    }
}
