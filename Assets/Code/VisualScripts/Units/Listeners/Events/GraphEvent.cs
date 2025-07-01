using System.Collections.Generic;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.VisualScripts.Units.Listeners.Contexts;
using Awaken.TG.VisualScripts.Units.Utils;
using Awaken.Utility.Debugging;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Listeners.Events {
    [TypeIcon(typeof(CustomEvent))]
    public interface IGraphEvent {
        IEnumerable<IEventListener> CreateListeners(IListenerContext context, GraphStack stack);
    }

    [UnitCategory("AR/General/Events")]
    public abstract class GraphEventBase<TSource, TPayloadInput, TPayloadOutput> : ARUnit, ICustomOutputOrderUnit, IGraphEvent where TSource : class, IModel {
        
        ValueOutput _data;
        ControlOutput _trigger;
        ValueOutput _payload;
        
        public override bool isControlRoot => _data.hasValidConnection;
        protected abstract TPayloadOutput Payload(TPayloadInput eventValue);

        protected override void Definition() {
            _data = ValueOutput("event", Creator);
            _trigger = ControlOutput("trigger");
            _payload = ValueOutput<TPayloadOutput>("payload");
        }
        IGraphEvent Creator(Flow flow) => this;

        protected abstract IEnumerable<Event<TSource, TPayloadInput>> Events();
        protected abstract IEnumerable<TSource> Sources(IListenerContext context);

        IEnumerable<IEventListener> IGraphEvent.CreateListeners(IListenerContext context, GraphStack stack) {
            var owner = stack.machine as IListenerOwner ?? VGUtils.GetModel(stack.self);
            var reference = stack.AsReference();
            if (context is AnySource) {
                foreach (var evt in Events()) {
                    yield return World.EventSystem.ListenTo(EventSelector.AnySource, evt, owner, payload => Trigger(payload, AutoDisposableFlow.New(reference)));
                }
            } else {
                foreach (var model in Sources(context)) {
                    foreach (var evt in Events()) {
                        if (model == null) {
                            Log.Minor?.Error($"Cannot listen to null model - {stack.self}");
                            continue;
                        }
                        yield return (model as TSource).ListenTo(evt, payload => Trigger(payload, AutoDisposableFlow.New(reference)), owner);
                    }
                }
            }
        }

        protected virtual void Trigger(TPayloadInput payload, AutoDisposableFlow flow) {
            flow.flow.SetValue(_payload, Payload(payload));
            Run(flow);
        }

        void Run(AutoDisposableFlow flow) {
            SafeGraph.Run(flow, _trigger);
        }
        
        public virtual IEnumerable<IUnitOutputPort> OrderedOutputs {
            get {
                yield return _data;
                yield return _trigger;
                yield return _payload;
            }
        }
    }

    // == Unary
    
    public abstract class GraphEvent<TSource, TPayloadInput, TPayloadOutput> : GraphEventBase<TSource, TPayloadInput, TPayloadOutput> where TSource : class, IModel {
        protected override IEnumerable<Event<TSource, TPayloadInput>> Events() => Event.Yield();
        protected abstract Event<TSource, TPayloadInput> Event { get; }

        protected override IEnumerable<TSource> Sources(IListenerContext context) => Source(context).Yield();
        protected abstract TSource Source(IListenerContext context);
    }
    
    public abstract class GraphHookableEvent<TSource, TPayloadOutput> 
        : GraphEventBase<TSource, HookResult<TSource, TPayloadOutput>, PreventableHook> where TSource : class, IModel {
        
        ValueOutput _hookPayload;
        
        protected override IEnumerable<Event<TSource, HookResult<TSource, TPayloadOutput>>> Events() => Event.Yield();
        protected abstract HookableEvent<TSource, TPayloadOutput> Event { get; }

        protected override IEnumerable<TSource> Sources(IListenerContext context) => Source(context).Yield();
        protected abstract TSource Source(IListenerContext context);
        protected override PreventableHook Payload(HookResult<TSource, TPayloadOutput> eventValue) => eventValue;

        // === Adding hook payload output port
        protected override void Definition() {
            base.Definition();
            _hookPayload = ValueOutput<TPayloadOutput>("hookPayload");
        }

        protected override void Trigger(HookResult<TSource, TPayloadOutput> payload, AutoDisposableFlow flow) {
            flow.flow.SetValue(_hookPayload, payload.Value);
            base.Trigger(payload, flow);
        }

        public override IEnumerable<IUnitOutputPort> OrderedOutputs {
            get {
                foreach (var output in base.OrderedOutputs) {
                    yield return output;
                }
                yield return _hookPayload;
            }
        }
    }

    public abstract class GraphEvent<TSource, TPayload> : GraphEvent<TSource, TPayload, TPayload> where TSource : class, IModel {
        protected override TPayload Payload(TPayload eventValue) => eventValue;
    }


    // == MultiSource
    
    public abstract class MultiSourceGraphEvent<TSource, TPayloadInput, TPayloadOutput> : GraphEventBase<TSource, TPayloadInput, TPayloadOutput> where TSource : class, IModel {
        protected override IEnumerable<Event<TSource, TPayloadInput>> Events() => Event.Yield();
        protected abstract Event<TSource, TPayloadInput> Event { get; }
    }

    [UnityEngine.Scripting.Preserve]
    public abstract class MultiSourceGraphEvent<TSource, TPayload> : MultiSourceGraphEvent<TSource, TPayload, TPayload> where TSource : class, IModel {
        protected override TPayload Payload(TPayload eventValue) => eventValue;
    }

    // == MultiType
    
    public abstract class MultiTypeGraphEvent<TSource, TPayloadInput, TPayloadOutput> : GraphEventBase<TSource, TPayloadInput, TPayloadOutput> where TSource : class, IModel {
        protected override IEnumerable<TSource> Sources(IListenerContext context) => Source(context).Yield();
        protected abstract TSource Source(IListenerContext context);
    }

    [UnityEngine.Scripting.Preserve]
    public abstract class MultiTypeGraphEvent<TSource, TPayload> : MultiTypeGraphEvent<TSource, TPayload, TPayload> where TSource : class, IModel {
        protected override TPayload Payload(TPayload eventValue) => eventValue;
    }
}