namespace Awaken.TG.MVC.UI.Handlers.Targets {
    /// <summary>
    /// Marker interface for views that can be targeted.
    /// Right-clicking on a view implementing this interface will automatically
    /// target the model.
    /// </summary>
    public interface ITargetableView : IUIAware, IView {
    }
}