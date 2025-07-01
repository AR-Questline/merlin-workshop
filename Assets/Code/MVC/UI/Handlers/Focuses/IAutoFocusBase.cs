namespace Awaken.TG.MVC.UI.Handlers.Focuses {
    /// <summary>
    /// Marker interface that informs Focus that this view wants to automatically change the focus base.
    /// If you want something to be Focus Base but not automatically, remember that every view can be a focus base without having to implement anything.
    /// </summary>
    public interface IAutoFocusBase : IView {
    }
}