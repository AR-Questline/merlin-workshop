using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Debugging.Cheats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Graphics {
    public partial class VfxQuality : Setting, IGraphicsIndexedSetting {
        const string PrefId = "VfxQuality";
        
        // === Options
        public EnumArrowsOption Option { get; }

        public sealed override string SettingName => LocTerms.SettingsVfxQuality.Translate();
        public override bool IsVisible => !PlatformUtils.IsConsole || CheatController.CheatsEnabled();

        static readonly ToggleOption[] OptionsArray = {
            new($"{PrefId}_low", Preset.Low.DisplayName, false, false),
            new($"{PrefId}_medium", Preset.Medium.DisplayName, false, false),
            new($"{PrefId}_high", Preset.High.DisplayName, true, false),
        };

        readonly Dictionary<Preset, ToggleOption> _presetsMapping = new() {
            {Preset.Low, OptionsArray[0]},
            {Preset.Medium, OptionsArray[1]},
            {Preset.High, OptionsArray[2]},
            {Preset.Ultra, OptionsArray[2]},
        };

        public int ActiveIndex => Option.OptionInt;
        public override IEnumerable<PrefOption> Options => Option.Yield();

        public IEnumerable<Preset> MatchingPresets => _presetsMapping
            .Where(kvp => kvp.Value == Option.Option)
            .Select(kvp => kvp.Key);
        
        // === Initialization
        public VfxQuality() {
            Option = new EnumArrowsOption(PrefId, SettingName, null, false, OptionsArray);
        }

        // === Logic
        public void SetValueForPreset(Preset preset) {
            Option.Option = _presetsMapping[preset];
        }
    }
}
