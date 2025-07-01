namespace Awaken.TG.MVC.Events {
    public interface IDisposableEventListener : IEventListener {
        bool ShouldBeDisposed { get; }
    }
}