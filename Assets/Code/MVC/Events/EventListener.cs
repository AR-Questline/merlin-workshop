using System;

namespace Awaken.TG.MVC.Events {
    public class EventListener<T> : IEventListener<T> {
        public IListenerOwner Owner { get; }
        public EventSelector Selector { get; }
        public bool IsModal { get; }
        public bool ShouldBeDisposed => false;

        readonly Action<T> _callback;

        public EventListener(Action<T> callback, IListenerOwner owner, EventSelector selector, bool isModal = false) {
            _callback = callback;
            Owner = owner;
            Selector = selector;
            IsModal = isModal;
        }

        public void InvokeWith(object payload) {
            _callback((T) payload);
        }

        public void InvokeWith(T payload) {
            _callback(payload);
        }
    }
}