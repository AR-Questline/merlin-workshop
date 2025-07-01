using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using Awaken.Utility.GameObjects;
using UnityEngine;

namespace Awaken.TG.Main.UI.Popup {
    [UsesPrefab("Story/VReadableObjectPopupUI")]
    class VReadableObjectPopupUI : VReadablePopupUI {
        [SerializeField] GameObject titleUnderline;
        
        protected override void OnInitialize() {
            base.OnInitialize();
            Target.AddElement(new StoryOnTop());
        }

        protected override void OnFullyInitialized() {
            base.OnFullyInitialized();
            titleUnderline.SetActiveOptimized(!string.IsNullOrWhiteSpace(titleText.text));
        }

        protected override void InitPrompts() {
            _prompts = Target.AddElement(new Prompts(null));
            _closePrompt = _prompts.BindPrompt(Prompt.Tap(KeyBindings.UI.Generic.Cancel, LocTerms.Close.Translate(), OnClose).AddAudio(), Target, closeButton);
            _closePrompt.SetVisible(true);
        }
    }
}