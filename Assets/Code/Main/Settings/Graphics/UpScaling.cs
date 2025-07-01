using System;
using System.Collections.Generic;
using Awaken.TG.Debugging.Cheats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;
using QFSW.QC;
using Unity.Mathematics;
using UnityEngine;
#if !UNITY_GAMECORE && !UNITY_PS5
using UnityEngine.NVIDIA;
#endif
using SystemInfo = UnityEngine.Device.SystemInfo;

namespace Awaken.TG.Main.Settings.Graphics {
    public partial class UpScaling : Setting, IGraphicsIndexedSetting {
        // === Options
        const int STPOptionIndex = 1;
        const int DLSSOptionIndex = 2;
        const string UpscalingTypePrefId = "UpScalingType";
        const string UpScalingQualityPrefId = "UpScalingQuality";

        static readonly ToggleOption[] UpScalingTypesOptions = {
            new($"{UpscalingTypePrefId}_None", LocTerms.None.Translate(), false, false),
            new($"{UpscalingTypePrefId}_STP", LocTerms.SettingsSTP.Translate(), false, false),
            new($"{UpscalingTypePrefId}_DLSS", LocTerms.SettingsDLSS.Translate(), false, false),
        };

        // { None, Low, Medium, High, Ultra, None }
        static readonly float[] UpScalingQualityDivisors = { 2.99f, 2f, 1.7f, 1.5f, 1.2f, 1f }; // Higher number means lower quality

        static readonly float[] QualityFactors = { 0.9f, 0.75f, 0.65f, 0.5f };

        // === Fields
        readonly DependentOption _mainOption;
        readonly EnumArrowsOption _upscaleType;
        readonly SliderOption _qualitySlider;

        // === Properties
        public static bool IsDLSSAvailable => !PlatformUtils.IsConsole && UnityEngine.Rendering.HighDefinition.HDDynamicResolutionPlatformCapabilities.DLSSDetected;
        public static bool IsSTPAvailable => SystemInfo.graphicsShaderLevel >= 50;

        public static bool IsAnyUpScalingAvailable => IsDLSSAvailable || IsSTPAvailable;
        public sealed override string SettingName => LocTerms.SettingsUpScaling.Translate();
        public override bool IsVisible => !PlatformUtils.IsConsole || CheatController.CheatsEnabled();

        public override IEnumerable<PrefOption> Options => _mainOption.Yield();
        public EnumArrowsOption UpscalingTypeOption => _upscaleType;

        public bool IsDLSSEnabled => ActiveUpScalingType == UpScalingType.DLSS;
        public bool IsSTPEnabled => ActiveUpScalingType == UpScalingType.STP;
        
        public int ActiveIndex => _upscaleType.OptionInt;
        public bool IsUpScalingEnabled => ActiveIndex > 0;
        public UpScalingType ActiveUpScalingType => (UpScalingType)_upscaleType.OptionInt;

        public float SliderValue => _qualitySlider.Value;

        /// <summary>
        /// Lower number means better quality (1f = 100% quality, 3f = 33% quality) 
        /// </summary>
        float QualityDivisor => math.remap(0f, 1f, UpScalingQualityDivisors[0], UpScalingQualityDivisors[^1], SliderValue);
        public float QualityScaling => 100f / QualityDivisor;
        
#if !UNITY_GAMECORE && !UNITY_PS5
        public uint DLSSQuality => 0;
        // {
        //     get {
        //         if (QualityDivisor >= UpScalingQualityDivisors[0]) {
        //             return DLSSQuality.UltraPerformance;
        //         } else if (QualityDivisor >= UpScalingQualityDivisors[1]) {
        //             return DLSSQuality.MaximumPerformance;
        //         } else if (QualityDivisor >= UpScalingQualityDivisors[2]) {
        //             return DLSSQuality.Balanced;
        //         } else {
        //             return DLSSQuality.MaximumQuality;
        //         }
        //     }
        // }
#endif

