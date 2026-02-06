using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.Utility;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using Keyboard = Awaken.TG.Main.UI.GamepadKeyboard.Keyboard;

namespace Awaken.TG.Main.UI.Popup.PopupContents {
    [UsesPrefab("Story/PopupContents/" + nameof(VEditNameUI))]
    public class VEditNameUI : View<EditNameUI>, IFocusSource {
        Keyboard _keyboard;
        
        [SerializeField] TMP_InputField inputField;
        [SerializeField] TextMeshProUGUI validationText;
        [SerializeField] Transform gamepadKeyboardParent;
        
        public bool ForceFocus => true;
        public Component DefaultFocus => this;

        protected override void OnInitialize() {
            inputField.text = Target.Value;
            inputField.onTouchScreenKeyboardStatusChanged.AddListener(OnTouchScreenKeyboardStatusChanged);
            inputField.onValueChanged.AddListener(OnValueUpdated);
            validationText.SetText(string.Empty);

            if (!PlatformUtils.IsConsole) {
                World.EventSystem.ListenTo(EventSelector.AnySource, Focus.Events.ControllerChanged, Target, SpawnGamepadKeyboard);
                SpawnGamepadKeyboard();
            }

            inputField.ActivateInputField();
        }
        
        void Update() {
            if (inputField.isFocused == Target.IsInputFieldFocused) {
                return;
            }

            Target.IsInputFieldFocused = inputField.isFocused;
            Target.Trigger(EditNameUI.Events.InputFieldFocusChanged, Target.IsInputFieldFocused);
        }

        void OnTouchScreenKeyboardStatusChanged(TouchScreenKeyboard.Status status) {
            switch (status) {
                case TouchScreenKeyboard.Status.Visible:
                    inputField.DeactivateInputField();
                    break;
                case TouchScreenKeyboard.Status.Canceled:
                    inputField.ActivateInputField();
                    break;
            }
        }

        void SpawnGamepadKeyboard() {
            if (PlatformUtils.IsSteamDeck) {
#if !UNITY_GAMECORE && !UNITY_PS5 && !MICROSOFT_GAME_CORE
                // var mode = Steamworks.EFloatingGamepadTextInputMode.k_EFloatingGamepadTextInputModeModeSingleLine;
                // HeathenEngineering.SteamworksIntegration.API.Utilities.Client.ShowVirtualKeyboard(mode, float2.zero, float2.zero);
#endif
            } else if (RewiredHelper.IsGamepad) {
                _keyboard = Target.AddElement(new Keyboard(gamepadKeyboardParent, inputField));
                _keyboard.ListenTo(Keyboard.Events.InputAccepted, Target.OnInputKeyboardAccepted, Target);
                _keyboard.ListenTo(Keyboard.Events.InputCanceled, Target.OnInputKeyboardCanceled, Target);
            }
        }

        void OnValueUpdated(string newName) {
            Target.ChangeValue(newName);
            validationText.SetText(Target.ErrorMsg);
        }
    }
}