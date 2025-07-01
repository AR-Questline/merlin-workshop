using System.Collections.Generic;
using Awaken.CommonInterfaces;
using Awaken.TG.Debugging.Cheats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Unity.Mathematics;

namespace Awaken.TG.Main.Settings.Graphics {
    public partial class MipmapsBiasSetting : Setting, IGraphicSetting {
        const string PrefKey = "MipmapsBiasSetting";
        public const float Remap0 = .6f;
        public const float Remap1 = 0f;

        // === Options
        SliderOption Option { get; }
        float? _debugValue;

        public sealed override string SettingName => LocTerms.SettingsMipmapsBias.Translate();
        public override bool IsVisible => !PlatformUtils.IsConsole || CheatController.CheatsEnabled();

        public override IEnumerable<PrefOption> Options => Option.Yield();

        public float Value => _debugValue ?? Option.Value;
        public float BiasValue => _debugValue ?? Value.Remap(0, 1, Remap0, Remap1);

        public IEnumerable<Preset> MatchingPresets => Preset.AllPredefined;

        // === Initialization
        public MipmapsBiasSetting() {
            Option = new(PrefKey, SettingName, 0f, 1f, false, NumberWithPercentFormat, 1, false);
        }

        // === Logic
        public void SetValueForPreset(Preset preset) {
            if (PlatformUtils.IsXboxScarlettS) {
                Option.Value = 0.5f;
            } else if (PlatformUtils.IsSteamDeck) {
                Option.Value = 0.6f;
            } else {
                Option.Value = 1f;
            }
        }

        protected override void OnApply() {
            Set();
        }

        public void SetBias(float bias) {
            Option.Value = math.unlerp(Remap0, Remap1, bias);
            OnApply();
            this.Trigger(Events.SettingRefresh, this);
        }

        void Set() {
            MipmapsBias.bias = BiasValue;
        }
    }
}
