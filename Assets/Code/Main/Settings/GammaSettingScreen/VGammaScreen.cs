using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Settings.GammaSettingScreen {
    [UsesPrefab("Settings/GammaScreen/" + nameof(VGammaScreen))]
    public class VGammaScreen : View<GammaScreen>, IAutoFocusBase {
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] Transform sliderParent;
        [SerializeField] VGenericPromptUI confirmPrompt;
        [SerializeField] TextMeshProUGUI message;

        public Transform SliderParent => sliderParent;
        public override Transform DetermineHost() => World.Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            message.text = LocTerms.SettingsGammaMessage.Translate();
            SetupPrompt();
            FadeInOut(1);
        }

        void SetupPrompt() {
            var prompts = Target.AddElement(new Prompts(null));
            var confirm = Prompt.Tap(KeyBindings.UI.Generic.Confirm, LocTerms.Confirm.Translate(), () => FadeInOut(0).OnComplete(Target.Discard)).AddAudio();
            prompts.BindPrompt(confirm, Target, confirmPrompt);
        }
        
        Tweener FadeInOut(float target) {
            return canvasGroup.DOCanvasFade(target, UITweens.FadeDuration);
        }
    }
}