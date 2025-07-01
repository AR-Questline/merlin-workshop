using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI.Events;

namespace Awaken.TG.MVC.UI.Handlers
{
    /// <summary>
    /// Smart handlers can trigger for any IUIAware, check some additional conditions
    /// and optionally handle an event on behalf of the handler. This is used to
    /// implement cross-cutting functionality, like tooltips or selection/ordering.
    /// </summary>
    public interface ISmartHandler : IElement<GameUI> {
        UIResult BeforeDelivery(UIEvent evt);
        UIResult BeforeHandlingBy(IUIAware handler, UIEvent evt);
        UIResult AfterHandlingBy(IUIAware handler, UIEvent evt);
        UIEventDelivery AfterDelivery(UIEventDelivery delivery);
    }
}