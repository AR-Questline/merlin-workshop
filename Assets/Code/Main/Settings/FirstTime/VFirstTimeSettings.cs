using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Graphics.Transitions;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Main.Settings.Options.Views;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Helpers;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.MVC.UI.Handlers.Hovers;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Animations;
using Awaken.Utility.Collections;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Settings.FirstTime {
    [UsesPrefab("Settings/" + nameof(VFirstTimeSettings))]
    public class VFirstTimeSettings : View<FirstTimeSettings>, IPromptHost {
        [Title("General")]
        [SerializeField] TMP_Text title;
        [SerializeField] Transform promptHost;

        [Title("Settings description")]
        [SerializeField] GameObject settingDescSection;
        [SerializeField] TMP_Text settingName;
        [SerializeField] TMP_Text settingDesc;

        [Title("Settings sections")]
        [SerializeField] Transform firstSection;
        [SerializeField] Transform secondSection;
        [SerializeField] Transform thirdSection;
        
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();
        public Transform PromptsHost => promptHost;
        readonly List<IVSetting> _spawnedSettings = new();
        readonly List<IVSetting> _gamepadDependentSettings = new();
        public (ISetting setting, Transform host)[] Settings { get; private set; } = Array.Empty<(ISetting setting, Transform host)>();
        DialogueSubtitleSetting _subtitleSetting;
        ScreenShakesAllInOneSetting _screenShakeSetting;
        ConsoleUISetting _consoleUISetting;
        FontSizeSetting _fontSizeSetting;
        AimAssistSetting _aimAssistSetting;

        protected override void OnFullyInitialized() {
            title.SetText(LocTerms.GeneralGameSettingsTitle.Translate());
            settingDescSection.SetActiveOptimized(false);
            
            PrepareSettings();
            SpawnSettingsSection(Settings);
            CreateNavigation();
            
            World.EventSystem.ListenTo(EventSelector.AnySource, Hovering.Events.HoverChanged, this, OnHoverChanged);
            World.EventSystem.ListenTo(EventSelector.AnySource, Focus.Events.ControllerChanged, this, RefreshDependentOptions);
            World.Services.Get<TransitionService>().ToCamera(1).Forget();
        }

        void PrepareSettings() {
            _subtitleSetting = World.Only<SettingsMaster>().AddElement(new DialogueSubtitleSetting());
            _screenShakeSetting = World.Only<SettingsMaster>().AddElement(new ScreenShakesAllInOneSetting());
            _consoleUISetting = World.Only<ConsoleUISetting>();
            _fontSizeSetting = World.Only<FontSizeSetting>();
            _aimAssistSetting = World.Only<AimAssistSetting>();
            
            _consoleUISetting.Option.onChange += OnConsoleUISettingChanged;
            _fontSizeSetting.EnumOption.onChange += OnFontSizeSettingChanged;
            
            Settings = new (ISetting setting, Transform host)[] {
                (_subtitleSetting, firstSection),
                (_fontSizeSetting, firstSection),
                (World.Only<DialogueAutoAdvance>(), firstSection),
                (World.Only<PerspectiveSetting>(), secondSection),
                (_screenShakeSetting, secondSection),
                (_aimAssistSetting, secondSection),
                (_consoleUISetting, thirdSection)
            };
        }
        
        void RefreshDependentOptions() {
            if (RewiredHelper.IsGamepad) {
                foreach (var view in Target.Views.OfType<VFocusableSetting>()) {
                    if (_aimAssistSetting.Options.Contains(view.GenericOption)) {
                        return;
                    }
                }
                
                SettingsUtil.SpawnViews(Target, _aimAssistSetting, secondSection, _gamepadDependentSettings);
                _spawnedSettings.InsertRange(_spawnedSettings.Count - 1, _gamepadDependentSettings);
            }
            else {
                foreach (var setting in _gamepadDependentSettings) {
                    setting.Discard();
                }
                _gamepadDependentSettings.Clear();
            }
            
            CreateNavigation();
        }
        
        void SpawnSettingsSection((ISetting setting, Transform host)[] settingsToSpawn) {
            foreach (var toSpawn in settingsToSpawn.WhereNotNull()) {
                SettingsUtil.SpawnViews(Target, toSpawn.setting, toSpawn.host,
                    toSpawn.setting is AimAssistSetting ? _gamepadDependentSettings : _spawnedSettings);
            }
            
            _spawnedSettings.InsertRange(_spawnedSettings.Count - 1, _gamepadDependentSettings);
        }

        void CreateNavigation() {
            Selectable previous = null;
            foreach (var setting in _spawnedSettings) {
                previous = SettingsUtil.EstablishNavigation(previous, setting.MainSelectable);
            }

            // Cycle navigation
            Selectable firstSelectable = _spawnedSettings.FirstOrDefault(s => s.MainSelectable != null)?.MainSelectable;
            Selectable lastSelectable = _spawnedSettings.LastOrDefault(s => s.MainSelectable != null)?.MainSelectable;
            if (lastSelectable != null && firstSelectable != null) {
                lastSelectable.ChangeNavi(n => {
                    n.selectOnDown = firstSelectable;
                    return n;
                });
                firstSelectable.ChangeNavi(n => {
                    n.selectOnUp = lastSelectable;
                    return n;
                });
            }
            
            World.Only<Focus>().Select(firstSelectable);
        }
        
        void OnHoverChanged(HoverChange hoverChange) {
            if (hoverChange.View == null || !hoverChange.Hovered) return;
            
            var view = Target.Views.FirstOrDefault(v => v as View == hoverChange.View) as VFocusableSetting;
            if (view == null) return;
            
            foreach (var sTuple in Settings) {
                if (sTuple.setting.Options.Contains(view.GenericOption)) {
                    SetNameAndDesc(sTuple.setting);
                    settingDescSection.SetActiveOptimized(true);
                    return;
                }
            }
            
            settingDescSection.SetActiveOptimized(false);
        }
        
        void SetNameAndDesc(ISetting hoveredSetting) {
            switch (hoveredSetting) {
                case null:
                    return;
                case DialogueSubtitleSetting:
                    settingName.text = LocTerms.SettingsDialogueSubtitlesOption.Translate();
                    settingDesc.text = LocTerms.SettingsDialogueSubtitlesDescription.Translate();
                    break;
                case FontSizeSetting:
                    settingName.text = LocTerms.SettingsFontSize.Translate();
                    settingDesc.text = LocTerms.SettingsFontSizeDescription.Translate();
                    break;
                case DialogueAutoAdvance:
                    settingName.text = LocTerms.SettingsDialogueAutoAdvance.Translate();
                    settingDesc.text = LocTerms.SettingsDialogueAutoAdvanceDescription.Translate();
                    break;
                case PerspectiveSetting:
                    settingName.text = LocTerms.SettingsPerspective.Translate();
                    object key = UIUtils.Key(KeyBindings.Gameplay.ChangeHeroPerspective);
                    settingDesc.text = LocTerms.SettingsPerspectiveDescription.Translate(key);
                    break;
                case ScreenShakesAllInOneSetting:
                    settingName.text = LocTerms.SettingsScreenShakeAll.Translate();
                    settingDesc.text = LocTerms.SettingsScreenShakeAllDescription.Translate();
                    break;
                case AimAssistSetting:
                    settingName.text = _aimAssistSetting.SettingName;
                    settingDesc.text = PlatformUtils.IsMicrosoft ? LocTerms.SettingsAimAssistMicrosoftDescription.Translate() : LocTerms.SettingsAimAssistDescription.Translate();
                    break;
                case ConsoleUISetting:
                    settingName.text = LocTerms.SettingsConsoleUI.Translate();
                    settingDesc.text = LocTerms.SettingsConsoleUIDescription.Translate();
                    break;
            }
        }

        void OnConsoleUISettingChanged(bool changed) {
            _fontSizeSetting.SetFontOption(FontSize.Medium);
            _consoleUISetting.Apply(out bool _);
        }
        
        void OnFontSizeSettingChanged(ToggleOption optionChanged) {
            _fontSizeSetting.Apply(out bool _);
        }
        
        protected override IBackgroundTask OnDiscard() {
            _consoleUISetting.Option.onChange -= OnConsoleUISettingChanged;
            _fontSizeSetting.EnumOption.onChange -= OnFontSizeSettingChanged;
            _subtitleSetting.Discard();
            _screenShakeSetting.Discard();
            return base.OnDiscard();
        }
    }
}