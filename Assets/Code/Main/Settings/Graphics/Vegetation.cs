using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Debugging.Cheats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Graphics {
    public partial class Vegetation : Setting, IGraphicSetting {
        const string PrefId = "Vegetation";

        // === Options
        EnumArrowsOption Option { get; }
        
        public sealed override string SettingName => LocTerms.SettingsVegetation.Translate();
        public override bool IsVisible => !PlatformUtils.IsXboxScarlettS  || CheatController.CheatsEnabled();

        static readonly ToggleOption Low = new($"{PrefId}_low", Preset.Low.DisplayName, false, false);
        static readonly ToggleOption Medium = new($"{PrefId}_med", Preset.Medium.DisplayName, false, false);
        static readonly ToggleOption High = new($"{PrefId}_high", Preset.High.DisplayName, false, false);
        static readonly ToggleOption Ultra = new($"{PrefId}_ultra", Preset.Ultra.DisplayName, true, false);
        
        readonly Dictionary<Preset, ToggleOption> _presetsMapping = new() {
            {Preset.Low, Low},
            {Preset.Medium, Medium},
            {Preset.High, High},
            {Preset.Ultra, Ultra},
        };

        public override IEnumerable<PrefOption> Options => Option.Yield();

        public int QualityIndex => Option.OptionInt;
        
        public IEnumerable<Preset> MatchingPresets {
            get {
                if (PlatformUtils.IsConsole) {
                    return Preset.AllPredefined;
                }
                return _presetsMapping
                    .Where(kvp => kvp.Value == Option.Option)
                    .Select(kvp => kvp.Key);
            }
        }

        // === Initialization
        public Vegetation() {
            Option = new EnumArrowsOption(PrefId, SettingName, null, false, Low, Medium, High, Ultra);
        }

        // === Logic
        public void SetValueForPreset(Preset preset) {
            if (PlatformUtils.IsConsole) {
                Option.Option = _presetsMapping[Preset.Medium];
            } else {
                Option.Option = _presetsMapping[preset];
            }
        }
    }
}