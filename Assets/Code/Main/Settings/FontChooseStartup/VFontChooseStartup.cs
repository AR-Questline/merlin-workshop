using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Settings.FontChooseStartup {
    [UsesPrefab("Settings/FontChooseStartup/" + nameof(VFontChooseStartup))]
    public class VFontChooseStartup : View<FontChooseStartup> {
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] VGenericPromptUI serifPromptUI;
        [SerializeField] VGenericPromptUI sansPromptUI;
        [Space]
        [SerializeField] TMP_Text serifSampleText;
        [SerializeField] TMP_Text sansSampleText;
        [SerializeField] TMP_Text generalInfoText;
        [SerializeField] TMP_Text settingsInfoText;
        
        public override Transform DetermineHost() => World.Services.Get<ViewHosting>().OnMainCanvas();

        protected override void OnInitialize() {
            SetupText();
            SetupPrompt();
        }

        protected override void OnFullyInitialized() {
            FadeInOut(1);
        }

        void SetupPrompt() {
            var prompts = Target.AddElement(new Prompts(null));
            var serifPrompt = Prompt.Tap(KeyBindings.UI.CloudConflict.ChoseLocal, LocTerms.MainSerifFont.Translate(), () => Submit(FontFamily.Serif).Forget());
            var sansPrompt = Prompt.Tap(KeyBindings.UI.CloudConflict.ChoseCloud, LocTerms.MainSansFont.Translate(), () => Submit(FontFamily.Sans).Forget());
            serifPrompt.AddAudio(CommonReferences.Get.AudioConfig.DefaultHoldPromptAudio);
            sansPrompt.AddAudio(CommonReferences.Get.AudioConfig.DefaultHoldPromptAudio);
            prompts.BindPrompt(serifPrompt, Target, serifPromptUI);
            prompts.BindPrompt(sansPrompt, Target, sansPromptUI);
        }

        void SetupText() {
            var sample = LocTerms.FontChooseSample.Translate();
            serifSampleText.text = sample;
            sansSampleText.text = sample;
            generalInfoText.text = LocTerms.FontChooseInfoText.Translate();
            settingsInfoText.text = LocTerms.FontChooseSettingsInfoText.Translate();
        }
        
        async UniTaskVoid Submit(FontFamily font) {
            Target.SubmitFont(font);
            FadeInOut(0);

            if (await AsyncUtil.DelayTime(Target, UITweens.FadeDuration)) {
                Target.Discard();
            }
        }
        
        Tweener FadeInOut(float target) {
            return canvasGroup.DOCanvasFade(target, UITweens.FadeDuration);
        }
    }
}