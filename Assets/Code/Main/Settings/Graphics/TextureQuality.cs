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
    public partial class TextureQuality : Setting, IGraphicSetting {
        const string PrefId = "TexQuality";

        // === Options
        EnumArrowsOption Option { get; }
        
        public sealed override string SettingName => LocTerms.SettingsTextureQuality.Translate();
        public override bool IsVisible => !PlatformUtils.IsConsole || CheatController.CheatsEnabled();

        static readonly ToggleOption HalfRes = new($"{PrefId}_half", Preset.Low.DisplayName, false, false);
        static readonly ToggleOption FullRes = new($"{PrefId}_full", Preset.High.DisplayName, true, false);
        
        Dictionary<Preset, ToggleOption> _presetsMapping = new() {
            {Preset.Low, HalfRes},
            {Preset.Medium, HalfRes},
            {Preset.High, FullRes},
            {Preset.Ultra, FullRes},
        };

        public override IEnumerable<PrefOption> Options => Option.Yield();
        public override bool RequiresRestart => Hero.Current != null && !CheatController.CheatsEnabled();

        public IEnumerable<Preset> MatchingPresets {
            get {
                if (PlatformUtils.IsSteamDeck) {
                    return Preset.AllPredefined;
                } else {
                    return _presetsMapping
                        .Where(kvp => kvp.Value == Option.Option)
                        .Select(kvp => kvp.Key);
                }
            }
        }

        // === Initialization
        public TextureQuality() {
            Option = new EnumArrowsOption(PrefId, SettingName, null, false, HalfRes, FullRes);
        }

        // === Logic
        public void SetValueForPreset(Preset preset) {
            if (PlatformUtils.IsSteamDeck) {
                Option.Option = FullRes;
            } else {
                Option.Option = _presetsMapping[preset];
            }
        }

        protected override void OnApply() {
            if (!RequiresRestart) {
                Set();
            }
        }

        public override void PerformOnSceneChange() { 
            Set();
        }

        public void OnQualitySettingsChanged() {
            Set();
        }

        void Set() {
            int chosenTextureLimit = 1 - Option.OptionInt;
            if (QualitySettings.globalTextureMipmapLimit != chosenTextureLimit) {
                QualitySettings.globalTextureMipmapLimit = chosenTextureLimit;
                this.Trigger(Events.SettingRefresh, this);
            }
        }
    }
}