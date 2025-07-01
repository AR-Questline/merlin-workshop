using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.UI.Popup.PopupContents;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.UI.Popup {
    public partial class PopupUI : Model, IUIStateSource {
        public override Domain DefaultDomain => Domain.Globals;
        public sealed override bool IsNotSaved => true;

        IVPopupUI View => MainView as IVPopupUI;
        UIState IUIStateSource.UIState => UIState.ModalState(HUDState.MiddlePanelShown | HUDState.QuestTrackerHidden).WithShortcutLayer();
        
        // === Creator

        public static PopupUI SpawnSimplePopup(Type viewType, string text, Prompt leftButtonPrompt, Prompt rightButtonPrompt, string title, DynamicContent dynamicContent = null) {
            PopupUI popup = AddSimplePopupUIToWorld(viewType);
            SetupSimplePopupUI(popup, text, leftButtonPrompt, rightButtonPrompt, title, dynamicContent);
            return popup;
        }

        public static PopupUI SpawnSimplePopup(Type viewType, string text, Action acceptCallback = null, string title = "", DynamicContent dynamicContent = null) {
            PopupUI popup = AddSimplePopupUIToWorld(viewType);
            
            var leftButtonPrompt = PopupUI.AcceptTapPrompt(() => {
                acceptCallback?.Invoke();
                popup.Discard();
            });
            var rightButtonPrompt = PopupUI.CancelTapPrompt(() => popup.Discard());
            
            SetupSimplePopupUI(popup, text, leftButtonPrompt, rightButtonPrompt, title, dynamicContent);
            return popup;
        }

        public static PopupUI SpawnSimplePopup3Choices(Type viewType, string text, Prompt leftButtonPrompt, Prompt middleButtonPrompt, Prompt rightButtonPrompt, string title, DynamicContent dynamicContent = null) {
            leftButtonPrompt.AddAudio(new PromptAudio { TapSound = CommonReferences.Get.AudioConfig.ButtonApplySound });
            PopupUI popup = World.Add(new PopupUI());
            World.SpawnView(popup, viewType, true);
            popup.ShowText(TextConfig.WithText(text));
            popup.SpawnContent(dynamicContent);
            popup.SetTitle(title);
            popup.OfferChoice(ChoiceConfig.WithPrompt(leftButtonPrompt));
            popup.OfferChoice(ChoiceConfig.WithPrompt(middleButtonPrompt));
            popup.OfferChoice(ChoiceConfig.WithPrompt(rightButtonPrompt));
            return popup;
        }
        
        public static PopupUI SpawnNoChoicePopup(Type viewType, string text, string title = "", Action callback = null) {
            PopupUI popup = CreateNoChoicePopup(viewType, title, text);
            popup.OfferChoice(ChoiceConfig.WithPrompt(PopupUI.AcceptTapPrompt(() => {
                callback?.Invoke();
                popup.Discard();
            })));
            return popup;
        }

        public static PopupUI SpawnNoChoicePopup(Type viewType, string title, string text, Prompt prompt) {
            var popup = CreateNoChoicePopup(viewType, title, text);
            popup.OfferChoice(ChoiceConfig.WithPrompt(prompt));
            return popup;
        }

        public static PopupUI SpawnNonInteractablePopup(Type viewType, string title, string text) {
            var popup = World.Add(new PopupUI());
            var view = World.SpawnView(popup, viewType, true);
            if (view is VMediumPopupUI vMediumPopup) {
                vMediumPopup.titleText.text = title;
            }
            popup.ShowText(TextConfig.WithText(text));
            popup.SetTitle(title);
            popup.ToggleBg(false);
            return popup;
        }

        static PopupUI CreateNoChoicePopup(Type viewType, string title, string text) {
            var popup = World.Add(new PopupUI());
            World.SpawnView(popup, viewType, true);
            popup.ShowText(TextConfig.WithText(text));
            popup.SetTitle(title);
            return popup;
        }
        
        static PopupUI AddSimplePopupUIToWorld(Type viewType) {
            PopupUI popup = World.Add(new PopupUI());
            World.SpawnView(popup, viewType, true);
            return popup;
        }

        static void SetupSimplePopupUI(PopupUI popup, string text, Prompt leftButtonPrompt, Prompt rightButtonPrompt, string title, DynamicContent dynamicContent = null) {
            popup.ShowText(TextConfig.WithText(text));
            popup.SpawnContent(dynamicContent);
            popup.SetTitle(title);
            leftButtonPrompt.AddAudio(new PromptAudio { TapSound = CommonReferences.Get.AudioConfig.ButtonApplySound });
            popup.OfferChoice(ChoiceConfig.WithPrompt(leftButtonPrompt));
            popup.OfferChoice(ChoiceConfig.WithPrompt(rightButtonPrompt));
        }

        // === Constructor

        PopupUI() {}

        protected override void OnInitialize() {
            
        }

        public void SetTitle(string title) => View.SetTitle(title);
        public void Clear() => View.Clear();
        public void SetArt(SpriteReference art) => View.SetArt(art);
        public void ShowText(TextConfig textConfig) => View.ShowText(textConfig);
        public void OfferChoice(ChoiceConfig choiceConfig) => View.OfferChoice(choiceConfig);
        
        public void ToggleBg(bool enabled) => View.ToggleBg(enabled);
        [UnityEngine.Scripting.Preserve] public void TogglePrompts(bool enabled) => View.TogglePrompts(enabled);


        public void SpawnContent(DynamicContent dynamicContent) => View.SpawnContent(dynamicContent);
        
        public static Prompt ConfirmTapPrompt(Action callback, bool visible = true, bool active = true, bool hold = false) =>
            CreatePrompt(KeyBindings.UI.Generic.Confirm, LocTerms.Confirm.Translate(), callback, Prompt.Position.First, visible, active, hold);

        public static Prompt AcceptTapPrompt(Action callback, bool visible = true, bool active = true, bool hold = false) =>
            CreatePrompt(KeyBindings.UI.Generic.Accept, LocTerms.Accept.Translate(), callback, Prompt.Position.First, visible, active, hold);
        
        public static Prompt CancelTapPrompt(Action callback, bool visible = true, bool active = true, bool hold = false) =>
            CreatePrompt(KeyBindings.UI.Generic.Cancel, LocTerms.Cancel.Translate(), callback, Prompt.Position.Last, visible, active, hold);
        
        public static Prompt BackTapPrompt(Action callback, bool visible = true, bool active = true, bool hold = false) => 
            CreatePrompt(KeyBindings.UI.Generic.Cancel, LocTerms.UIGenericBack.Translate(), callback, Prompt.Position.Last, visible, active, hold);
        
        public static Prompt ExitTapPrompt(Action callback, bool visible = true, bool active = true, bool hold = false) =>
            CreatePrompt(KeyBindings.UI.Generic.Exit, LocTerms.Exit.Translate(), callback, Prompt.Position.Last, visible, active, hold);

        static Prompt CreatePrompt(KeyBindings keyBinding, string promptName, Action callback, Prompt.Position position, bool visible, bool active, bool hold) {
            Prompt prompt = hold
                ? Prompt.Hold(keyBinding, promptName, callback, position).AddAudio()
                : Prompt.Tap(keyBinding, promptName, callback, position).AddAudio();
            
            prompt.SetupState(visible, active);
            return prompt;
        }
    }
}