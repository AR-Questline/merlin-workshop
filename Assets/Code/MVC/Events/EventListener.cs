using System;

namespace Awaken.TG.MVC.Events {
    public class EventListener<T> : IEventListener {
        public IListenerOwner Owner { get; }
        public EventSelector Selector { get; }
        public bool IsModal { get; }

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
    }
}