using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Events;

namespace Awaken.TG.MVC.UI.Handlers.Hovers {
    /// <summary>
    /// Handles hovering over views.
    /// </summary>
    public partial class Hovering : Element<GameUI>, ISmartHandler {
        public sealed override bool IsNotSaved => true;

        // === State
        public View HoveredView { get; private set; }
        public static bool IsHovered(View view) => World.Only<Hovering>().HoveredView == view;

        // === Events

        public new class Events {
            public static readonly Event<IView, HoverChange> HoverChanged = new(nameof(HoverChanged));
        }

        // === Operations
        
        public void ChangeHoverTo(object view) {
            if (view != (object)HoveredView) {
                View oldHovered = HoveredView;
                HoveredView = view as View;
                oldHovered?.Trigger(Events.HoverChanged, new HoverChange(oldHovered, false));
                HoveredView?.Trigger(Events.HoverChanged, new HoverChange(HoveredView, true));
            }
        }

        // === Event handling

        public UIResult BeforeDelivery(UIEvent evt) => UIResult.Ignore;
        public UIResult BeforeHandlingBy(IUIAware handler, UIEvent evt) => UIResult.Ignore;

        public UIResult AfterHandlingBy(IUIAware handler, UIEvent evt) {
            if (handler is IHoverableView && evt is UIEPointTo) return UIResult.Accept;
            return UIResult.Ignore;
        }

        public UIEventDelivery AfterDelivery(UIEventDelivery delivery) {
            if (delivery.handledEvent is UIEPointTo) {
                ChangeHoverTo(delivery.finalResult == UIResult.Accept ? delivery.responsibleObject : null);
            }

            return delivery;
        }
    }
}
