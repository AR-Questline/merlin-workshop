using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using Awaken.Utility.Animations;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Tutorials.TutorialPopups {
    [UsesPrefab("UI/Tutorials/" + nameof(VTutorialText))]
    public class VTutorialText : VTutorialText<TutorialText> { }

    public class VTutorialText<T> : View<T> where T : TutorialText {
        const float FadeDuration = 0.1f;
        
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text contentText;
        [SerializeField] VGenericPromptUI closeButton;
        [SerializeField] GameObject content;

        [SerializeField] Color bgColorInInventory;
        [SerializeField] Color bgColorInGameplay;
        
        Sequence _textsSequence;
        
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnTutorials();
        
        protected override void OnInitialize() {
            content.SetActive(false);
            SetupTexts();
            World.EventSystem.ListenTo(EventSelector.AnySource, Focus.Events.ControllerChanged, this, SetupTextsWithFade);
        }

        public void Show(bool state) {
            content.SetActive(state);
            
            if (state) {
                Services.Get<CanvasService>().ShowTutorialCanvasOnly(Target.DisableOtherCanvases);
                ShowContent();
            }
        }

        protected virtual void ShowContent() {
            InitPrompts();
        }

        void SetupTextsWithFade() {
            _textsSequence.Kill();
            
            _textsSequence = DOTween.Sequence().SetUpdate(true)
                .Append(titleText.DOFade(0f, FadeDuration))
                .Join(contentText.DOFade(0f, FadeDuration))
                .AppendCallback(SetupTexts)
                .Append(titleText.DOFade(1f, FadeDuration))
                .Join(contentText.DOFade(1f, FadeDuration));
        }

        void SetupTexts() {
            titleText.SetActiveAndText(!string.IsNullOrEmpty(Target.TitleText), Target.TitleText);
            contentText.SetActiveAndText(!string.IsNullOrEmpty(Target.ContentText), Target.ContentText);
        }

        void InitPrompts() {
            var prompts = Target.AddElement(new Prompts(null));
            prompts.BindPrompt(Prompt.Tap(KeyBindings.UI.Generic.Cancel, LocTerms.Close.Translate(), Target.Close), Target, closeButton);
        }

        protected override IBackgroundTask OnDiscard() {
            _textsSequence.Kill();
            _textsSequence = null;
            return base.OnDiscard();
        }
    }
}