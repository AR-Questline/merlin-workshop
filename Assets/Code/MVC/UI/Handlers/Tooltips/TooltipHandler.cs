using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI.Events;
using UnityEngine;

namespace Awaken.TG.MVC.UI.Handlers.Tooltips {
    /// <summary>
    /// Implements the functionality of automatically displaying tooltips
    /// for stuff implementing IWithTooltip.
    /// </summary>
    public partial class TooltipHandler : Element<GameUI>, ISmartHandler {
        public sealed override bool IsNotSaved => true;

        // === State

        IWithTooltip _tooltipSource;
        int _updatedOnFrame;
        bool _hasTooltipSourceWithConstructor;
        bool _isRightClicked;

        IWithTooltip TooltipSource => _updatedOnFrame == Time.frameCount ? _tooltipSource : null;
        IWithTechnicalTooltip TechnicalTooltipSource => TooltipSource as IWithTechnicalTooltip;
        bool ShowTechnicalTooltip => _isRightClicked && TechnicalTooltipSource != null;
        TooltipConstructor TooltipText => ShowTechnicalTooltip ? TechnicalTooltipSource.TechnicalTooltipConstructor : TooltipSource.TooltipConstructor;

        // === Handling UI events for tooltipped views

        public UIResult BeforeDelivery(UIEvent evt) => UIResult.Ignore;
        public UIResult BeforeHandlingBy(IUIAware handler, UIEvent evt) => UIResult.Ignore;

        public UIResult AfterHandlingBy(IUIAware handler, UIEvent evt) {
            if (_hasTooltipSourceWithConstructor == false && handler is IWithTooltip withTooltip && evt is UIEPointTo) {
                _tooltipSource = withTooltip;
                _updatedOnFrame = Time.frameCount;
                if (_tooltipSource.TooltipConstructor != null) {
                    _hasTooltipSourceWithConstructor = true;
                }

                return UIResult.Ignore;
            }

            return UIResult.Ignore;
        }

        public UIEventDelivery AfterDelivery(UIEventDelivery delivery) {
            if (delivery.handledEvent is UIEPointTo) {
                _hasTooltipSourceWithConstructor = false;
                RefreshTooltip();
            }
            if (delivery.handledEvent is UIEMouseDown md && md.IsRight) {
                _isRightClicked = true;
                RefreshTooltip();
            } else if (delivery.handledEvent is UIEMouseUp mu && mu.IsRight) {
                _isRightClicked = false;
                RefreshTooltip();
            }
            
            return delivery;
        }

        void RefreshTooltip() {
            if (TooltipSource != null && TooltipText != null) {
                Tooltip.ShowTooltip(TooltipText);
            } else {
                Tooltip.HideTooltip();
                _tooltipSource = null;
            }
        }
    }
}
