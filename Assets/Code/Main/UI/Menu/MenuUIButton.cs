using Awaken.TG.Main.UI.Components;
using DG.Tweening;
using UnityEngine;

namespace Awaken.TG.Main.UI.Menu {
    public class MenuUIButton : MonoBehaviour {
        const float FadeDuration = 0.3f;
        const float NotHoveredAlpha = 0.6f;
        const float HoveredScale = 1.1f;
        
        public ARButton button;
        public CanvasGroup canvasGroup;

        Sequence _fadeSequence;
        
        void Start() {
            button.OnHover += OnHover;
            canvasGroup.alpha = NotHoveredAlpha;
        }

        public void OnHover(bool hover) {
            if (canvasGroup) {
                _fadeSequence.Kill();
                _fadeSequence = DOTween.Sequence().SetUpdate(true)
                    .Append(canvasGroup.DOFade(hover ? 1f : NotHoveredAlpha, FadeDuration))
                    .Join(transform.DOScale(hover ? HoveredScale : 1f, FadeDuration));
            }
        }

        void Reset() {
            button = GetComponent<ARButton>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (!canvasGroup) {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }
}