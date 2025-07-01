using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Debugging.Cheats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors.SceneInitialization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Cysharp.Threading.Tasks;
using QFSW.QC;
using Unity.Mathematics;
using UnityEngine;
using Log = Awaken.Utility.Debugging.Log;

namespace Awaken.TG.Main.Settings.Graphics {
    public partial class ScreenResolution : Setting {
        const string PrefIdResolution = "Resolution";
        const string PrefIdRefreshRate = "RefreshRate";
        const string PrefIdVSync = "VSync";
        
        const string MaxResolutionHeightConfigKey = "max-resolution-height";

        // it is a getter to ensure proper resolution when changing displaying device
        static ARResolution s_nativeResolution = PlatformUtils.IsConsole
            ? new() { width = Display.main.systemWidth, height = Display.main.systemHeight }
            // We trust that we do it before any resolution change so it would be native at the point of calling/caching
            : new() { width = Screen.currentResolution.width, height = Screen.currentResolution.height };

        // === Options
        public EnumArrowsOption FullscreenOption { get; private set; }
        Dictionary<ToggleOption, FullScreenMode> _fullScreenModes = new();
        
        public EnumArrowsOption ResolutionOption { get; private set; }
        Dictionary<ToggleOption, ARResolution> _resolutions = new();
        
        public EnumArrowsOption RefreshRateOption { get; private set; }
        Dictionary<ToggleOption, ARRefreshRate> _refreshRates = new();
        ARRefreshRate _refreshRateUnlimited;
        
        public ToggleOption VSyncOption { get; private set; }
        
        public sealed override string SettingName => LocTerms.SettingsResolution.Translate();

        public override IEnumerable<PrefOption> Options {
            get {
                if (!PlatformUtils.IsConsole) {
                    yield return FullscreenOption;
                }
                if (!PlatformUtils.IsSteamDeck) {
                    yield return ResolutionOption;
                }
                yield return RefreshRateOption;
                yield return VSyncOption;
            }
        }

        public ARResolution SelectedResolution => _resolutions[ResolutionOption.Option];
        
        // === Initialization
        public ScreenResolution() {
            Init();
        }

