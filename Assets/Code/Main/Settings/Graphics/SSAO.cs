using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Debugging.Cheats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Graphics {
    public partial class SSAO : Setting, IGraphicSetting {
        const string PrefId = "SSAO";

        // === Options
        ToggleOption Option { get; }
        
        public sealed override string SettingName => LocTerms.SettingsSSAO.Translate();
        public override bool IsVisible => !PlatformUtils.IsConsole || CheatController.CheatsEnabled();
        public bool Enabled => Option.Enabled;

        public override IEnumerable<PrefOption> Options => Option.Yield();

        readonly Preset[] _allowedForPresets = {Preset.High, Preset.Ultra};

        // === Initialization
        public SSAO() {
            Option = new ToggleOption(PrefId, SettingName, true, false);
        }

        // === Logic
        public void SetValueForPreset(Preset preset) {
            Option.Enabled = _allowedForPresets.Contains(preset);
        }

        public IEnumerable<Preset> MatchingPresets {
            get {
                if (Option.Enabled) {
                    yield return Preset.High;
                    yield return Preset.Ultra;
                } else {
                    yield return Preset.Low;
                    yield return Preset.Medium;
                }
            }
        }
    }
}