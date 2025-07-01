using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.Main.Settings.Audio;
using Awaken.TG.Main.Settings.Controls;
using Awaken.TG.Main.Settings.Debug;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Rewired;
using UnityEngine.Rendering;
using Volume = Awaken.TG.Main.Settings.Audio.Volume;

namespace Awaken.TG.Main.Settings {
    /// <summary>
    /// Model that creates and manages all of the settings in the game.
    /// It also allows simple Unity objects like MonoBehaviour, to attach pseudo-listeners, so that they can listen to specific settings.
    /// At game start, it should sets graphic settings to calculated user default. 
    /// </summary>
    public partial class SettingsMaster : Model {
        public override Domain DefaultDomain => Domain.Globals;
        public sealed override bool IsNotSaved => true;

        // === Queries
        public ModelsSet<ISetting> Settings => Elements<ISetting>();
        public IEnumerable<ISetting> GraphicSettings => _graphicSettings;
        public IEnumerable<ISetting> DisplaySettings => _displaySettings;
        public IEnumerable<ISetting> AudioSettings => _audioSettings;
        public IEnumerable<ISetting> GeneralSettings => _generalSettings;
        public IEnumerable<ISetting> AccessibilitySettings => _accessibilitySettings;
        public IEnumerable<GameControls> ControlsSettings => _controlsSettings;
        public IEnumerable<GameControls> GamepadControlsSettings => _gamepadControlsSettings;
        public IEnumerable<ISetting> GameplaySettings => _gameplaySettings;
        //Player.ControllerHelper ControllerHelper => RewiredHelper.Player.controllers;
        
        List<ISetting> _graphicSettings = new();
        List<ISetting> _displaySettings = new();
        List<ISetting> _audioSettings = new();
        List<ISetting> _generalSettings = new();
        List<ISetting> _accessibilitySettings = new();
        List<GameControls> _controlsSettings = new();
        List<GameControls> _gamepadControlsSettings = new();
        List<ISetting> _gameplaySettings = new();

        ToggleOption _initialSettingsApplied = new("InitialSettingsApplied", "", false, false);
        readonly Preset _defaultPreset;
        
        public SettingsMaster(Preset defaultPreset = null) {
            _defaultPreset = defaultPreset;
            ModelElements.SetInitCapacity(60);
        }

        // === Initialization
        protected override void OnInitialize() {
#if LOCALIZATION_TESTS
            AddGameplaySetting(new GameLanguage());
#else
            if (PlatformUtils.IsDebug || PlatformUtils.IsMicrosoft || PlatformUtils.IsPS5) {
                AddGameplaySetting(new GameLanguage());
            }
#endif

            // display and graphics
            if (PlatformUtils.IsConsole) {
                AddGraphicSetting(new ScreenResolution());
                AddGraphicSetting(new GraphicPresets(_defaultPreset));
                AddGraphicSetting(new UpScaling());
                AddGraphicSetting(new ScreenDisplay());
                AddGraphicSetting(new FOVSetting());
                AddGraphicSetting(new ContrastSetting());
                AddGraphicSetting(new MotionBlurSetting());
                AddGraphicSetting(new GammaSetting());
            } else {
                AddDisplaySetting(new ScreenResolution());
                AddDisplaySetting(new ScreenDisplay());
                AddDisplaySetting(new FOVSetting());
                AddDisplaySetting(new MotionBlurSetting());
                AddDisplaySetting(new ContrastSetting());
                AddDisplaySetting(new GammaSetting());
                
                AddGraphicSetting(new GraphicPresets(_defaultPreset));
                AddGraphicSetting(new UpScaling());
            }
            
            AddGraphicSetting(new AntiAliasing());
            if (PlatformUtils.IsXbox) {
                AddGraphicSetting(new GeneralGraphicsXbox());
            } else {
                AddGraphicSetting(new GeneralGraphics());
            }

            AddGraphicSetting(new DistanceCullingSetting());
            AddGraphicSetting(new TextureQuality());
            AddGraphicSetting(new MipmapsBiasSetting());
            AddGraphicSetting(new Vegetation());
            AddGraphicSetting(new VfxQuality());
            AddGraphicSetting(new FogQuality());
            AddGraphicSetting(new SSAO());
            AddGraphicSetting(new ChromaticAberrationSetting());
            AddGraphicSetting(new Shadows());
            AddGraphicSetting(new Reflections());
            AddGraphicSetting(new SSS());

            //gameplay
            AddGameplaySetting(new DifficultySetting());
            AddGameplaySetting(new ShowTutorials());
            AddGameplaySetting(new AutoSaveSetting());
            AddGameplaySetting(new PerspectiveSetting());
            AddGameplaySetting(new TppCameraDistanceSetting());
            AddGameplaySetting(new CameraSensitivity());
            AddGameplaySetting(new InvertCameraY());
            AddGameplaySetting(new CameraKillEffectSetting());
            AddGameplaySetting(new AimAssistSetting());
            AddGameplaySetting(new PadVibrations());
            AddGameplaySetting(new AdaptiveTriggers());
            AddGameplaySetting(new ShowUIHUD());
            AddGameplaySetting(new ReversedHandsSetting());
            AddGameplaySetting(new ToolHeroActionSetting());
            AddGameplaySetting(new QuickUseWheelSetting());
            AddGameplaySetting(new DisableHeroHelmetSetting());
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            AddGameplaySetting(new CPUAffinity());
#endif
            //AddGameplaySetting(new InvertGliderPitch());
            // accessibility
            AddAccessibilitySetting(new AimDeadzone());
            AddAccessibilitySetting(new HeadBobbingSetting());
            AddAccessibilitySetting(new FOVChanges());
            AddAccessibilitySetting(new ScreenShakesProactiveSetting());
            AddAccessibilitySetting(new ScreenShakesReactiveSetting());
            AddAccessibilitySetting(new MenuUIScale());
            AddAccessibilitySetting(new HUDScale());
            AddAccessibilitySetting(new HudBackgroundsIntensity());
            AddAccessibilitySetting(new ConsoleUISetting());
            AddAccessibilitySetting(new FontSizeSetting());
            AddAccessibilitySetting(new SubtitlesSetting());
            AddAccessibilitySetting(new DialogueAutoAdvance());
            // audio
            AddAudioSetting(new Volume(AudioGroup.MASTER));
            AddAudioSetting(new Volume(AudioGroup.MUSIC, 0.7f));
            AddAudioSetting(new Volume(AudioGroup.SFX));
            AddAudioSetting(new Volume(AudioGroup.VO));
            AddAudioSetting(new Volume(AudioGroup.VIDEO));
            AddAudioSetting(new InfluencerMode());
            AddAudioSetting(new DisableAudioInBackground());
            // controls
            AddControlsSetting(new GameControls(ControlScheme.KeyboardAndMouse));
            // data & others
            AddGeneralSetting(new CollectData());
            AddGeneralSetting(new ShowBugReporting());
            AddGeneralSetting(new ShowCreditsSetting());
            AddGeneralSetting(new PrivacyPolicy());
            AddGeneralSetting(new FlickerFixSetting());

            this.ListenTo(Events.AfterFullyInitialized, AfterInit, this);
        }