        void Init() {
            // First collect all available resolutions
            CollectResolutions(out var availableResolutions, out var availableRefreshRates);

            { // Resolutions
                ToggleOption defaultOption = null;
                var nativeRes = GetNativeRes();
                
                foreach (var res in availableResolutions) {
                    var isNative = res.Equals(nativeRes);
                    var option = new ToggleOption($"{PrefIdResolution}_{res.width}x{res.height}", $"{res.width}x{res.height}", isNative, false);
                    _resolutions.Add(option, res);
                    if (isNative) {
                        defaultOption = option;
                    }
                }
                
                var options = _resolutions.Keys.ToArray();
                
                if (defaultOption == null) {
                    Log.Important?.Error("No default resolution found!");
                    defaultOption = options[0];
                }
                
                ResolutionOption = new EnumArrowsOption(PrefIdResolution, SettingName, defaultOption, false, options);
                
                if (options.All(option => option.Enabled == false)) {
                    ResolutionOption.Option = defaultOption;
                }

                ARResolution GetNativeRes() {
                    foreach (var resolution in availableResolutions) {
                        if (resolution.Equals(s_nativeResolution)) {
                            return resolution;
                        }
                    }
                    return availableResolutions[^1];
                }
            }
            
            { // Refresh Rates
                var maxAllowedRefreshRate = GetMaxRefreshRate();
                ToggleOption defaultOption = null;

                ARRefreshRate? rate30 = null;
                ARRefreshRate? rate50 = null;
                ARRefreshRate? rate60 = null;
                ARRefreshRate? rate100 = null;
                ARRefreshRate? rate120 = null;
                ARRefreshRate? rate144 = null;
                
                foreach (var rate in availableRefreshRates) {
                    TryAdd(rate, 1);
                    TryAdd(rate, 2);
                    TryAdd(rate, 3);
                    TryAdd(rate, 4);
                }

                if (rate30 == null) {
                    TryAdd(new RefreshRate {
                        numerator = 30,
                        denominator = 1,
                    }, 1);
                }

                bool default30 = false;
                bool default50 = false;
                bool default60 = false;

                if (PlatformUtils.IsSteamDeck) {
                    default30 = true;
                } else {
                    if (rate60 != null) {
                        default60 = true;
                    } else if (rate50 != null) {
                        default50 = true;
                    } else {
                        default30 = true;
                    }
                }

                if (rate30 != null) {
                    AddOption(rate30.Value, "30", "30", default30);
                }
                if (rate50 != null) {
                    AddOption(rate50.Value, "50", "50", default50);
                }
                if (rate60 != null) {
                    AddOption(rate60.Value, "60", "60", default60);
                }
                if (rate100 != null) {
                    AddOption(rate100.Value, "100", "100", false);
                }
                if (rate120 != null) {
                    AddOption(rate120.Value, "120", "120", false);
                }
                if (rate144 != null) {
                    AddOption(rate144.Value, "144", "144", false);
                }
                
                _refreshRateUnlimited = new ARRefreshRate {
                    refreshRate = new RefreshRate {
                        numerator = 0,
                        denominator = 1,
                    },
                    vSyncCount = 1,
                };
                AddOption(_refreshRateUnlimited, "Unlimited", LocTerms.SettingsUnlimitedFps.Translate(), false);

                var options = _refreshRates.Keys.ToArray();

                RefreshRateOption = new EnumArrowsOption(PrefIdRefreshRate, LocTerms.SettingsTargetFps.Translate(), defaultOption, false, options);

                if (options.All(option => option.Enabled == false)) {
                    RefreshRateOption.Option = defaultOption;
                }

                double GetMaxRefreshRate() {
                    if (CheatController.CheatsEnabled()) {
                        return 1000;
                    }
                    if (PlatformUtils.IsConsole) {
                        return 60;
                    }
                    if (PlatformUtils.IsSteamDeck) {
                        return 30;
                    }
                    return 1000;
                }
                
                void TryAdd(RefreshRate refreshRate, int vSyncCount) {
                    var rate = refreshRate.value / vSyncCount;
                    if (rate > maxAllowedRefreshRate) {
                        return;
                    }
                    if (rate <= 30) {
                        TryReplace(30, rate, ref rate30, refreshRate, vSyncCount);
                    } else if (rate <= 50) {
                        TryReplace(50, rate, ref rate50, refreshRate, vSyncCount);
                    } else if (rate <= 60) {
                        TryReplace(60, rate, ref rate60, refreshRate, vSyncCount);
                    } else if (rate <= 100) {
                        TryReplace(100, rate, ref rate100, refreshRate, vSyncCount);
                    } else if (rate <= 120) {
                        TryReplace(120, rate, ref rate120, refreshRate, vSyncCount);
                    } else if (rate <= 144) {
                        TryReplace(144, rate, ref rate144, refreshRate, vSyncCount);
                    }

                    static void TryReplace(double target, double newRate, ref ARRefreshRate? arRate, RefreshRate newRefreshRate, int newVSyncCount) {
                        if (arRate is null) {
                            arRate = new ARRefreshRate {
                                refreshRate = newRefreshRate,
                                vSyncCount = newVSyncCount,
                            };
                            return;
                        }

                        var currentRefreshRate = arRate.Value.refreshRate;
                        var currentVSync = arRate.Value.vSyncCount;
                        var currentRate = currentRefreshRate.value / currentVSync;

                        var currentScore = (currentRate - target) + VSyncScore(currentVSync);
                        var newScore = (newRate - target) + VSyncScore(newVSyncCount);
                        
                        if (newScore > currentScore) {
                            arRate = new ARRefreshRate {
                                refreshRate = newRefreshRate,
                                vSyncCount = newVSyncCount,
                            };
                        }

                        static double VSyncScore(int vSyncCount) {
                            return 4 - vSyncCount;
                        }
                    }
                }
                
                void AddOption(ARRefreshRate rate, string name, string displayName, bool isDefault) {
                    var option = new ToggleOption($"{PrefIdRefreshRate}_{name}", displayName, isDefault, false);
                    _refreshRates.Add(option, rate);
                    if (isDefault) {
                        defaultOption = option;
                    }
                }
            }

            { // Fullscreen Modes
                ToggleOption fullscreenWindowed = new($"{PrefIdResolution}_FullScreenWindowed", LocTerms.SettingsBorderless.Translate(), !PlatformUtils.IsConsole, false);
                ToggleOption windowed = new($"{PrefIdResolution}_Windowed", LocTerms.SettingsWindowed.Translate(), false, false);
                ToggleOption fullScreen = new($"{PrefIdResolution}_FullScreen", LocTerms.SettingsFullscreen.Translate(), PlatformUtils.IsConsole, false);
                _fullScreenModes.Add(fullscreenWindowed, FullScreenMode.FullScreenWindow);
                _fullScreenModes.Add(windowed, FullScreenMode.Windowed);
                _fullScreenModes.Add(fullScreen, FullScreenMode.ExclusiveFullScreen);
                FullscreenOption = new EnumArrowsOption($"{PrefIdResolution}_FullScreenMode", LocTerms.SettingsFullscreen.Translate(), _fullScreenModes.Keys.FirstOrDefault(r => r.DefaultValue), false, _fullScreenModes.Keys.ToArray());
            }

            { // VSync
                bool defaultValue = PlatformUtils.IsPS5;
                VSyncOption = new ToggleOption(PrefIdVSync, LocTerms.SettingsVSync.Translate(), defaultValue, false);
            }
        }

