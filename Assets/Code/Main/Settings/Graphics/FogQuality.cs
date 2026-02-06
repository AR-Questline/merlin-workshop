using System.Collections.Generic;
using Awaken.TG.Debugging.Cheats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Graphics {
    public partial class FogQuality : Setting, IGraphicSetting {
        public const string PrefId = "Fog";
    
        // === Options
        EnumArrowsOption Option { get; }

        static readonly ToggleOption[] OptionsArray = {
            new($"{PrefId}_low", Preset.Low.DisplayName, PlatformUtils.IsConsole, false),
            new($"{PrefId}_high", Preset.High.DisplayName, !PlatformUtils.IsConsole, false)
        };
        
        readonly Dictionary<Preset, ToggleOption> _presetsMapping = new() {
            {Preset.Low, OptionsArray[0]},
            {Preset.Medium, OptionsArray[0]},
            {Preset.High, OptionsArray[0]},
            {Preset.Ultra, OptionsArray[1]},
        };
        
        public sealed override string SettingName => LocTerms.SettingsFog.Translate();
        public override bool IsVisible => !PlatformUtils.IsConsole || CheatController.CheatsEnabled();

        public override IEnumerable<PrefOption> Options => Option.Yield();

        public int Quality => Option.OptionInt;
        public int MaxQuality => OptionsArray.Length - 1;
        
        public IEnumerable<Preset> MatchingPresets => Preset.AllPredefined;
        
        // === Initialization
        public FogQuality() {
            Option = new EnumArrowsOption(PrefId, SettingName, null, false, OptionsArray);
        }

        // === Logic
        public void SetValueForPreset(Preset preset) {
            if (PlatformUtils.IsConsole) {
                Option.Option = OptionsArray[0];
            } else {
                Option.Option = _presetsMapping[preset];
            }
        }
    }
}