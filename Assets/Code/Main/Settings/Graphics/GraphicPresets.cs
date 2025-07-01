using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Debugging.Cheats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Enums;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Settings.Graphics {
    public partial class GraphicPresets : Setting {
        const string PrefId = "GraphicsPreset";
        const string DefaultGraphicsPresetFromBenchmarks = "DefaultGraphicsPreset";
        
        public sealed override string SettingName => LocTerms.SettingsGraphicPreset.Translate();
        public override bool IsVisible => !(PlatformUtils.IsConsole || PlatformUtils.IsSteamDeck) || CheatController.CheatsEnabled();

        // === Fields & Properties
        public EnumArrowsOption EnumOption { get; }
        
        List<ToggleOption> _toggleOptions = new();
        List<IGraphicSetting> _graphicSettingsBuffer = new(32);
        Dictionary<ToggleOption, Preset> _presetByOption = new();
        
        public override IEnumerable<PrefOption> Options => EnumOption.Yield();

        public Preset ActivePreset => _presetByOption[EnumOption.Option];
        ToggleOption OptionFor(Preset preset) => _presetByOption.First(kvp => kvp.Value == preset).Key;
        
        // === Static Helper
        public static async UniTask<Preset> GetDefaultGraphicSetting() {
            int preset;
#if UNITY_GAMECORE || UNITY_PS5
            preset = 2;
#else
            int availablePresets = Preset.AllPredefined.Length;
            if (PlatformUtils.IsSteamDeck) {
                preset = 0;
            } else if (PrefMemory.HasKey(DefaultGraphicsPresetFromBenchmarks)) {
                preset = PrefMemory.GetInt(DefaultGraphicsPresetFromBenchmarks);
            } else {
                preset = 0;
                // HardwareScore hardwareScore = await HardwareInfo.GetHardwareScore();
                // preset = (int)(hardwareScore.LowestScore * (availablePresets - 1));
                // PrefMemory.Set(DefaultGraphicsPresetFromBenchmarks, preset, false);
            }
#endif

            // It returns null if score is above 100, but that's ok, that null is converted to Ultra later on (in constructor)
            return RichEnum.AllValuesOfType<Preset>().FirstOrDefault(p => p.QualityIndex == preset);
        }

        // === Constructor
        public GraphicPresets(Preset defaultPreset = null) {
            ToggleOption defaultOption = null;

            if (PlatformUtils.IsXbox) {
                defaultOption = PrepareXboxOptions();
            } else if (PlatformUtils.IsPS5) {
                defaultOption = PreparePSOptions();
            } else if (PlatformUtils.IsSteamDeck) {
                defaultOption = PrepareSteamDeckOptions();
            } else {
                defaultPreset ??= Preset.Ultra;

                foreach (var preset in RichEnum.AllValuesOfType<Preset>()) {
                    ToggleOption option = new($"{PrefId}_{preset.EnumName}", preset.DisplayName, preset == defaultPreset, false);
                    _toggleOptions.Add(option);
                    _presetByOption.Add(option, preset);
                    if (preset == defaultPreset) {
                        defaultOption = option;
                    }
                }
            }

            EnumOption = new EnumArrowsOption(PrefId, SettingName, defaultOption, false, _toggleOptions.ToArray());
            EnumOption.SetForbiddenOptions(OptionFor(Preset.Custom));
            EnumOption.onChangeByPlayer += _ => ApplyPresetToChildren();
        }

        ToggleOption PreparePSOptions() {
            _toggleOptions.Add(new ToggleOption($"{PrefId}_quality", LocTerms.SettingsPresetQuality.Translate(), true, false));
            _presetByOption.Add(_toggleOptions[0], Preset.Ultra);

            AddCustomPreset();
            return _toggleOptions[0];
        }

        ToggleOption PrepareXboxOptions() {
            if (PlatformUtils.IsXboxScarlettX) {
                _toggleOptions.Add(new ToggleOption($"{PrefId}_quality", LocTerms.SettingsPresetQuality.Translate(), true, false));
                _presetByOption.Add(_toggleOptions[0], Preset.Ultra);
            } else {
                _toggleOptions.Add(new ToggleOption($"{PrefId}_performance", LocTerms.SettingsPresetPerformance.Translate(), true, false));
                _presetByOption.Add(_toggleOptions[0], Preset.Low);
            }

            AddCustomPreset();
            return _toggleOptions[0];
        }

        ToggleOption PrepareSteamDeckOptions() {
            _toggleOptions.Add(new ToggleOption($"{PrefId}_performance", LocTerms.SettingsPresetPerformance.Translate(), true, false));
            _presetByOption.Add(_toggleOptions[0], Preset.Low);

            AddCustomPreset();
            return _toggleOptions[0];
        }

        void AddCustomPreset() {
            _toggleOptions.Add(new ToggleOption($"{PrefId}_{Preset.Custom.EnumName}", Preset.Custom.DisplayName, false, false));
            _presetByOption.Add(_toggleOptions[^1], Preset.Custom);
        }

        // === Public
        public void ApplyInitialPreset() {
            if (ActivePreset == Preset.Custom) {
                return;
            }
            
            foreach (var setting in World.All<IGraphicSetting>()) {
                setting.SetValueForPreset(ActivePreset);
                setting.InitialApply();
            }
        }

        public override void RestoreDefault() {
            base.RestoreDefault();
            ApplyPresetToChildren();
        }

        public void RefreshActivePreset() {
            var mapping = new MultiMap<Preset, IGraphicSetting>();
            World.All<IGraphicSetting>().FillList(_graphicSettingsBuffer);
            foreach (var setting in _graphicSettingsBuffer) {
                foreach (var matchingPreset in setting.MatchingPresets) {
                    mapping.Add(matchingPreset, setting);
                }
            }

            Preset allInOnePreset = mapping.FirstOrDefault(kvp => _graphicSettingsBuffer.All(s => kvp.Value.Contains(s))).Key;
            if (allInOnePreset != null) {
                EnumOption.Option = OptionFor(allInOnePreset);
            } else {
                EnumOption.Option = OptionFor(Preset.Custom);
            }
            _graphicSettingsBuffer.Clear();
        }

        void ApplyPresetToChildren() {
            foreach (var setting in World.All<IGraphicSetting>()) {
                setting.RestoreDefault();
                setting.SetValueForPreset(ActivePreset);
            }
        }
    }
}