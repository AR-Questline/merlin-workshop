using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Choices.ChoicePreviews {
    [UsesPrefab("Story/VChoicePreview")]
    public class VChoicePreview : View<ChoicePreview> {
        public const float PreviewFadeDuration = 0.3f;

        [SerializeField] TextMeshProUGUI setNameText;
        [SerializeField] CanvasGroup canvasGroup;

        public override Transform DetermineHost() => Target.ParentModel.Story.StatsPreviewGroup();

        Tween _fadeTween;

        protected override void OnInitialize() {
            canvasGroup.alpha = 0;
            setNameText.SetText(Target.choicePreviewTitle);
        }

        public void OnHoverEnter() {
            _fadeTween.Kill();
            _fadeTween = canvasGroup.DOFade(1f, PreviewFadeDuration);
            foreach (var preview in Target.Elements<BonusPreview>()) {
                preview.View<VBonusPreview>().FadeIn();
            }
        }

        public void OnHoverExit() {
            _fadeTween.Kill(); 
            _fadeTween = canvasGroup.DOFade(0f, PreviewFadeDuration);
            foreach (var preview in Target.Elements<BonusPreview>()) {
                preview.View<VBonusPreview>().FadeOut();
            }
        }
    }
}