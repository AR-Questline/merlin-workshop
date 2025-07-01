using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Debugging.Cheats;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Settings.Graphics {
    /// <summary>
    /// Currently not used, but it's there in case we start using APV
    /// </summary>
    public partial class GeneralGraphicsWithAPV : Setting, IGraphicsIndexedSetting {
        const string PrefId = "GeneralGraphics";
        
        // === Options
        EnumArrowsOption Option { get; }
        bool _previousApvState;

        public override bool IsVisible => !PlatformUtils.IsConsole || CheatController.CheatsEnabled();
        public sealed override string SettingName => LocTerms.SettingsGeneral.Translate();
        public override bool RequiresRestart => Hero.Current != null && !CheatController.CheatsEnabled();

        [UnityEngine.Scripting.Preserve]
        public static IReadOnlyCollection<ToggleOption> AllOptionsStatic => s_optionsArray;

        static ToggleOption[] s_optionsArray = {
            new($"{PrefId}_low", Preset.Low.DisplayName, false, false),
            new($"{PrefId}_medium", Preset.Medium.DisplayName, false, false),
            new($"{PrefId}_high", Preset.High.DisplayName, true, false)
        };

        Dictionary<Preset, ToggleOption> _presetsMapping = new() {
            {Preset.Low, s_optionsArray[0]},
            {Preset.Medium, s_optionsArray[1]},
            {Preset.High, s_optionsArray[2]},
            {Preset.Ultra, s_optionsArray[2]},
        };

        [UnityEngine.Scripting.Preserve]
        public ToggleOption Active => s_optionsArray.ElementAtOrDefault(Option.OptionInt) ?? s_optionsArray[2];
        public int ActiveIndex => Option.OptionInt;

        public override IEnumerable<PrefOption> Options => Option.Yield();

        public IEnumerable<Preset> MatchingPresets => _presetsMapping
            .Where(kvp => kvp.Value == Option.Option)
            .Select(kvp => kvp.Key);
        
        // === Initialization
        public GeneralGraphicsWithAPV() {
            Option = new EnumArrowsOption(PrefId, SettingName, s_optionsArray[2], false, s_optionsArray);
        }

        // === Logic
        public void SetValueForPreset(Preset preset) {
            Option.Option = _presetsMapping[preset];
        }

        protected override void OnApply() {
            if (RequiresRestart) {
                return;
            }

            SetInternal();
        }

        public override void PerformOnSceneChange() {
            SetInternal();
        }

        // === Callbacks
        void SetInternal() {
            RefreshQuality(_previousApvState);
            this.Trigger(Events.SettingRefresh, this);
        }

        // This should be called from SceneService
        public void RefreshQualityRuntime(bool apvState) {
            _previousApvState = apvState;
            RefreshQuality(apvState);
        }
        
        public static void RefreshQuality(bool apvState) {
            //Quality levels count is doubled, because each level has its duplicate witch apv option enabled.
            //For example quality level array can look like this 0:Low, 1:High, 2:LowAPV, 3:HighAPV
            int qualitiesCount = QualitySettings.names.Length / 2;
            int qualityLevel = QualitySettings.GetQualityLevel();
            if ((apvState && qualityLevel >= qualitiesCount) || (!apvState && qualityLevel < qualitiesCount)) {
                return;
            }

            QualitySettings.SetQualityLevel(apvState switch {
                true => qualityLevel + qualitiesCount,
                false => qualityLevel - qualitiesCount
            });
            World.Only<TextureQuality>().OnQualitySettingsChanged();
        }
    }
}