        protected override void OnInitialize() {
            var sceneInitializer = Services.TryGet<SceneInitializer>();
            if (sceneInitializer != null) {
                sceneInitializer.SceneInitializationHandle.OnInitialized += ApplyAfterInit;
            }
        }

        void ApplyAfterInit() {
            Services.Get<SceneInitializer>().SceneInitializationHandle.OnInitialized -= ApplyAfterInit;
            OnApply();
        }

        // === Operations
        protected override void OnApply() {
            Refresh();
        }

        public void Refresh() {
            var mode = PlatformUtils.IsConsole ? FullScreenMode.ExclusiveFullScreen : _fullScreenModes[FullscreenOption.Option];
            var resolution = _resolutions[ResolutionOption.Option];
            var refreshRate = _refreshRates[RefreshRateOption.Option];
            var vSync = VSyncOption.Enabled;

            Application.targetFrameRate = refreshRate.refreshRate.numerator == 0 ? -1 : (int) refreshRate.refreshRate.value;
            Screen.fullScreenMode = mode;
            Screen.SetResolution(resolution.width, resolution.height, mode, refreshRate.refreshRate);
            QualitySettings.vSyncCount = vSync ? refreshRate.vSyncCount : 0;
        }

        public async UniTaskVoid ResetScreenResolution(AsyncOperation screenMoveOp) {
            await screenMoveOp;
            
            if (HasBeenDiscarded) {
                return;
            }
            if (!PlatformUtils.IsConsole) {
                s_nativeResolution = new() { width = Screen.currentResolution.width, height = Screen.currentResolution.height };
            }
            
            _resolutions.Clear();
            _refreshRates.Clear();
            _fullScreenModes.Clear();
            Init();

            ResolutionOption.Option = _resolutions.First(k => k.Value.Equals(s_nativeResolution)).Key;
            InitialApply();
        }