        void AfterInit() {
            AfterInitWhenPipelineCreated().Forget();
            World.EventSystem.ListenTo(EventSelector.AnySource, Focus.Events.ControllerChanged, this, () => InitOptionsForConnectedController());

            // foreach (var controller in ControllerHelper.Controllers.Where(c => c.type == ControllerType.Joystick)) {
            //     InitOptionsForConnectedController(controller);
            // }
        }
        
        public void InitOptionsForConnectedController(Controller controller = null) {
            // var newController = controller ?? ControllerHelper.GetLastActiveController();
            //
            // if (newController is not { type: ControllerType.Joystick }) {
            //     return;
            // }
            //
            // bool alreadyRegistered = _gamepadControlsSettings.Any(setting => setting.controllerIdentifier == newController.hardwareTypeGuid);
            //
            // if (!alreadyRegistered) {
            //     AddControlsSetting(new GameControls(ControlScheme.Gamepad, newController.id, newController.hardwareTypeGuid));
            // }
        }

        async UniTaskVoid AfterInitWhenPipelineCreated() {
            while (RenderPipelineManager.currentPipeline == null) {
                if (!await AsyncUtil.DelayFrame(this, 2)) {
                    return;
                }
            }
            if (!_initialSettingsApplied.Enabled) {
                Element<GraphicPresets>().ApplyInitialPreset();
                _initialSettingsApplied.Enabled = true;
                _initialSettingsApplied.Apply();
            }
            foreach (var setting in Settings) {
                setting.InitialApply();
            }
        }

        // === Public API
        public void Apply() {
            var requireRestart = new List<ISetting>();
            foreach (var setting in Settings) {
                setting.Apply(out bool restart);
                if (restart) {
                    requireRestart.Add(setting);
                }
            }

            if (requireRestart.Any()) {
                string settingNames = string.Join(", ", requireRestart.Select(static s => s.SettingName));
                PopupUI.SpawnNoChoicePopup(typeof(VSmallPopupUI), LocTerms.SettingsRestartRequiredDesc.Translate(settingNames), LocTerms.SettingsRestartRequired.Translate(settingNames));
            }
            PrefMemory.Save();
        }

        public void Cancel() {
            foreach (var setting in Settings) {
                setting.Cancel();
            }
            PrefMemory.Save();
        }
        
        public void RestoreDefaults(IEnumerable<ISetting> settings) {
            foreach (var setting in settings.Where(s => s is not IGraphicSetting)) {
                // Graphic settings are reset by GraphicPresets
                try {
                    setting.RestoreDefault();
                } catch (Exception e) {
                    Log.Important?.Error($"Can not restore setting {setting?.SettingName}");
                    UnityEngine.Debug.LogException(e);
                }
            }
            PrefMemory.Save();
        }

        public void PerformOnSceneChange() {
            foreach (var setting in Settings) {
                setting.PerformOnSceneChange();
            }
            PrefMemory.Save();
        }
        
        // === Helpers
        void AddGraphicSetting(Setting setting) {
            AddElement(setting);
            _graphicSettings.Add(setting);
        }

        void AddDisplaySetting(Setting setting) {
            AddElement(setting);
            _displaySettings.Add(setting);
        }

        void AddAudioSetting(Setting setting) {
            AddElement(setting);
            _audioSettings.Add(setting);
        }

        void AddGeneralSetting(Setting setting) {
            AddElement(setting);
            _generalSettings.Add(setting);
        }
        
        void AddAccessibilitySetting(Setting setting) {
            AddElement(setting);
            _accessibilitySettings.Add(setting);
        }

        void AddControlsSetting(GameControls controls) {
            AddElement(controls);
            if (controls.ControlScheme == ControlScheme.Gamepad) {
                _gamepadControlsSettings.Add(controls);
            } else {
                _controlsSettings.Add(controls);
            }
        }

        void AddGameplaySetting(Setting setting) {
            AddElement(setting);
            _gameplaySettings.Add(setting);
        }
    }
}