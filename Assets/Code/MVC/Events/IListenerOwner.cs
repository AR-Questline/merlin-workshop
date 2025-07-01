namespace Awaken.TG.MVC.Events
{
    /// <summary>
    /// Marker interface that an object has to implement in order to be allowed to
    /// own event listeners. Owning event listeners only makes sense for objects
    /// whose lifecycle we control. For this reason, currently listening is only allowed for:
    /// Model, View, ViewComponent, Presenter, IService classes.
    /// </summary>
    public interface IListenerOwner {
        bool CanReceiveEvents => true;
    }
}