        void CollectResolutions(out ARResolution[] allResolutions, out RefreshRate[] allRefreshRates) {
            var uniqueResolutions = new HashSet<int2>();
            var uniqueRefreshRates = new HashSet<uint2>();
            
            var maxAllowedResolutionHeight = Configuration.GetInt(MaxResolutionHeightConfigKey, GetMaxResolution());
            
            foreach (var resolution in Screen.resolutions) {
                AddResolution(resolution.width, resolution.height);
                AddRefreshRate(resolution.refreshRateRatio);
            }

            var currentResolution = s_nativeResolution;
            AddResolution(currentResolution.width, currentResolution.height);
            AddRefreshRate(Screen.currentResolution.refreshRateRatio);

            allResolutions = ArrayUtils.Select(uniqueResolutions, res => new ARResolution {
                width = res.x,
                height = res.y
            });
            allRefreshRates = ArrayUtils.Select(uniqueRefreshRates, rate => new RefreshRate {
                numerator = rate.x,
                denominator = rate.y
            });
            
            Array.Sort(allResolutions);

            void AddResolution(int width, int height) {
                if (height <= maxAllowedResolutionHeight) {
                    uniqueResolutions.Add(new int2(width, height));
                }
            }
            
            void AddRefreshRate(in RefreshRate rate) {
                if (rate.value > 0) {
                    uniqueRefreshRates.Add(new uint2(rate.numerator, rate.denominator));
                }
            }

            int GetMaxResolution() {
                if (CheatController.CheatsEnabled()) {
                    return 4320;
                }
                if (PlatformUtils.IsPS5) {
                    return PlatformUtils.IsPS5Pro ? 2160 : 1440;
                }
                if (PlatformUtils.IsXbox) {
                    return PlatformUtils.IsXboxScarlettS ? 1080 : 2160;
                }
                return 2160;
            }
        }

        // === Helper Struct
        public struct ARResolution : IEquatable<ARResolution>, IComparable<ARResolution> {
            public int width;
            public int height;

            public ARResolution(Resolution resolution) {
                width = resolution.width;
                height = resolution.height;
            }
            
            public bool Equals(ARResolution other) {
                return width == other.width && height == other.height;
            }

            public override bool Equals(object obj) {
                return obj is ARResolution other && Equals(other);
            }
            
            public override int GetHashCode() {
                unchecked {
                    return (width * 397) ^ height;
                }
            }

            public int CompareTo(ARResolution other) {
                var widthCompare = width.CompareTo(other.width);
                if (widthCompare != 0) {
                    return widthCompare;
                }
                return height.CompareTo(other.height);
            }
        }

        public struct ARRefreshRate {
            public RefreshRate refreshRate;
            public int vSyncCount;
        }
        
        
        [Command("settings.vsync", "")][UnityEngine.Scripting.Preserve]
        public static void SetVSync(int count) {
            QualitySettings.vSyncCount = count;
            PrintVSync();
        }
        
        [Command("settings.vsync", "")][UnityEngine.Scripting.Preserve]
        public static void PrintVSync() {
            QuantumConsole.Instance.LogToConsoleAsync("VSync: " + QualitySettings.vSyncCount);
        }
        
        [Command("settings.target-framerate", "")][UnityEngine.Scripting.Preserve]
        public static void SetTargetFramerate(int framerate) {
            Application.targetFrameRate = framerate;
            PrintTargetFramerate();
        }
        
        [Command("settings.target-framerate", "")][UnityEngine.Scripting.Preserve]
        public static void PrintTargetFramerate() {
            QuantumConsole.Instance.LogToConsoleAsync("Target Framerate: " + Application.targetFrameRate);
        }
        
        [Command("settings.target-framerate", "")][UnityEngine.Scripting.Preserve]
        public static void SetRefreshRate(uint numerator, uint denominator) {
            var resolution = Screen.currentResolution;
            var refreshRate = new RefreshRate {
                numerator = numerator,
                denominator = denominator,
            };
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreenMode, refreshRate);
            PrintRefreshRate();
        }
        
        [Command("settings.target-framerate", "")][UnityEngine.Scripting.Preserve]
        public static void PrintRefreshRate() {
            var ratio = Screen.currentResolution.refreshRateRatio;
            QuantumConsole.Instance.LogToConsoleAsync($"Target Framerate: {ratio.value:0.##} ({ratio.numerator}/{ratio.denominator})");
        }
        
        [Command("settings.max-queued-frames", "")][UnityEngine.Scripting.Preserve]
        public static void SetMaxQueuedFrames(int frames) {
            QualitySettings.maxQueuedFrames = frames;
            PrintMaxQueuedFrames();
        }
        
        [Command("settings.max-queued-frames", "")][UnityEngine.Scripting.Preserve]
        public static void PrintMaxQueuedFrames() {
            QuantumConsole.Instance.LogToConsoleAsync("Max Queued Frames: " + QualitySettings.maxQueuedFrames);
        }
    }
}