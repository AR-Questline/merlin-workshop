using Awaken.TG.Assets;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.UI.Popup {
    [UsesPrefab("Story/" + nameof(VFullScreenPopupUI))]
    public class VFullScreenPopupUI : VMediumPopupUI {
        [SerializeField] VGenericPromptUI closeButton;
        [SerializeField] EventReference closeSound;
        
        Prompts _prompts;
        Prompt _closePrompt;

        protected override void OnInitialize() { }

        protected override void OnFullyInitialized() {
            _prompts = Target.AddElement(new Prompts(null));
            _closePrompt = _prompts.BindPrompt(Prompt.Tap(KeyBindings.UI.Generic.Cancel, LocTerms.Close.Translate(), OnClose).AddAudio(), Target, closeButton);
            _closePrompt.AddAudio(new PromptAudio {
                TapSound = closeSound
            });
        }

        void OnClose() {
            Target.Discard();
        }

        public override void SetArt(SpriteReference art) { }
        public override void SetTitle(string title) { }
        public override void OfferChoice(ChoiceConfig choiceConfig) { }
    }
}