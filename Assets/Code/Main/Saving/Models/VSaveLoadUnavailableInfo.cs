using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Saving.Models {
    [UsesPrefab("VSaveLoadUnavailableInfo")]
    public class VSaveLoadUnavailableInfo : View<SaveLoadUnavailableInfo> {
        [SerializeField] TextMeshProUGUI reasonText;
        [SerializeField] CanvasGroup canvasGroup;
        
        Tween _contentTween;
        
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();
        
        protected override void OnInitialize() {
            Target.ListenTo(Model.Events.AfterChanged, Show, this);
        } 
        
        public void Show() {
            canvasGroup.alpha = 0;
            reasonText.text = Target.Reason.Translate();
            _contentTween.Kill();
            _contentTween = DOTween.To(() => canvasGroup.alpha, a => canvasGroup.alpha = a, 1, 0.25f)
                .OnComplete(Hide);
        }

        void Hide() {
            _contentTween = DOTween.To(() => canvasGroup.alpha, a => canvasGroup.alpha = a, 0, 0.75f)
                .OnComplete(() => {
                    if (!HasBeenDiscarded && !Target.HasBeenDiscarded) {
                        Target.Discard();
                    }
                });
        }
    }
}
