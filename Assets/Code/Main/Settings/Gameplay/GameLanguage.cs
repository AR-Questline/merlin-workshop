using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Main.UI.TitleScreen;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Awaken.TG.Main.Settings.Gameplay {
    public partial class GameLanguage : Setting {
        public const string ChosenLanguageKey = "ChosenLanguageKey";
        const string PrefId = "LanguageSet";
        
        // === Options
        PopupUI _popup;
        EnumArrowsOption _option;
        List<Locale> _languages;

        public sealed override string SettingName => LocTerms.SettingsLanguageSet.Translate();
        public override IEnumerable<PrefOption> Options => Services.TryGet<TitleScreen>() != null ? LanguageOption.Yield() : Enumerable.Empty<PrefOption>();
        protected override bool AutoApplyOnInit => false;

        EnumArrowsOption LanguageOption {
            get {
                if (_option == null || _option.Options.Count() != Languages.Count) {
                    ConfigureOption();
                }
                return _option;
            }
        }

        List<Locale> Languages {
            get {
                if (_languages == null || _languages.Count <= 0) {
                    _languages = LocalizationSettings.AvailableLocales.Locales.Where(l => l.Identifier.Code != "en-jm").ToList();
                }
                return _languages;
            }
        }

        void ConfigureOption() {
            List<ToggleOption> options = new();
            Locale selectedLocale = LocalizationSettings.SelectedLocale;
            foreach (var locale in Languages) {
                options.Add(new ToggleOption($"{PrefId}_{locale.LocaleName}", locale.LocaleName, locale == selectedLocale, false));
            }
            
            if (!options.Any(o => o.Enabled)) {
                options.First().Enabled = true;
            }

            _option = new EnumArrowsOption(PrefId, SettingName, options.First(o => o.Enabled), false, options.ToArray());
        }
        
        protected override void OnApply() {
            if (LanguageOption != null) {
                _popup = PopupUI.SpawnSimplePopup(typeof(VSmallPopupUI),
                    LocTerms.PopupGameLanguageChanged.Translate(),
                    PopupUI.AcceptTapPrompt(ApplyAndExit),
                    PopupUI.CancelTapPrompt(DiscardChange),
                    LocTerms.PopupGameLanguageChangedTitle.Translate()
                );
            }
        }

        void DiscardChange() {
            _popup?.Discard();
            LanguageOption.RestoreDefault();
            LanguageOption.Apply();
        }

        void ApplyAndExit() {
            Locale selectedLocale = GetSelectedLocale();
            LocalizationSettings.SelectedLocale = selectedLocale;
            PrefMemory.Set(ChosenLanguageKey, selectedLocale.LocaleName, true);
#if UNITY_PS5 && !UNITY_EDITOR
            PrefMemory.Save();
            SocialServices.PlayStationServices.PlayStationUtils.RestartGame();
#elif UNITY_GAMECORE
            PrefMemory.Save();
            SocialServices.MicrosoftServices.MicrosoftManager.RestartGame();
#else
            TitleScreenUI.Exit();
#endif
        }

        Locale GetSelectedLocale() {
            int index = LanguageOption.OptionInt;
            if (index < 0 || index >= Languages.Count) {
                index = 0;
                LanguageOption.Option = LanguageOption.Options.First();
            }
            return Languages[index];
        }
    }
}