        public IEnumerable<Preset> MatchingPresets {
            get {
                if (PlatformUtils.IsConsole || PlatformUtils.IsSteamDeck) {
                    foreach (var preset in Preset.AllPredefined) {
                        yield return preset;
                    }
                } else if (IsUpScalingEnabled) {
                    if (SliderValue >= ModifiedQualityFactor(1)) {
                        yield return Preset.Ultra;
                    }
                    if (ModifiedQualityFactor(0) >= SliderValue && SliderValue >= ModifiedQualityFactor(2)) {
                        yield return Preset.High;
                    }
                    if (ModifiedQualityFactor(1) >= SliderValue && SliderValue >= ModifiedQualityFactor(3)) {
                        yield return Preset.Medium;
                    }
                    if (ModifiedQualityFactor(2) >= SliderValue) {
                        yield return Preset.Low;
                    }
                } else {
                    foreach (var preset in Preset.AllPredefined) {
                        yield return preset;
                    }
                }
            }
        }

        // === Initialization
        public UpScaling() {
            var defaultOption = UpScalingTypesOptions[GetDefaultOptionIndex()];
            _upscaleType = new EnumArrowsOption(UpscalingTypePrefId, SettingName, defaultOption, false, UpScalingTypesOptions);
            _upscaleType.SetForbiddenOptions(GetNotAvailableOptions());
            _upscaleType.EnsureCurrentOptionNotForbidden();

            _upscaleType.SetInteractabilityFunction(() => !PlatformUtils.IsSteamDeck);
            
            _qualitySlider = new SliderOption(UpScalingQualityPrefId, LocTerms.SettingsUpScalingQuality.Translate(), 0f, 1f, false, NumberWithPercentFormat, 0f, false);
            _mainOption = new DependentOption(_upscaleType, () => IsUpScalingEnabled, _qualitySlider);
        }

        protected override void OnInitialize() {
            ModelUtils.DoForFirstModelOfType<ScreenResolution>(ScreenResolutionAvailable, this);
        }

        void ScreenResolutionAvailable(ScreenResolution screenResolution) {
            screenResolution.ListenTo(Setting.Events.SettingChanged, ResolutionChanged, this);
            ResolutionChanged(screenResolution);
        }

        void ResolutionChanged(Setting setting) {
            var screenResolution = (ScreenResolution)setting;
            _qualitySlider.Value = GetForcedSliderValue(screenResolution.SelectedResolution);
            Apply(out var _);
        }

        // === Logic
        public void SetValueForPreset(Preset preset) {
            if (preset.QualityIndex < 0 || preset.QualityIndex >= QualityFactors.Length) {
                return;
            }

            if (PlatformUtils.IsConsole) {
                // Just for API wholeness, should be called, ever
                _qualitySlider.Value = GetForcedSliderValue(new ScreenResolution.ARResolution(Screen.currentResolution));
            } else if (PlatformUtils.IsSteamDeck) {
                _qualitySlider.Value = 0.5f;
            } else {
                var reversedIndex = QualityFactors.Length - 1 - preset.QualityIndex;
                float sliderValue = ModifiedQualityFactor(reversedIndex);
                _qualitySlider.Value = sliderValue;
            }
        }

        ToggleOption[] GetNotAvailableOptions() {
            var notAvailableOptions = new List<ToggleOption>();
            if (!IsDLSSAvailable) {
                notAvailableOptions.Add(UpScalingTypesOptions[DLSSOptionIndex]);
            }
            if (!IsSTPAvailable) {
                notAvailableOptions.Add(UpScalingTypesOptions[STPOptionIndex]);
            }
            return notAvailableOptions.ToArray();
        }

        int GetDefaultOptionIndex() {
            if (IsDLSSAvailable) {
                return DLSSOptionIndex;
            }
            if (IsSTPAvailable) {
                return STPOptionIndex;
            }
            return 0;
        }

