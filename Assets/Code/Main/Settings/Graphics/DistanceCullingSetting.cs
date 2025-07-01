using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Debugging.Cheats;
using Awaken.TG.Graphics.Culling;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Settings.Graphics {
    public partial class DistanceCullingSetting : Setting, IGraphicSetting {
        const string PrefKey = "DistanceCullingBias";
        const float SeriesSViewDistance = 0.55f;

        // === Options
        SliderOption Option { get; }
        float? _debugValue;
        
        public sealed override string SettingName => LocTerms.SettingsDistanceCulling.Translate();
        public override bool IsVisible => !PlatformUtils.IsConsole || CheatController.CheatsEnabled();

        public override IEnumerable<PrefOption> Options => Option.Yield();
        
        public float Value => _debugValue ?? Option.Value;
        public float BiasValue => _debugValue ?? Value.Remap(0, 1, 0.4f, 1f);

        readonly Dictionary<Preset, FloatRange> _presetsMapping = new() {
            { Preset.Low, new(0, 0.4499f) },
            { Preset.Medium, new(0.45f, 0.799f) },
            { Preset.High, new(0.8f, 0.999f) },
            { Preset.Ultra, new(1, 1) },
        };

        readonly Dictionary<Preset, float> _defaultValueByPreset = new() {
            { Preset.Low, 0.4f },
            { Preset.Medium, 0.6f },
            { Preset.High, 0.85f },
            { Preset.Ultra, 1f },
        };
        
        public IEnumerable<Preset> MatchingPresets {
            get {
                if (PlatformUtils.IsXboxScarlettS) {
                    return Preset.AllPredefined;
                }
                
                return _presetsMapping
                    .Where(kvp => kvp.Value.Contains(Option.Value))
                    .Select(static kvp => kvp.Key);
            }
        }

        // === Initialization
        public DistanceCullingSetting() {
            Option = new(PrefKey, SettingName, 0f, 1f, false, NumberWithPercentFormat, 1, false);
        }

        // === Logic
        public void SetValueForPreset(Preset preset) {
            if (PlatformUtils.IsXboxScarlettS) {
                Option.Value = SeriesSViewDistance;
            } else {
                Option.Value = _defaultValueByPreset[preset];
            }
        }

        protected override void OnApply() {
            Set();
        }

        public void RefreshSetting() {
            Set();
            this.Trigger(Events.SettingRefresh, this);
        }

        public void OnGUI() {
            Option.Value = GUILayout.HorizontalSlider(Option.Value, Option.MinValue, Option.MaxValue, GUILayout.Width(260));
        }
        
        public void SetDebugValue(float value) {
            _debugValue = value;
            Set();
            this.Trigger(Events.SettingRefresh, this);
        }

        void Set() {
            World.Services.TryGet<DistanceCullersService>()?.BiasChanged();
            QualitySettings.lodBias = BiasValue;
        }
    }
}
