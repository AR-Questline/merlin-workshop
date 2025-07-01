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
    [UsesPrefab("Settings/VEnumArrows")]
    public class VEnumArrows : VFocusableSetting<EnumArrowsOption> {
        // === References
        [SerializeField] TextMeshProUGUI settingNameText;
        [SerializeField] TextMeshProUGUI optionNameText;
        [SerializeField] ARButton leftButton;
        [SerializeField] ARButton rightButton;

        Prompt _left;
        Prompt _right;
        
        // === VFocusableSetting
        public override void Setup(PrefOption option) {
            base.Setup(option);

            leftButton.Interactable = Option.Interactable;
            rightButton.Interactable = Option.Interactable;

            settingNameText.text = Option.DisplayName;
            leftButton.OnClick += PreviousOption;
            rightButton.OnClick += NextOption;
            Refresh();

            Option.onChange += OnOptionChange;
            Option.onForbiddenOptionsChange += OnOptionChange;
        }

        protected override void RemovePrompts() {
            Target.Prompts.RemovePrompt(ref _left);
            Target.Prompts.RemovePrompt(ref _right);
        }
        
        protected override void SpawnPrompts() {
            _left = Target.Prompts.AddPrompt(
                Prompt.Tap(KeyBindings.Gamepad.DPad_Left, LocTerms.SettingsPreviousOption.Translate(), PreviousOption,
                    controllers: ControlSchemeFlag.Gamepad), Target);
            _right = Target.Prompts.AddPrompt(
                Prompt.Tap(KeyBindings.Gamepad.DPad_Right, LocTerms.SettingsNextOption.Translate(), NextOption,
                    controllers: ControlSchemeFlag.Gamepad), Target);
        }
        
        protected override void Cleanup() {
            Option.onChange -= OnOptionChange;
            leftButton.OnClick -= PreviousOption;
            rightButton.OnClick -= NextOption;
        }
        
        protected override void Refresh() {
            optionNameText.text = Option.Option.DisplayName;
            leftButton.Interactable = Option.Interactable && Option.CanChoosePrevOption;
            rightButton.Interactable = Option.Interactable && Option.CanChooseNextOption;
        }

        // === Callbacks
        void NextOption() {
            Option.NextOption();
            Refresh();
        }
        
        void PreviousOption() {
            Option.PreviousOption();
            Refresh();
        }

        void OnOptionChange(ToggleOption option) {
            Refresh();
        }
    }
}