using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Debugging.Cheats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Controllers;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Graphics {
    public partial class GeneralGraphics : Setting, IGraphicsIndexedSetting {
        const string PrefId = "GeneralGraphics";
        
        // === Options
        protected EnumArrowsOption Option { get; }

        public override bool IsVisible => !(PlatformUtils.IsConsole || PlatformUtils.IsSteamDeck) || CheatController.CheatsEnabled();
        public sealed override string SettingName => LocTerms.SettingsGeneral.Translate();

        static readonly ToggleOption[] OptionsArray = {
            new($"{PrefId}_low", Preset.Low.DisplayName, false, false),
            new($"{PrefId}_medium", Preset.Medium.DisplayName, false, false),
            new($"{PrefId}_high", Preset.High.DisplayName, true, false)
        };

        readonly Dictionary<Preset, ToggleOption> _presetsMapping = new() {
            { Preset.Low, OptionsArray[0] },
            { Preset.Medium, OptionsArray[1] },
            { Preset.High, OptionsArray[2] },
            { Preset.Ultra, OptionsArray[2] },
        };

        public int ActiveIndex => Option.OptionInt;
        public override IEnumerable<PrefOption> Options => Option.Yield();

        public IEnumerable<Preset> MatchingPresets => _presetsMapping
            .Where(kvp => kvp.Value == Option.Option)
            .Select(kvp => kvp.Key);
        
        // === Initialization
        public GeneralGraphics() {
            Option = new EnumArrowsOption(PrefId, SettingName, null, false, OptionsArray);
            Option.AddTooltip(static () => LocTerms.SettingsTooltipGeneral.Translate());
        }

        // === Logic
        public void SetValueForPreset(Preset preset) {
            Option.Option = _presetsMapping[preset];
        }

        protected override void OnApply() {
            base.OnApply();
            foreach (var view in Views.ToArray()) {
                if (view is IGeneralGraphicsSceneView dependentView) {
                    dependentView.SettingsRefreshed(this);
                }
            }
        }
    }
}