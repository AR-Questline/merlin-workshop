using System;

namespace Awaken.TG.MVC.Events {
    public class LimitedEventListener<T> : IDisposableEventListener {
        public IListenerOwner Owner { get; }
        public EventSelector Selector { get; }
        public int Charges { get; private set; }
        public bool IsModal { get; private set; }

        public bool ShouldBeDisposed => Charges <= 0;

        readonly Action<T> _callback;

        public LimitedEventListener(Action<T> callback, IListenerOwner owner, EventSelector selector, int charges, bool isModal = false) {
            _callback = callback;
            Owner = owner;
            Selector = selector;
            Charges = charges;
            IsModal = isModal;
        }

        public void InvokeWith(object payload) {
            _callback((T) payload);
            Charges--;
        }
    }
}