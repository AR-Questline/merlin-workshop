using System;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using UnityEngine;

namespace Awaken.TG.Main.UI.Helpers {
    /// <summary>
    /// ViewComponent allows hovering objects without using default View-Hovering system.
    /// </summary>
    public class VCManualHover : ViewComponent, IUIAware {

        // === Properties & Fields

        public bool IsHovered { get; private set; }

        Action<bool> _onHoverChange;
        int _lastHoveredFrame;

        // === Initialization

        public void AssignCallback(Action<bool> onHoverChange) {
            _onHoverChange += onHoverChange;
        }

        // === Event handling

        public UIResult Handle(UIEvent evt) {
            if (evt is UIEPointTo) {
                _lastHoveredFrame = Time.frameCount;
                SetHoverState(true);
                return UIResult.Prevent;
            }

            return UIResult.Ignore;
        }

        // === Unity Update

        void Update() {
            if (_lastHoveredFrame < Time.frameCount -1) {
                SetHoverState(false);
            }
        }

        // === Helper

        void SetHoverState(bool enabled) {
            if (IsHovered != enabled) {
                IsHovered = enabled;
                _onHoverChange?.Invoke(enabled);
            }
        }
    }
}