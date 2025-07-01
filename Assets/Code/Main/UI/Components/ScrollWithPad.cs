using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Components {
    [RequireComponent(typeof(ScrollRect))]
    public class ScrollWithPad : ViewComponent<IModel> {
        const float Speed = 1000;

        ScrollRect _scrollRect;

        protected override void OnAttach() {
            _scrollRect = GetComponent<ScrollRect>();
        }

        void Update() {
            if (!RewiredHelper.IsGamepad) return;

            float offsetH = 0;//RewiredHelper.Player.GetAxis(KeyBindings.UI.Generic.ScrollHorizontal);
            float offsetV = 0;//RewiredHelper.Player.GetAxis(KeyBindings.UI.Generic.ScrollVertical);

            var contentRect = _scrollRect.content.rect;
            var viewportRect = _scrollRect.viewport.rect;
            
            if (_scrollRect.horizontal && offsetH != 0 && contentRect.width > viewportRect.width) {
                var normalizedOffset = offsetH * Speed * Time.unscaledDeltaTime / contentRect.width;
                var normalizedPosition = _scrollRect.horizontalNormalizedPosition + normalizedOffset;
                _scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(normalizedPosition);
            }
            
            if (_scrollRect.vertical && offsetV != 0 && contentRect.height > viewportRect.height) {
                var normalizedOffset = offsetV * Speed * Time.unscaledDeltaTime / contentRect.height;
                var normalizedPosition = _scrollRect.verticalNormalizedPosition + normalizedOffset;
                _scrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalizedPosition);
            }

            if (offsetH != 0 || offsetV != 0) {
                InterruptAutoScroll();
            }
        }

        void InterruptAutoScroll() {
            GetComponent<SelectionAutoScroll>()?.Interrupt();
        }
    }
}