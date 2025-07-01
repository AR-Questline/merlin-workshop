using System;
using Awaken.TG.MVC.Events;
using JetBrains.Annotations;

namespace Awaken.TG.MVC.Utils {
    public struct MutableListenerWrapper<TSource, TPayload> where TSource : class, IEventSource, IModel {
        readonly Event<TSource, TPayload> _event;

        TSource _source;
        Action<TPayload> _action;
        IListenerOwner _owner;

        IEventListener _listener;

        public MutableListenerWrapper([NotNull] Event<TSource, TPayload> @event) : this() {
            _event = @event;
        }

        public void Setup(TSource source, Action<TPayload> action, IListenerOwner owner) {
            _source = source;
            _action = action;
            _owner = owner;
            Refresh();
        }
        
        public void ChangeSource(TSource source) {
            if (_source == source) return;
            _source = source;
            Refresh();
        }

        [UnityEngine.Scripting.Preserve]
        public void ChangeAction(Action<TPayload> action) {
            if (_action == action) return;
            _action = action;
            Refresh();
        }
        
        [UnityEngine.Scripting.Preserve]
        public void ChangeOwner(IListenerOwner owner) {
            if (_owner == owner) return;
            _owner = owner;
            Refresh();
        }

        public void Reset() {
            if (_listener != null) {
                World.EventSystem.RemoveListener(_listener);
                _listener = null;
            }
        }

        void Refresh() {
            Reset();
            if (_action != null) {
                _listener = _source?.ListenTo(_event, _action, _owner);
            }
        }
    }
}