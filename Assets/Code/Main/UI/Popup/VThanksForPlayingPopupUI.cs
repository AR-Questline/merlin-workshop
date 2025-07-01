using Awaken.TG.Assets;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.TitleScreen;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.UI.Popup {
    [UsesPrefab("Story/" + nameof(VThanksForPlayingPopupUI))]
    public class VThanksForPlayingPopupUI : VMediumPopupUI<Story>, IVStoryPanel {
        [SerializeField] VGenericPromptUI closeButton;
        [SerializeField] EventReference closeSound;
        
        Prompts _prompts;
        Prompt _closePrompt;
        
        protected override void OnInitialize() { }

        protected override void OnFullyInitialized() {
            _prompts = Target.AddElement(new Prompts(null));
            _closePrompt = _prompts.BindPrompt(Prompt.Tap(KeyBindings.UI.Generic.Cancel, LocTerms.Close.Translate(), () => Target.FinishStory()).AddAudio(), Target, closeButton);
            _closePrompt.AddAudio(new PromptAudio {
                TapSound = closeSound
            });

            World.SpawnView<VTitleScreenMusic>(Target);
        }

        public void ClearText() { }
        public override void SetArt(SpriteReference art) { }
        public override void SetTitle(string title) { }
        public override void OfferChoice(ChoiceConfig choiceConfig) { }
    }
}