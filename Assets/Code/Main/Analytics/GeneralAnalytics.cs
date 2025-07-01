#if !UNITY_GAMECORE && !UNITY_PS5
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.CharacterCreators.Difficulty;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.Main.Settings.Controls;
using Awaken.TG.Main.Settings.FirstTime;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.Main.UI.TitleScreen;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Automation;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Analytics {
    public partial class GeneralAnalytics : Element<GameAnalyticsController> {
        public sealed override bool IsNotSaved => true;
        const string FirstTimeSettingsPrefix = "FirstTime";
        const string DefaultSettingsPrefix = "Settings";

        protected override void OnInitialize() {
            World.EventSystem.LimitedListenTo(EventSelector.AnySource, Hero.Events.HeroLongTeleported, this, 
                _ => SendGamepadEvent().Forget(), 1);
            if (!PrefMemory.GetBool(TitleScreenUtils.FirstTimeSettingPrefKey)) {
                World.EventSystem.LimitedListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<FirstTimeSettings>(), this, 
                    OnFirstTimeSettingsApplied, 1);
            }
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<ChooseDifficulty>(), this,
                OnFirstTimeDifficultySelected);
            World.Only<DifficultySetting>().ListenTo(Setting.Events.SettingChanged, SendDifficultySettingEvent, this);
            World.Only<FOVSetting>().ListenTo(Setting.Events.SettingChanged, SendFOVSettingEvent, this);
            World.Only<ShowUIHUD>().ListenTo(Setting.Events.SettingChanged, SendShowHUDSettingEvent, this);
            World.Only<ScreenShakesProactiveSetting>().ListenTo(Setting.Events.SettingChanged, SendCameraShakesProactiveSettingEvent, this);
            World.Only<ScreenShakesReactiveSetting>().ListenTo(Setting.Events.SettingChanged, SendCameraShakesReactiveSettingEvent, this);
            World.Only<HeadBobbingSetting>().ListenTo(Setting.Events.SettingChanged, SendHeadBobbingSettingEvent, this);
            World.Only<SubtitlesSetting>().ListenTo(Setting.Events.SettingChanged, SendDialogueSubtitleSettingsEvent, this);
            World.Only<FontSizeSetting>().ListenTo(Setting.Events.SettingChanged, SendFontSizeSettingsEvent, this);
            World.Only<DialogueAutoAdvance>().ListenTo(Setting.Events.SettingChanged, SendDialogueAutoAdvanceSettingsEvent, this);
            World.Only<PerspectiveSetting>().ListenTo(Setting.Events.SettingChanged, SendPerspectiveSettingEvent, this);
            World.Only<TppCameraDistanceSetting>().ListenTo(Setting.Events.SettingChanged, SendTppDistanceEvent, this);
            World.Only<AimAssistSetting>().ListenTo(Setting.Events.SettingChanged, SendAimAssistSettingsEvent, this);
            World.Only<ConsoleUISetting>().ListenTo(Setting.Events.SettingChanged, SendConsoleUISettingsEvent, this);
            //Graphics
            World.Only<GraphicPresets>().ListenTo(Setting.Events.SettingChanged, SendGraphicsPresetSettingsEvent, this);
            World.Only<AntiAliasing>().ListenTo(Setting.Events.SettingChanged, SendAntiAliasingSettingsEvent, this);
            World.Only<GeneralGraphics>().ListenTo(Setting.Events.SettingChanged, SendGeneralQualitySettingsEvent, this);
            World.Only<TextureQuality>().ListenTo(Setting.Events.SettingChanged, SendTextureQualitySettingsEvent, this);
            World.Only<Vegetation>().ListenTo(Setting.Events.SettingChanged, SendVegetationQualitySettingsEvent, this);
            World.Only<VfxQuality>().ListenTo(Setting.Events.SettingChanged, SendVFXQualitySettingsEvent, this);
            World.Only<FogQuality>().ListenTo(Setting.Events.SettingChanged, SendFogQualitySettingsEvent, this);
            World.Only<SSAO>().ListenTo(Setting.Events.SettingChanged, SendSSAOSettingsEvent, this);
            World.Only<MotionBlurSetting>().ListenTo(Setting.Events.SettingChanged, SendMotionBlurSettingsEvent, this);
            World.Only<ChromaticAberrationSetting>().ListenTo(Setting.Events.SettingChanged, SendChromaticAberrationSettingsEvent, this);
            World.Only<Shadows>().ListenTo(Setting.Events.SettingChanged, SendShadowsSettingsEvent, this);
            World.Only<Reflections>().ListenTo(Setting.Events.SettingChanged, SendReflectionsSettingsEvent, this);
            World.Only<SSS>().ListenTo(Setting.Events.SettingChanged, SendSSSSettingsEvent, this);
            World.Only<UpScaling>().ListenTo(Setting.Events.SettingChanged, SendUpScalingSettingsEvent, this);
        }

        async UniTaskVoid SendGamepadEvent() {
            await UniTask.Delay(10000);
            if (HasBeenDiscarded || !Application.isPlaying) {
                return;
            }
            //ControllerType controller = RewiredHelper.ActiveController();
            //AnalyticsUtils.SendDesignEvent($"General:Settings:Controller:{controller.ToString()}");
        }

        void SendDifficultySettingEvent(Setting setting) => SendDifficultySettingEvent(setting, DefaultSettingsPrefix);
        void SendDifficultySettingEvent(Setting setting, string prefix) {
            if (World.Any<ChooseDifficulty>()) {
                return;
            }
            DifficultySetting difficultySetting = (DifficultySetting) setting;
            string newDifficulty = difficultySetting.Difficulty.EnumName;
            string evt = $"Difficulty";
            AnalyticsUtils.SendDesignEvent($"General:{prefix}:{evt}:{newDifficulty}");
        }
        
        void SendFOVSettingEvent(Setting setting) {
            FOVSetting fovSetting = (FOVSetting) setting;
            string evt = $"FOV";
            AnalyticsUtils.SendDesignEvent($"General:Settings:{evt}", (int) fovSetting.FOV);
        }
        
        void SendShowHUDSettingEvent(Setting setting) {
            ShowUIHUD hudSetting = (ShowUIHUD) setting;
            string hudState = hudSetting.HUDEnabled ? "Enabled" : "Disabled";
            string compassState = hudSetting.CompassEnabled ? "Enabled" : "Disabled";
            string questTrackerState = hudSetting.QuestsEnabled ? "Enabled" : "Disabled";
            
            string hudEvt = "ShowHUD";
            string compassEvt = "ShowCompass";
            string questTrackerEvt = "ShowQuestTracker";
            
            AnalyticsUtils.SendDesignEvent($"General:Settings:{hudEvt}:{hudState}");
            AnalyticsUtils.SendDesignEvent($"General:Settings:{compassEvt}:{compassState}");
            AnalyticsUtils.SendDesignEvent($"General:Settings:{questTrackerEvt}:{questTrackerState}");
        } 
        
        void SendCameraShakesProactiveSettingEvent(Setting setting) => SendCameraShakesProactiveSettingEvent(setting, DefaultSettingsPrefix);
        void SendCameraShakesProactiveSettingEvent(Setting setting, string prefix) {
            if (World.Any<FirstTimeSettings>()) {
                return;
            }
            ScreenShakesProactiveSetting shakesProactiveSetting = (ScreenShakesProactiveSetting) setting;
            string state = shakesProactiveSetting.Enabled ? "Enabled" : "Disabled";
            string evt = "CameraShakesProactive";
            AnalyticsUtils.SendDesignEvent($"General:{prefix}:{evt}:{state}");
        } 
        
        void SendCameraShakesReactiveSettingEvent(Setting setting) => SendCameraShakesReactiveSettingEvent(setting, DefaultSettingsPrefix);
        void SendCameraShakesReactiveSettingEvent(Setting setting, string prefix) {
            if (World.Any<FirstTimeSettings>()) {
                return;
            }
            ScreenShakesReactiveSetting shakesReactiveSetting = (ScreenShakesReactiveSetting) setting;
            string state = shakesReactiveSetting.Enabled ? "Enabled" : "Disabled";
            string evt = "CameraShakesReactive";
            AnalyticsUtils.SendDesignEvent($"General:{prefix}:{evt}:{state}");
        }

        void SendHeadBobbingSettingEvent(Setting setting) {
            HeadBobbingSetting headBobbingSetting = (HeadBobbingSetting) setting;
            string state = headBobbingSetting.Intensity > 0f ? "Enabled" : "Disabled";
            string evt = "CameraShakesReactive";
            AnalyticsUtils.SendDesignEvent($"General:Settings:{evt}:{state}", headBobbingSetting.Intensity);
        }
                
        // -- Graphics Settings
        
        void SendGraphicsPresetSettingsEvent(Setting setting) {
            GraphicPresets graphicsSetting = (GraphicPresets) setting;
            string quality = graphicsSetting.ActivePreset.EnumName;
            string evt = "GraphicsPreset";
            AnalyticsUtils.SendDesignEvent($"General:GraphicsSettings:{evt}:{quality}");
        }
        
        void SendAntiAliasingSettingsEvent(Setting setting) {
            AntiAliasing graphicsSetting = (AntiAliasing) setting;
            string quality;
            if (graphicsSetting.TAA) {
                quality = "TAA";
            } else if (graphicsSetting.SMAA) {
                quality = "SMAA";
            } else if (graphicsSetting.FXAA) {
                quality = "FXAA";
            } else {
                quality = "none";
            }
            string evt = "AntiAliasingPreset";
            AnalyticsUtils.SendDesignEvent($"General:GraphicsSettings:{evt}:{quality}");
        }

        void SendGeneralQualitySettingsEvent(Setting setting) {
            GeneralGraphics graphicsSetting = (GeneralGraphics) setting;
            string quality = graphicsSetting.ActiveIndex switch {
                0 => "Low",
                1 => "Medium",
                2 => "High",
                3 => "Ultra",
                _ => "Unknown"
            };
            string evt = "GeneralQuality";
            AnalyticsUtils.SendDesignEvent($"General:GraphicsSettings:{evt}:{quality}");
        }
        
        void SendTextureQualitySettingsEvent(Setting setting) {
            string quality = QualitySettings.globalTextureMipmapLimit switch {
                0 => "FullRes",
                1 => "HalfRes",
                2 => "QrtRes",
                _ => "Unknown"
            };
            string evt = "TextureQuality";
            AnalyticsUtils.SendDesignEvent($"General:GraphicsSettings:{evt}:{quality}");
        }
        
        void SendVegetationQualitySettingsEvent(Setting setting) {
            string quality = PrefMemory.Get("VegetationQuality") switch {
                0 => "Low",
                1 => "Medium",
                2 => "High",
                3 => "Ultra",
                _ => "Unknown",
            };
            string evt = "VegetationQuality";
            AnalyticsUtils.SendDesignEvent($"General:GraphicsSettings:{evt}:{quality}");
        }
        
        void SendVFXQualitySettingsEvent(Setting setting) {
            VfxQuality graphicsSetting = (VfxQuality) setting;
            string quality = graphicsSetting.ActiveIndex switch {
                0 => "Low",
                1 => "Medium",
                2 => "High",
                3 => "Ultra",
                _ => "Unknown"
            };
            string evt = "VfxQuality";
            AnalyticsUtils.SendDesignEvent($"General:GraphicsSettings:{evt}:{quality}");
        }
        
        void SendFogQualitySettingsEvent(Setting setting) {
            FogQuality graphicsSetting = (FogQuality) setting;
            string quality = graphicsSetting.Quality switch {
                0 => "Low",
                1 => "Medium",
                2 => "High",
                3 => "Ultra",
                _ => "Unknown"
            };
            string evt = "FogQuality";
            AnalyticsUtils.SendDesignEvent($"General:GraphicsSettings:{evt}:{quality}");
        }
        
        void SendSSAOSettingsEvent(Setting setting) {
            SSAO graphicsSetting = (SSAO) setting;
            string state = graphicsSetting.Enabled ? "Enabled" : "Disabled";
            string evt = "SSAO";
            AnalyticsUtils.SendDesignEvent($"General:GraphicsSettings:{evt}:{state}");
        } 
        
        void SendMotionBlurSettingsEvent(Setting setting) {
            MotionBlurSetting graphicsSetting = (MotionBlurSetting) setting;
            string state = graphicsSetting.Enabled ? "Enabled" : "Disabled";
            string evt = "MotionBlur";
            AnalyticsUtils.SendDesignEvent($"General:GraphicsSettings:{evt}:{state}");
        }

        void SendChromaticAberrationSettingsEvent(Setting setting) {
            ChromaticAberrationSetting graphicsSetting = (ChromaticAberrationSetting)setting;
            string state = graphicsSetting.Enabled ? "Enabled" : "Disabled";
            string evt = "ChromaticAberration";
            AnalyticsUtils.SendDesignEvent($"General:GraphicsSettings:{evt}:{state}");
        }
        
        void SendShadowsSettingsEvent(Setting setting) {
            Shadows graphicsSetting = (Shadows) setting;
            string state = graphicsSetting.ShadowsEnabled ? "Enabled" : "Disabled";
            int distance = (int) (graphicsSetting.ShadowsDistance * 100);
            string evt = "Shadows";
            AnalyticsUtils.SendDesignEvent($"General:GraphicsSettings:{evt}:{state}", distance);
            
            state = graphicsSetting.ContactShadowsEnabled ? "Enabled" : "Disabled";
            evt = "ShadowsContact";
            AnalyticsUtils.SendDesignEvent($"General:GraphicsSettings:{evt}:{state}");
        } 
        
        void SendReflectionsSettingsEvent(Setting setting) {
            Reflections graphicsSetting = (Reflections) setting;
            string state = graphicsSetting.Enabled ? "Enabled" : "Disabled";
            string evt = "Reflections";
            AnalyticsUtils.SendDesignEvent($"General:GraphicsSettings:{evt}:{state}");
        } 
        
        void SendSSSSettingsEvent(Setting setting) {
            SSS graphicsSetting = (SSS) setting;
            string state = graphicsSetting.Enabled ? "Enabled" : "Disabled";
            string evt = "SSS";
            AnalyticsUtils.SendDesignEvent($"General:GraphicsSettings:{evt}:{state}");
        }

        void SendUpScalingSettingsEvent(Setting setting) {
            UpScaling graphicsSetting = (UpScaling)setting;
            string state = graphicsSetting.IsUpScalingEnabled ? "Enabled" : "Disabled";
            int quality = (int)(graphicsSetting.SliderValue * 100);
            string evt = "UpScaling";
            string prefix = graphicsSetting.ActiveUpScalingType.ToString();
            AnalyticsUtils.SendDesignEvent($"General:GraphicsSettings:{evt}:{prefix}_{state}", quality);
        }

        void SendDialogueSubtitleSettingsEvent(Setting setting) => SendDialogueSubtitleSettingsEvent(setting, DefaultSettingsPrefix);
        void SendDialogueSubtitleSettingsEvent(Setting setting, string prefix) {
            if (World.Any<FirstTimeSettings>()) {
                return;
            }
            SubtitlesSetting subtitleSetting = (SubtitlesSetting)setting;
            string state = subtitleSetting.SubsEnabled ? "Enabled" : "Disabled";
            string evt = "Subs";
            AnalyticsUtils.SendDesignEvent($"General:{prefix}:{evt}:{state}");
            state = subtitleSetting.EnviroSubsEnabled ? "Enabled" : "Disabled";
            evt = "EnviroSubs";
            AnalyticsUtils.SendDesignEvent($"General:{prefix}:{evt}:{state}");
            state = subtitleSetting.AreNamesShownInDialogues ? "Enabled" : "Disabled";
            evt = "Names";
            AnalyticsUtils.SendDesignEvent($"General:{prefix}:{evt}:{state}");
            state = subtitleSetting.AreNamesShownOutsideDialogues ? "Enabled" : "Disabled";
            evt = "OutsideNames";
            AnalyticsUtils.SendDesignEvent($"General:{prefix}:{evt}:{state}");
        }
        
        void SendFontSizeSettingsEvent(Setting setting) => SendFontSizeSettingsEvent(setting, DefaultSettingsPrefix);
        void SendFontSizeSettingsEvent(Setting setting, string prefix) {
            if (World.Any<FirstTimeSettings>()) {
                return;
            }
            FontSizeSetting subtitleSetting = (FontSizeSetting)setting;
            string size = subtitleSetting.ActiveFontSize.EnumName;
            string evt = "FontSize";
            AnalyticsUtils.SendDesignEvent($"General:{prefix}:{evt}:{size}");
        }
        
        void SendDialogueAutoAdvanceSettingsEvent(Setting setting) => SendDialogueAutoAdvanceSettingsEvent(setting, DefaultSettingsPrefix);
        void SendDialogueAutoAdvanceSettingsEvent(Setting setting, string prefix) {
            if (World.Any<FirstTimeSettings>()) {
                return;
            }
            DialogueAutoAdvance subtitleSetting = (DialogueAutoAdvance)setting;
            string state = subtitleSetting.Enabled ? "Enabled" : "Disabled";
            string evt = "DialogueAutoAdvance";
            AnalyticsUtils.SendDesignEvent($"General:{prefix}:{evt}:{state}");
        }

        void SendTppDistanceEvent(Setting setting) {
            var perspectiveSetting = World.Any<PerspectiveSetting>();
            if (perspectiveSetting is { IsTPP: true }) {
                SendPerspectiveSettingEvent(perspectiveSetting);
            }
        }
        
        void SendPerspectiveSettingEvent(Setting setting) => SendPerspectiveSettingEvent(setting, DefaultSettingsPrefix);
        void SendPerspectiveSettingEvent(Setting setting, string prefix) {
            if (World.Any<FirstTimeSettings>()) {
                return;
            }
            PerspectiveSetting perspectiveSetting = (PerspectiveSetting)setting;
            string state = perspectiveSetting.IsTPP ? "TPP" : "FPP";
            string evt = "Perspective";
            float distance = perspectiveSetting.IsTPP 
                ? World.Any<TppCameraDistanceSetting>()?.TppCameraDistance ?? 0f
                : 0f;
            AnalyticsUtils.SendDesignEvent($"General:{prefix}:{evt}:{state}", distance);
        }

        void SendAimAssistSettingsEvent(Setting setting) => SendAimAssistSettingsEvent(setting, DefaultSettingsPrefix);
        void SendAimAssistSettingsEvent(Setting setting, string prefix) {
            if (World.Any<FirstTimeSettings>()) {
                return;
            }
            AimAssistSetting aimAssistSetting = (AimAssistSetting)setting;
            string state = aimAssistSetting.Enabled ? (aimAssistSetting.HighAssistEnabled ? "High" : "Low") : "Disabled";
            string evt = "AimAssist";
            AnalyticsUtils.SendDesignEvent($"General:{prefix}:{evt}:{state}");
        }
        
        void SendConsoleUISettingsEvent(Setting setting) => SendConsoleUISettingsEvent(setting, DefaultSettingsPrefix);
        void SendConsoleUISettingsEvent(Setting setting, string prefix) {
            if (World.Any<FirstTimeSettings>()) {
                return;
            }
            ConsoleUISetting consoleUISetting = (ConsoleUISetting)setting;
            string state = consoleUISetting.Enabled ? "Enabled" : "Disabled";
            string evt = "ConsoleUI";
            AnalyticsUtils.SendDesignEvent($"General:{prefix}:{evt}:{state}");
        }

        void OnFirstTimeSettingsApplied(Model model) {
            var subtitleSetting = World.Any<SubtitlesSetting>();
            if (subtitleSetting != null) {
                SendDialogueSubtitleSettingsEvent(subtitleSetting, FirstTimeSettingsPrefix);
            }
            var fontSizeSetting = World.Any<FontSizeSetting>();
            if (fontSizeSetting != null) {
                SendFontSizeSettingsEvent(fontSizeSetting, FirstTimeSettingsPrefix);
            }
            var autoAdvanceSetting = World.Any<DialogueAutoAdvance>();
            if (autoAdvanceSetting != null) {
                SendDialogueAutoAdvanceSettingsEvent(autoAdvanceSetting, FirstTimeSettingsPrefix);
            }
            var perspectiveSetting = World.Any<PerspectiveSetting>();
            if (perspectiveSetting != null) {
                SendPerspectiveSettingEvent(perspectiveSetting, FirstTimeSettingsPrefix);
            }
            var screenShakeProactiveSetting = World.Any<ScreenShakesProactiveSetting>();
            if (screenShakeProactiveSetting != null) {
                SendCameraShakesProactiveSettingEvent(screenShakeProactiveSetting, FirstTimeSettingsPrefix);
            }
            var screenShakeReactiveSetting = World.Any<ScreenShakesReactiveSetting>();
            if (screenShakeReactiveSetting != null) {
                SendCameraShakesReactiveSettingEvent(screenShakeReactiveSetting, FirstTimeSettingsPrefix);
            }
            var aimAssistSetting = World.Any<AimAssistSetting>();
            if (aimAssistSetting != null) {
                SendAimAssistSettingsEvent(aimAssistSetting, FirstTimeSettingsPrefix);
            }
            var consoleUISetting = World.Any<ConsoleUISetting>();
            if (consoleUISetting != null) {
                SendConsoleUISettingsEvent(consoleUISetting, FirstTimeSettingsPrefix);
            }
        }
        
        void OnFirstTimeDifficultySelected(Model model) {
            var difficultySetting = World.Any<DifficultySetting>();
            if (difficultySetting != null) {
                SendDifficultySettingEvent(difficultySetting, FirstTimeSettingsPrefix);
            }
        }
    }
}
#endif