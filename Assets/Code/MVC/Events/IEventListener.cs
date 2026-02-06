namespace Awaken.TG.MVC.Events {
    public interface IEventListener {
        IListenerOwner Owner { get; }
        EventSelector Selector { get; }
        bool IsModal { get; }
        bool ShouldBeDisposed { get; }

        void InvokeWith(object payload);
    }

    public interface IEventListener<in T> : IEventListener {
        void InvokeWith(T payload);
    }
}