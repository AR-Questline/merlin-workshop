using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.MVC.UI.Sources;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Animations;
using Unity.Mathematics;
using UnityEngine;
using Keyboard = Awaken.TG.Main.UI.GamepadKeyboard.Keyboard;

namespace Awaken.TG.Main.Heroes.CharacterCreators.Parts {
    public class VCCHeroName : View<CCHeroName>, IUIAware, IVCCFocusablePart, ICCPromptSource {
        [SerializeField] ARInputField inputField;
        [SerializeField] Transform gamepadKeyboardParent;
        [SerializeField] VGenericPromptUI writePromptView;

        bool _isFocusedOnConsole;
        Prompt _writePrompt;

        public ARInputField InputField => inputField;
        
        protected override void OnInitialize() {
            World.Only<GameUI>().AddElement(new AlwaysPresentHandlers(UIContext.Keyboard, inputField, Target));
            inputField.Initialize(LocTerms.CharacterCreationEnterNamePlaceholder.Translate(), Target.SavedValue, ChangeValue);
            inputField.OnSelected += OnSelect;
            inputField.OnHovered += OnHover;

            _writePrompt = Prompt.Tap(KeyBindings.UI.Generic.Accept, string.Empty, GamepadWrite, controllers: ControlSchemeFlag.Gamepad);
            Target.CharacterCreator.Prompts.BindPrompt(_writePrompt, Target, writePromptView);
        }

        void ChangeValue(string value) {
            Target.SavedValue = value;
        }
        
        void OnSelect(bool selected) {
            if (RewiredHelper.IsGamepad == false) {
                Target.ParentModel.ParentModel.TabsController.BlockNavigation = selected;
            }
        }
        
        void OnHover(bool hovered) {
            if (hovered) {
                Target.CharacterCreator.SetPromptInvoker(this);
            }
        }
        
        void GamepadWrite() {
            _writePrompt.SetActive(false);
            inputField.InvokeSelect(true);
            if (PlatformUtils.IsConsole && !_isFocusedOnConsole) {
                inputField.TMPInputField.ActivateInputField();
                _isFocusedOnConsole = true;
            } else if (PlatformUtils.IsSteamDeck && !_isFocusedOnConsole) {
#if !UNITY_GAMECORE && !UNITY_PS5
                // var mode = Steamworks.EFloatingGamepadTextInputMode.k_EFloatingGamepadTextInputModeModeSingleLine;
                // HeathenEngineering.SteamworksIntegration.API.Utilities.Client.ShowVirtualKeyboard(mode, float2.zero, float2.zero);
                inputField.TMPInputField.ActivateInputField();
                _isFocusedOnConsole = true;
#endif
            } else if (RewiredHelper.IsGamepad) {
                var keyboard = Target.AddElement(new Keyboard(gamepadKeyboardParent, inputField));
                keyboard.ListenTo(Model.Events.AfterDiscarded, ReceiveFocus, this);
            }
        }

        public void ReceiveFocusFromTop(float horizontalPercent) => ReceiveFocus();
        public void ReceiveFocusFromBottom(float horizontalPercent) => ReceiveFocus();
        void ReceiveFocus() {
            _writePrompt.SetActive(true);
            inputField.InvokeSelect(false);
            World.Only<Focus>().Select(this);
        }

        void Update() {
            if (inputField.TMPInputField.isFocused == _isFocusedOnConsole) {
                return;
            }
            
            _isFocusedOnConsole = inputField.TMPInputField.isFocused;
            if (!_isFocusedOnConsole) {
                ReceiveFocus();
            }
        }
        
        public UIResult Handle(UIEvent evt) {
            if (evt is UINaviAction naviAction) {
                if (naviAction.direction == NaviDirection.Up) {
                    Target.FocusAbove();
                    return UIResult.Accept;
                }

                if (naviAction.direction == NaviDirection.Down) {
                    Target.FocusBelow();
                    return UIResult.Accept;
                }
            }
            
            return UIResult.Ignore;
        }

        protected override IBackgroundTask OnDiscard() {
            inputField.OnHovered -= OnHover;
            return base.OnDiscard();
        }
    }
}