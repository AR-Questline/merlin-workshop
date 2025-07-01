using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Settings.Options.Views {
    [UsesPrefab("Settings/VButtonOption")]
    public class VButtonOption : VFocusableSetting<ButtonOption> {
        [SerializeField] TextMeshProUGUI displayName;
        
        Prompt _click;

        public override void Setup(PrefOption option) {
            base.Setup(option);
            displayName.text = Option.DisplayName;
        }

        protected override void SpawnPrompts() {
            _click = Target.Prompts.AddPrompt(
                Prompt.Tap(KeyBindings.UI.Items.SelectItem, LocTerms.SettingsButtonAction.Translate(), Option.OnClick
                    ), Target);
        }
        
        protected override void RemovePrompts() {
            Target.Prompts.RemovePrompt(ref _click);
        }

        protected override void Cleanup() {}

        protected override void Refresh() {}
    }
}