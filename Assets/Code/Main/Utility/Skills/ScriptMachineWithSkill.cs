using System;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC.Events;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Utility.Skills {
    public class ScriptMachineWithSkill : EventMachineUpdateless<FlowGraph, ScriptGraphAsset>, IListenerOwner {
        public Skill Owner { get; set; }
        
        public void Initialize() {
            if (hasGraph) {
                StartListening();
            }
        }

        public void Discard() {
            if (hasGraph) {
                StopListening();
            }
        }

        public override FlowGraph DefaultGraph() {
            return FlowGraph.WithStartUpdate();
        }

        void StartListening() {
            using (var stack = reference.ToStackPooled()) {
                foreach (var unit in graph.units) {
                    if (unit is IGraphEventListener listener) {
                        if (unit is EventUnit<CustomEventArgs> eventUnit) {
                            var data = stack.GetElementData<EventUnit<CustomEventArgs>.Data>(eventUnit);
                            if (data.isListening) {
                                continue;
                            }
                            
                            var hook = eventUnit.GetHook(reference);
                            hook = new EventHook(hook.name, this, hook.tag);
                            Action<CustomEventArgs> handler = args => eventUnit.Trigger(reference, args);
                            EventBus.Register(hook, handler);
                            
                            data.hook = hook;
                            data.handler = handler;
                            data.isListening = true;
                            continue;
                        }
                        listener.StartListening(stack);
                    }
                }
            }
        }

        void StopListening() {
            using (var stack = reference.ToStackPooled()) {
                foreach (var unit in graph.units) {
                    if (unit is IGraphEventListener listener) {
                        if (unit is EventUnit<CustomEventArgs> eventUnit) {
                            var data = stack.GetElementData<EventUnit<CustomEventArgs>.Data>(eventUnit);
                            if (!data.isListening) {
                                return;
                            }

                            // The coroutine's flow will dispose at the next frame, letting us
                            // keep the current flow for clean up operations if needed
                            foreach (var activeCoroutine in data.activeCoroutines) {
                                activeCoroutine.StopCoroutine(false);
                            }

                            EventBus.Unregister(data.hook, data.handler);
                            data.handler = null;
                            data.isListening = false;
                            continue;
                        }
                        listener.StopListening(stack);
                    }
                }
                stack.ClearReferencePublic();
            }
        }
    }
}