using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Settings.Options.Views {
    [UsesPrefab("Settings/VToggle")]
    public class VToggle : VFocusableSetting<ToggleOption> {
        [SerializeField] TextMeshProUGUI displayName;
        [SerializeField] TextMeshProUGUI optionNameText;
        [SerializeField] ARButton leftButton;
        [SerializeField] ARButton rightButton;

        string OptionText(bool isEnabled) => isEnabled ? LocTerms.On.Translate() : LocTerms.Off.Translate();
        
        Prompt _left;
        Prompt _right;
        
        public override void Setup(PrefOption option) {
            base.Setup(option);

            Refresh();

            displayName.text = Option.DisplayName;
            optionNameText.text = OptionText(Option.Enabled);
            Option.onChange += OnChange;
            leftButton.OnClick += Toggle;
            rightButton.OnClick += Toggle;
        }
        
        protected override void RemovePrompts() {
            Target.Prompts.RemovePrompt(ref _left);
            Target.Prompts.RemovePrompt(ref _right);
        }
        
        protected override void SpawnPrompts() {
            _left = Target.Prompts.AddPrompt(
                Prompt.Tap(KeyBindings.Gamepad.DPad_Left, LocTerms.SettingsToggle.Translate(), Toggle,
                    controllers: ControlSchemeFlag.Gamepad), Target);
            _right = Target.Prompts.AddPrompt(
                Prompt.Tap(KeyBindings.Gamepad.DPad_Right, LocTerms.SettingsToggle.Translate(), Toggle,
                    controllers: ControlSchemeFlag.Gamepad), Target);
        }
        
        protected override void Cleanup() {
            Option.onChange -= OnChange;
            leftButton.OnClick -= Toggle;
            rightButton.OnClick -= Toggle;
        }

        protected override void Refresh() {
            leftButton.Interactable = Option.Interactable;
            rightButton.Interactable = Option.Interactable;
        }

        void Toggle() {
            Option.Enabled = !Option.Enabled;
        }

        void OnChange(bool enabled) {
            optionNameText.text = OptionText(enabled);
        }
    }
}