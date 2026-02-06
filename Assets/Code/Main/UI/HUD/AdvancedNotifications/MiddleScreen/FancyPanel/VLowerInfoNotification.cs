using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Animations;
using DG.Tweening;
using UnityEngine;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.FancyPanel {
    [UsesPrefab("HUD/AdvancedNotifications/" + nameof(VLowerInfoNotification))]
    public class VLowerInfoNotification : VLowerFancyPanelNotification {
        [SerializeField] CanvasGroup contentGroup;
        [SerializeField] float fadeDuration = 0.15f;
        [SerializeField] float displayDuration = 2f;
        
        Sequence _animationSequence;

        protected override void OnInitialize() {
            base.OnInitialize();
            PlayAnimation();
        }
        
        public void ExtendAnimation() {
            _animationSequence?.Kill();
            CreateAnimation();
        }
        
        void PlayAnimation() {
            _animationSequence?.Kill();
            contentGroup.alpha = 0;
            CreateAnimation();
        }
        
        void CreateAnimation() {
            _animationSequence = DOTween.Sequence()
                .Append(contentGroup.DOFade(1, fadeDuration))
                .AppendInterval(displayDuration)
                .Append(contentGroup.DOFade(0, fadeDuration))
                .OnComplete(DiscardNotification);
        }

        protected override IBackgroundTask OnDiscard() {
            contentGroup.alpha = 0;
            UITweens.DiscardSequence(ref _animationSequence);
            return base.OnDiscard();
        }
    }
}