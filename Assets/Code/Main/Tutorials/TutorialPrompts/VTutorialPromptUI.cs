using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility.Animations;
using Awaken.Utility.Animations;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Tutorials.TutorialPrompts {
    [UsesPrefab("UI/Tutorials/VTutorialPromptUI")]
    public class VTutorialPromptUI : View<TutorialPrompt> {
        const float FadeTime = 0.5f;
        const float PreferredHeight = 30f;
        
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] TextMeshProUGUI description;
        [SerializeField] GameObject secondKeyParent;
        [SerializeField] LayoutElement layoutElement;

        Tween _tween;
        Tween _layoutTween;
        
        protected override void OnInitialize() {
            canvasGroup.alpha = 0;
            _tween = canvasGroup.DOFade(1, FadeTime).SetUpdate(true);
            _layoutTween = DOTween.To(() => layoutElement.minHeight, h => layoutElement.minHeight = h, PreferredHeight, FadeTime);
            secondKeyParent.SetActive(Target.HasSecondKey);
            description.text = Target.Description;
        }

        protected override IBackgroundTask OnDiscard() {
            _tween.Kill();
            _tween = canvasGroup.DOFade(0, FadeTime).SetUpdate(true);

            _layoutTween.Kill();
            _layoutTween = DOTween.To(() => layoutElement.minHeight, h => layoutElement.minHeight = h, 0, FadeTime);
            
            return new TweenTask(_tween);
        }
    }
}