        float GetForcedSliderValue(ScreenResolution.ARResolution resolution) {
            const uint PixelsCount4K = 3840 * 2160;
            const uint PixelsCount2K = 2560 * 1440;
            const uint PixelsCount1K = 1920 * 1080;

#if UNITY_PS5
            var pixelsCount = resolution.width * resolution.height;

            if (PlatformUtils.IsPS5Pro) {
                if (pixelsCount >= PixelsCount4K) {
                    return 0.7f;
                } else if (pixelsCount > PixelsCount2K) {
                    return 0.8f;
                } else if (pixelsCount == PixelsCount2K) {
                    return 0.9f;
                } else if (pixelsCount > PixelsCount1K) {
                    return 0.95f; // Fix distortion
                } else {
                    return 0.9f;
                }
            } else {
                if (pixelsCount >= PixelsCount4K) {
                    return 0.6f;
                } else if (pixelsCount >= PixelsCount2K) {
                    return 0.85f;
                } else if (pixelsCount > PixelsCount1K) {
                    return 0.95f; // Fix distortion
                } else {
                    return 0.9f;
                }
            }
#elif UNITY_GAMECORE
            if (PlatformUtils.IsXboxScarlettS) {
                return 0.5f;
            } else {
                var pixelsCount = resolution.width * resolution.height;
                if (pixelsCount > PixelsCount4K) {
                    return 0.1f;
                } else if (pixelsCount > PixelsCount2K) {
                    return 0.4f;
                } else {
                    return 0.9f;
                }
            }
#else
            return _qualitySlider.Value;
#endif
        }

        // === DPI scaling
        // 24"   1K -> 92 -> No additional factor
        // 47"   4k -> 94 -> No additional factor
        // 27"   2K -> 109 -> 0.1f
        // 15.6" 1K -> 188 -> 0.2f
        // 15.6" 2K -> 188 -> 0.3f
        // 15.6" 4K -> 282 -> 0.4f
        // 17.3" 2K -> 170 -> 0.2f
        // 17.3" 4K -> 255 -> 0.4f
        // 13.3" 1K -> 166 -> 0.2f
        // 13.3" 2K -> 221 -> 0.4f
        // Fore more ask AI:
        // Prepare table for screens 13.3, 15.6, 17.3, 24, 27, 32, 37 and 47, for 1K, 2K and 4K where for each configuration you calculate dpi

        static readonly DpiScalingData[] DpiScalingConfig = {
            new() { dpiThreshold = 96f, optionValueModifier = 0.1f },
            new() { dpiThreshold = 120f, optionValueModifier = 0.2f },
            new() { dpiThreshold = 180f, optionValueModifier = 0.3f },
            new() { dpiThreshold = 220f, optionValueModifier = 0.4f },
        };

        float ModifiedQualityFactor(int index) {
            var dpi = Configuration.GetFloat("upscaling.dpi.modifier", Screen.dpi);
            if (dpi <= 0f) {
                return QualityFactors[index];
            }

            for (int i = DpiScalingConfig.Length - 1; i >= 0; i--) {
                if (dpi >= DpiScalingConfig[i].dpiThreshold) {
                    return QualityFactors[index] - DpiScalingConfig[i].optionValueModifier;
                }
            }
            return QualityFactors[index];
        }
        
        [Command("settings.upscaling-quality", "")][UnityEngine.Scripting.Preserve]
        public static void PrintUpscalingQuality() {
            float quality = World.Only<UpScaling>()._qualitySlider.Value;
            QuantumConsole.Instance.LogToConsoleAsync($"Upscaling quality: {quality}");
        }
        
        [Command("settings.upscaling-quality", "")][UnityEngine.Scripting.Preserve]
        public static void SetUpscalingQuality(float quality) {
            var upscaling = World.Only<UpScaling>();
            upscaling._qualitySlider.Value = quality;
            upscaling.Apply(out var _);
            PrintUpscalingQuality();
        }

        [Serializable]
        struct DpiScalingData {
            public float dpiThreshold;
            public float optionValueModifier;
        }
    }
}