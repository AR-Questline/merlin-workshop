using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Utility.UI {
    public static class UITweens {
        public const float ColorChangeDuration = 0.2f;
        public const float FadeDuration = 0.2f;
        
        [UnityEngine.Scripting.Preserve]
        public static Tweener DOUIPop(this Component uiElement, Vector2 offset, float duration) {
            Vector3 basePos = uiElement.transform.localPosition;
            Vector3 offset3d = (Vector3) offset;
            float t = 1f;
            var tween = DOTween.To(() => t, (v) => {
                t = v;
                uiElement.transform.localPosition = basePos + offset3d * t;
            }, 0f, duration);
            return tween;
        }

        [UnityEngine.Scripting.Preserve]
        public static Tween DoBump(this Transform transform) =>
            DoBump(transform, Vector3.one * 0.5f, Vector3.one * 1.6f, 0.65f);

        public static Tween DoBump(this Transform transform, Vector3 startScale, Vector3 maxScale, float duration) {
            transform.localScale = startScale;

            Tween scaleUp = DOTween
                .To(() => transform.localScale, v => transform.localScale = v, maxScale, duration * 0.7f)
                .SetEase(Ease.OutCubic);
            Tween scaleDown = DOTween
                .To(() => transform.localScale, v => transform.localScale = v, Vector3.one, duration * 0.3f)
                .SetEase(Ease.InCubic);

            Tween sequence = DOTween.Sequence()
                .Append(scaleUp)
                .Append(scaleDown);

            return sequence;
        }

        [UnityEngine.Scripting.Preserve]
        public static Tweener DOUIShake(this Component uiElement, float force, float rotation, float duration) {
            Vector3 basePos = uiElement.transform.localPosition;
            float t = 1f;
            var tween = DOTween.To(() => t, (v) => {
                t = v;
                float displacement = force * t;
                float angle = rotation * t;
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * displacement;
                uiElement.transform.localPosition = basePos + offset;
            }, 0f, duration);
            return tween;
        }

        public static Tweener DOUIFill(this Image image, float endValue, float duration) {
            return DOTween.To(() => image.fillAmount, v => image.fillAmount = v, endValue, duration);
        }

        [UnityEngine.Scripting.Preserve]
        static void DisableLayoutFor(Tweener tween, Component uiElement) {
            LayoutElement le = uiElement.GetComponent<LayoutElement>();
            if (le != null && !le.ignoreLayout) {
                le.ignoreLayout = true;
                tween.OnComplete(() => le.ignoreLayout = false);
            }
        }

        public static Tween SetInstant(this Tween tween, bool instant) {
            if (instant) {
                tween.Complete();
            }
            return tween;
        }
        
        public static Tweener DOCanvasFade(this CanvasGroup target, float alpha, float duration, bool isIndependentUpdate = true) {
            return target.DOFade(alpha, duration).SetUpdate(isIndependentUpdate);
        }

        [UnityEngine.Scripting.Preserve]
        public static Tweener DOLabelColor(this ButtonConfig target, Color color, float duration, bool isIndependentUpdate = true) {
            return target.Label.DOColor(color, duration).SetUpdate(isIndependentUpdate);
        }
        
        public static Tweener DOGraphicColor(this Graphic target, Color color, float duration, bool isIndependentUpdate = true) {
            return target.DOColor(color, duration).SetUpdate(isIndependentUpdate);
        }
        
        /// <summary>
        /// Kills tween and sets to null.
        /// </summary>
        /// <param name="tween"></param>
        /// <param name="completeTween"></param>
        public static void DiscardTween(ref Tween tween, bool completeTween = false) {
            if (tween == null) return;
            
            tween.Kill(completeTween);
            tween = null;
        }
        
        /// <summary>
        /// Kills sequence and sets to null.
        /// </summary>
        /// <param name="sequence"></param>
        /// <param name="completeSequence"></param>
        public static void DiscardSequence(ref Sequence sequence, bool completeSequence = false) {
            if (sequence == null) return;
            
            sequence.Kill(completeSequence);
            sequence = null;
        }
    }
}