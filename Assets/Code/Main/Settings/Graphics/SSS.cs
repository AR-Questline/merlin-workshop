using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Debugging.Cheats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Graphics {
    public partial class SSS : Setting, IGraphicSetting {
        const string SssEnableId = "SSS_Enable";
        const string SssQualityId = "SSS_Quality";

        // === Options

        readonly DependentOption _mainOption;
        readonly ToggleOption _enableToggle;
        readonly EnumArrowsOption _quality;
        
        public sealed override string SettingName => LocTerms.SettingsSSS.Translate();
        public override bool IsVisible => !PlatformUtils.IsConsole || CheatController.CheatsEnabled();

        static readonly ToggleOption[] OptionsArray = {
            new($"{SssQualityId}_low", Preset.Low.DisplayName, false, false),
            new($"{SssQualityId}_high", Preset.High.DisplayName, true, false),
        };

        readonly Dictionary<Preset, ToggleOption> _presetsMapping = new() {
            {Preset.Low, OptionsArray[0]},
            {Preset.Medium, OptionsArray[0]},
            {Preset.High, OptionsArray[1]},
            {Preset.Ultra, OptionsArray[1]},
        };

        public override IEnumerable<PrefOption> Options => _mainOption.Yield();

        public bool Enabled => _enableToggle.Enabled;
        public int Quality => _quality.OptionInt;

        // === Initialization
        public SSS() {
            _enableToggle = new ToggleOption(SssEnableId, SettingName, true, false);
            _enableToggle.AddTooltip(static () => LocTerms.SettingsTooltipSSS.Translate());

            _quality = new EnumArrowsOption(SssQualityId, LocTerms.SettingsSSSQuality.Translate(), null, false, OptionsArray);
            _mainOption = new DependentOption(_enableToggle, _quality);
        }

        // === Logic
        public void SetValueForPreset(Preset preset) {
            if (PlatformUtils.IsConsole) {
                _enableToggle.Enabled = false;
            } else {
                _enableToggle.Enabled = preset != Preset.Low;
                _quality.Option = _presetsMapping[preset];
            }
        }

        public IEnumerable<Preset> MatchingPresets {
            get {
                if (PlatformUtils.IsConsole) {
                    return Preset.AllPredefined;
                }
                
                if (!_enableToggle.Enabled) {
                    return Preset.Low.Yield();
                }
                return _presetsMapping
                    .Where(kvp => kvp.Value == _quality.Option)
                    .Select(static kvp => kvp.Key);
            }
        }
    }
}
