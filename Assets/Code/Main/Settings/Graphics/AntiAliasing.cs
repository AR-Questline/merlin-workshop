using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Debugging.Cheats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Main.Settings.Options.Views;
using Awaken.TG.Main.Settings.Windows;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Graphics {
    public partial class AntiAliasing : Setting, IGraphicSetting {
        const string PrefId = "AntiAliasing";
        
        public static bool IsUpScalingWithAAEnabled => World.Only<UpScaling>().IsUpScalingEnabled;
        static bool IsUpScalingWithAAAvailable => UpScaling.IsAnyUpScalingAvailable;

        // === Options
        EnumArrowsOption Option { get; }
        
        public sealed override string SettingName => LocTerms.SettingsAntiAliasing.Translate();
        public override bool IsVisible => !(PlatformUtils.IsConsole || PlatformUtils.IsSteamDeck) || CheatController.CheatsEnabled();

        static readonly ToggleOption[] OptionsArray = {
            new($"{PrefId}_none", LocTerms.SettingsNone.Translate(), false, false),
            new($"{PrefId}_fxaa", LocTerms.SettingsFXAA.Translate(), false, false),
            new($"{PrefId}_smaa", LocTerms.SettingsSMAA.Translate(), false, false),
            new($"{PrefId}_taa", LocTerms.SettingsTAA.Translate(), true, false),
        };
        
        readonly Dictionary<Preset, ToggleOption> _presetsMapping = new() {
            {Preset.Low, OptionsArray[1]},
            {Preset.Medium, OptionsArray[1]},
            {Preset.High, OptionsArray[3]},
            {Preset.Ultra, OptionsArray[3]},
        };
        
        public override IEnumerable<PrefOption> Options => Option.Yield();

        public bool FXAA => Option.Option == OptionsArray[1];
        public bool SMAA => Option.Option == OptionsArray[2];
        public bool TAA => Option.Option == OptionsArray[3];
        
        public IEnumerable<Preset> MatchingPresets => Preset.AllPredefined;

        // === Initialization
        public AntiAliasing() {
            Option = new EnumArrowsOption(PrefId, SettingName, null, false, OptionsArray);
            Option.SetInteractabilityFunction(static () => !IsUpScalingWithAAAvailable || !IsUpScalingWithAAEnabled);
        }

        protected override void OnInitialize() {
            var option = World.Only<UpScaling>().UpscalingTypeOption;
            option.onChange += RefreshInteractability;
        }

        // === Logic
        public void SetValueForPreset(Preset preset) {
            Option.Option = _presetsMapping[preset];
        }
        
        void RefreshInteractability(ToggleOption option) {
            var allSettings = World.Any<AllSettingsUI>();
            var view = allSettings?.Views.OfType<VEnumArrows>().FirstOrDefault(v => v.Option == Option);
            view?.UpdateInteractability();
        }
    }
}