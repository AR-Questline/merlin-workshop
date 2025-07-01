using Awaken.TG.Main.UIToolkit.Utils;
using Awaken.Utility;
using DG.Tweening;
using EnhydraGames.BetterTextOutline;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.UIToolkit {
    public static class UIToolkitExtensions {
        /// <summary>
        /// Disables/Enables the VisualElement by setting the display style to none or flex.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="active"></param>
        public static void SetActiveOptimized(this VisualElement element, bool active) {
            if (element.style.display == DisplayStyle.None && !active ||
                element.style.display == DisplayStyle.Flex && active) {
                return;
            }

            element.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public static Tweener SetActiveOptimizedWithFullFade(this VisualElement element, bool active, float fadeDuration) {
            Tweener tween = active ? element.DoFade(0.0f, 1.0f, fadeDuration) : element.DoFade(1.0f, 0.0f, fadeDuration);

            if (!active) {
                tween.OnComplete(() => element.SetActiveOptimized(false));
            } else {
                element.SetActiveOptimized(true);
            }

            return tween;
        }

        public static void SetTextColor(this VisualElement element, ARColor color) {
            element.SetTextColor(color.Color);
        }

        public static void SetTextColor(this VisualElement element, Color color) {
            element.style.color = color;
        }

        [UnityEngine.Scripting.Preserve]
        public static void SetBackgroundColor(this VisualElement element, ARColor color) {
            element.SetBackgroundColor(color.Color);
        }

        public static void SetBackgroundColor(this VisualElement element, Color color) {
            element.style.backgroundColor = color;
        }

        [UnityEngine.Scripting.Preserve]
        public static void SetBackgroundTintColor(this VisualElement element, ARColor color) {
            element.SetBackgroundTintColor(color.Color);
        }

        public static void SetBackgroundTintColor(this VisualElement element, Color color) {
            element.style.unityBackgroundImageTintColor = color;
        }

        [UnityEngine.Scripting.Preserve]
        public static void SetBackgroundImage(this VisualElement element, Texture2D texture) {
            element.style.backgroundImage = texture;
        }

        public static void SetBackgroundImage(this VisualElement element, Sprite sprite) {
            element.style.backgroundImage = new StyleBackground(sprite);
        }

        [UnityEngine.Scripting.Preserve]
        public static void ToUpperCase(this Label label) {
            label.text = label.text.ToUpper();
        }
        
        [UnityEngine.Scripting.Preserve]
        public static void ToUpperCase(this BetterOutlinedLabel label) {
            label.text = label.text.ToUpper();
        }

        [UnityEngine.Scripting.Preserve]
        public static void ToUpperCase(this Label label, string text) {
            label.text = text.ToUpper();
        }
        
        public static void ToUpperCase(this BetterOutlinedLabel label, string text) {
            label.text = text.ToUpper();
        }

        [UnityEngine.Scripting.Preserve]
        public static void ToLowerCase(this Label label) {
            label.text = label.text.ToLower();
        }
        
        [UnityEngine.Scripting.Preserve]
        public static void ToLowerCase(this BetterOutlinedLabel label) {
            label.text = label.text.ToLower();
        }

        [UnityEngine.Scripting.Preserve]
        public static void ToLowerCase(this Label label, string text) {
            label.text = text.ToLower();
        }
        
        [UnityEngine.Scripting.Preserve]
        public static void ToLowerCase(this BetterOutlinedLabel label, string text) {
            label.text = text.ToLower();
        }

        public static Tweener DoFade(this VisualElement element, float startValue, float endValue, float duration) {
            return DOTween.To(x => element.style.opacity = x, startValue, endValue, duration);
        }

        public static Tweener DoFade(this VisualElement element, float endValue, float duration) {
            return DOTween.To(() => element.style.opacity.value, x => element.style.opacity = x, endValue, duration);
        }

        /// <summary>
        /// Sets the opacity of the VisualElement to 0
        /// </summary>
        /// <param name="element"></param>
        public static void Hide(this VisualElement element) {
            element.style.opacity = 0f;
        }

        public static void HideAndSetActiveOptimized(this VisualElement element) {
            element.Hide();
            element.SetActiveOptimized(false);
        }

        /// <summary>
        /// Sets the opacity of the VisualElement to 1
        /// </summary>
        /// <param name="element"></param>
        public static void Show(this VisualElement element) {
            element.style.opacity = 1f;
        }

        public static void ShowAndSetActiveOptimized(this VisualElement element) {
            element.Show();
            element.SetActiveOptimized(true);
        }

        public static Tweener DoMove(this VisualElement element, Vector3 endValue, float duration) {
            return DOTween.To(() => element.transform.position, x => element.transform.position = x, endValue, duration);
        }

        public static Tweener DoHeight(this VisualElement element, float endValue, float duration) {
            return DOTween.To(() => element.style.height.value.value, x => element.style.height = x, endValue, duration);
        }

        [UnityEngine.Scripting.Preserve]
        public static Tweener DoWidth(this VisualElement element, float endValue, float duration) {
            return DOTween.To(() => element.style.width.value.value, x => element.style.width = x, endValue, duration);
        }

        public static Tweener DoScale(this VisualElement element, Vector3 endValue, float duration) {
            return DOTween.To(() => element.transform.scale, x => element.transform.scale = x, endValue, duration);
        }

        [UnityEngine.Scripting.Preserve]
        public static Tweener DoFontSize(this Label label, float endValue, float duration) {
            return DOTween.To(() => label.style.fontSize.value.value, x => label.style.fontSize = x, endValue, duration);
        }

        public static void SetAnchoredRect(this VisualElement element, AnchoredRect anchoredRect) {
            element.style.position = Position.Absolute;
            element.style.width = anchoredRect.Width;
            element.style.height = anchoredRect.Height;
            element.SetAnchor(anchoredRect);
        }

        /// <summary>
        /// According to parent element world bound, sets the anchor of the VisualElement.
        /// Parent <see cref="VisualElement.worldBound"/> or <see cref="VisualElement.layout"/> must be calculated within GeometryChangedEvent callback to get correct position, otherwise returns NaN.
        /// </summary>
        public static void SetAnchor(this VisualElement element, AnchoredRect anchoredRect) {
            element.parent.RegisterCallbackOnce<GeometryChangedEvent>(_ => SetAnchorInternal(element, anchoredRect));
        }

        static void SetAnchorInternal(VisualElement element, AnchoredRect anchoredRect) {
            switch (anchoredRect.AnchoredPoint) {
                case AnchoredPoint.TopLeft:
                    element.style.left = anchoredRect.X;
                    element.style.top = anchoredRect.Y;
                    break;
                case AnchoredPoint.TopRight:
                    element.style.right = anchoredRect.X;
                    element.style.top = anchoredRect.Y;
                    break;
                case AnchoredPoint.TopCenter:
                    element.style.left = element.parent.worldBound.width / 2 - anchoredRect.X;
                    element.style.top = anchoredRect.Y;
                    break;
                case AnchoredPoint.BottomLeft:
                    element.style.left = anchoredRect.X;
                    element.style.bottom = anchoredRect.Y;
                    break;
                case AnchoredPoint.BottomRight:
                    element.style.right = anchoredRect.X;
                    element.style.bottom = anchoredRect.Y;
                    break;
                case AnchoredPoint.BottomCenter:
                    element.style.left = element.parent.worldBound.width / 2 - anchoredRect.X;
                    element.style.bottom = anchoredRect.Y;
                    break;
                case AnchoredPoint.CenterLeft:
                    element.style.left = anchoredRect.X;
                    element.style.top = element.parent.worldBound.height / 2 - anchoredRect.Y;
                    break;
                case AnchoredPoint.CenterRight:
                    element.style.right = anchoredRect.X;
                    element.style.top = element.parent.worldBound.height / 2 - anchoredRect.Y;
                    break;
                case AnchoredPoint.Center:
                    element.style.left = element.parent.worldBound.width / 2 - anchoredRect.X;
                    element.style.top = element.parent.worldBound.height / 2 - anchoredRect.Y;
                    break;
            }
        }

        public static void SetPosition(this VisualElement element, Vector2 position) {
            element.transform.position = position;
        }

        public static bool IsActive(this VisualElement element) {
            return element.style.display == DisplayStyle.Flex;
        }
    }
}