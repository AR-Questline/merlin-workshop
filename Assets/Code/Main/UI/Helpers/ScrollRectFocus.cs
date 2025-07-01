using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.Helpers {
    public static class ScrollRectFocus {

        public static void SetFocus(this ScrollRect scroll, RectTransform targetTransform) {
            var view = TransformRect(scroll.viewport);
            var content = TransformRect(scroll.content);
            var target = TransformRect(targetTransform);

            if (scroll.horizontal) {
                FocusAxis(0, view, content, target, scroll);
            }
            if (scroll.vertical) {
                FocusAxis(1, view, content, target, scroll);
            }
        }

        static void FocusAxis(int axis, Rect view, Rect content, Rect target, ScrollRect scroll) {
            var scrollRange = content.size[axis] - view.size[axis]; // how much scroll can move
            if (scrollRange > 0f) {
                var scrollPosition = scroll.normalizedPosition;
                var value = scrollPosition[axis];

                var leftOff = view.min[axis] - target.min[axis];
                if (leftOff > 0f) {
                    value -= leftOff / scrollRange;
                } else {
                    var rightOff = target.max[axis] - view.max[axis];
                    if (rightOff > 0f) {
                        value += rightOff / scrollRange;
                    }
                }
                
                scrollPosition[axis] = Mathf.Clamp01(value);
                scroll.normalizedPosition = scrollPosition;
            }
        }

        /// <summary>
        /// transforms rect from local to world space
        /// </summary>
        static Rect TransformRect(RectTransform rectTransform) {
            var rect = rectTransform.rect;
            var min = rectTransform.TransformPoint(rect.min);
            var max = rectTransform.TransformPoint(rect.max);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
