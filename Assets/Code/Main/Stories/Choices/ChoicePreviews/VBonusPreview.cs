using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Stories.Choices.ChoicePreviews {
    [UsesPrefab("Story/VBonusPreview")]
    public class VBonusPreview : View<BonusPreview> {
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] TextMeshProUGUI descriptionText;
        [SerializeField] Image statIcon;
        
        Sequence _fadeSequence;

        public override Transform DetermineHost() => Target.ParentModel.View<VChoicePreview>().transform;

        protected override void OnInitialize() {
            canvasGroup.alpha = 0;
            if (Target.bonusInfo.InfoIcon is {IsSet: true}) {
                Target.bonusInfo.InfoIcon.RegisterAndSetup(this, statIcon);
            }
            
            nameText.SetText(Target.bonusInfo.InfoName);
            descriptionText.SetText(Target.bonusInfo.InfoDescription);
        }
        
        public void FadeIn() {
            _fadeSequence.Kill();
            _fadeSequence = DOTween.Sequence()
                .Join(canvasGroup.DOFade(1f, VChoicePreview.PreviewFadeDuration));
        }
        
        public void FadeOut() {
            _fadeSequence.Kill();
            _fadeSequence = DOTween.Sequence()
                .Join(canvasGroup.DOFade(0f, VChoicePreview.PreviewFadeDuration));
        }
    }
}