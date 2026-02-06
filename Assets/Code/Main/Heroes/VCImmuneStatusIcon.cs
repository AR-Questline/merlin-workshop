using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes {
    public class VCImmuneStatusIcon : ViewComponent {
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] Image immuneIcon;
        
        Sequence _fadeSequence;
        SpriteReference _immuneIconRef;

        public bool HasSetIcon(SpriteReference spriteReference) {
            return _immuneIconRef.arSpriteReference.Equals(spriteReference.arSpriteReference);
        }
        
        public void TryDisplayImmuneIcon(StatusSourceInfo sourceInfo, float fadeDuration, float visibilityDuration, Action callback = null) {
            Hide();
            _immuneIconRef?.Release();
            if (sourceInfo.Icon is {IsSet: true} shareableSpriteRef) {
                _immuneIconRef = shareableSpriteRef.Get();
                _immuneIconRef.SetSprite(immuneIcon, (_, _) => {
                    Fade(fadeDuration, visibilityDuration, callback);
                });
            }
        }

        public void RefreshFade(float fadeDuration, float visibilityDuration, Action callback = null) {
            Fade(fadeDuration, visibilityDuration, callback);
        }
        
        public void Fade(float fadeDuration, float visibilityDuration, Action onComplete = null) {
            _fadeSequence.Kill();
            _fadeSequence = DOTween.Sequence().SetDelay(0.05f)
                .Append(canvasGroup.DOFade(1f, fadeDuration))
                .AppendInterval(visibilityDuration)
                .AppendCallback(() => onComplete?.Invoke());
        }

        public void Discard() {
            _immuneIconRef?.Release();
            _immuneIconRef = null;
            Hide();
        }

        void Hide() {
            canvasGroup.alpha = 0;
        }
    }
}