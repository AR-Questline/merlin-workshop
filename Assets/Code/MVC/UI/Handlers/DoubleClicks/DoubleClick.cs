using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI.Events;
using UnityEngine;

namespace Awaken.TG.MVC.UI.Handlers.DoubleClicks {
    public partial class DoubleClick : Element<GameUI>, ISmartHandler {
        // Max time between clicks to do double-click
        const float DoubleClickTimeout = 0.2f;

        public sealed override bool IsNotSaved => true;

        // === States

        int _lastMouseButton;
        float _clickTime;
        IUIAware _mouseDownHandler;

        // === ISmartHandler implementation

        public UIResult BeforeDelivery(UIEvent evt) {
            if (evt is UIEMouseDown mouseDown) {
                // Second click in according time span
                if (mouseDown.Button == _lastMouseButton && (Time.realtimeSinceStartup - _clickTime) < DoubleClickTimeout) {
                    var delivery = ParentModel.HandleMouseDoubleClick(mouseDown);
                    _lastMouseButton = int.MinValue; // Just random number
                    _clickTime = -1;
                    // Save reference to model that handled double click to assign him in mouseDown
                    if (delivery.finalResult != UIResult.Ignore) {
                        _mouseDownHandler = delivery.responsibleObject;
                    }
                    return delivery.finalResult;
                }
                // Update state if first click or nth click but performed not in proper time span
                _lastMouseButton = mouseDown.Button;
                _clickTime = Time.realtimeSinceStartup;
                return UIResult.Ignore;
            }

            return UIResult.Ignore;
        }

        public UIResult BeforeHandlingBy(IUIAware handler, UIEvent evt) => UIResult.Ignore;

        public UIResult AfterHandlingBy(IUIAware handler, UIEvent evt) => UIResult.Ignore;

        public UIEventDelivery AfterDelivery(UIEventDelivery delivery) {
            // Fallback handler to mouseDown
            if (delivery.handledEvent is UIEMouseDown && _mouseDownHandler != null) {
                delivery.responsibleObject = _mouseDownHandler;
                _mouseDownHandler = null;
            }

            return delivery;
        }
    }
}
