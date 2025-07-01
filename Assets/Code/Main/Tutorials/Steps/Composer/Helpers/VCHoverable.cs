using System;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using UnityEngine;

namespace Awaken.TG.Main.Tutorials.Steps.Composer.Helpers {
    public class VCHoverable : ViewComponent, IUIAware {
        public Action<bool> onHover;

        bool _hovered;
        int _lastHoverFrame;
        
        public UIResult Handle(UIEvent evt) {
            if (evt is UIEPointTo) {
                _lastHoverFrame = Time.frameCount;
                if (!_hovered) {
                    onHover?.Invoke(true);
                    _hovered = true;
                }
            }
            return UIResult.Ignore;
        }

        void Update() {
            if (_hovered && Time.frameCount - _lastHoverFrame > 2) {
                onHover?.Invoke(false);
                _hovered = false;
            }
        }
    }
}