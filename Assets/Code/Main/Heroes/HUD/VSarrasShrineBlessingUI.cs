using Awaken.TG.Main.Localization;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using Awaken.Utility.Animations;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.HUD {
    [UsesPrefab("HUD/" + nameof(VSarrasShrineBlessingUI))]
    public class VSarrasShrineBlessingUI : View<SarrasShrineBlessingUI> {
        [SerializeField] TextMeshProUGUI blessingText;
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] float startDelay = 5f;
        [SerializeField] float fadeDuration = 2f;
        [SerializeField] float stayDuration = 2f;

        Sequence _sequence;
        
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnHUD();

        protected override void OnInitialize() {
            blessingText.SetText(LocTerms.UIBlessingOfSarras.Translate());
            Animate();
        }

        void Animate() {
            canvasGroup.alpha = 0;
            _sequence = DOTween.Sequence().SetUpdate(true).SetDelay(startDelay)
                .Append(canvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.OutCirc))
                .AppendInterval(stayDuration)
                .Append(canvasGroup.DOFade(0f, fadeDuration).SetEase(Ease.InCirc))
                .OnComplete(Target.Discard)
                .OnKill(Target.Discard);
        }

        protected override IBackgroundTask OnDiscard() {
            _sequence.Kill();
            _sequence = null;
            
            return base.OnDiscard();
        }
    }
}