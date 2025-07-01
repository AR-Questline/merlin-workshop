namespace Awaken.TG.MVC.Events {
    public interface IEventListener {
        IListenerOwner Owner { get; }
        EventSelector Selector { get; }
        bool IsModal { get; }

        void InvokeWith(object payload);
    }
}