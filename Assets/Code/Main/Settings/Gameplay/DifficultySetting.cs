using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Settings.Gameplay {
    public partial class DifficultySetting : Setting {
        const string PrefId = "DifficultySetting";
        
        EnumArrowsOption _options;
        Difficulty _cachedDifficulty;
        string _lastDifficultyOptionId;
        public override string SettingName => LocTerms.SettingsDifficulty.Translate();
        public override IEnumerable<PrefOption> Options => _options?.Yield() ?? Enumerable.Empty<PrefOption>();
        public Difficulty Difficulty {
            get {
                if (_options == null) {
                    Log.Critical?.Error("DifficultySetting accessed before initialization!!!");
                    return Difficulty.Normal;
                }

                var optionID = _options.Option.ID;
                if ((optionID != _lastDifficultyOptionId) | (_cachedDifficulty == null)) {
                    _lastDifficultyOptionId = optionID;
                    _cachedDifficulty = RichEnum.FromName<Difficulty>(optionID);
                }

                return _cachedDifficulty;
            }
        }
        int OptionsCount { get; set; }
        
        public DifficultySetting() {
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelInitialized<Hero>(), this, CreateHeroDependentOptions);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<Hero>(), this, () => _options = null);
        }

        void CreateHeroDependentOptions() {
            var defaultDifficulty = Difficulty.Normal;
            var difficulties = RichEnum.AllValuesOfType<Difficulty>();
            OptionsCount = difficulties.Length;
            var toggleOptions = new ToggleOption[OptionsCount];
            int defaultIndex = 0;
            for (int i = 0; i < OptionsCount; i++) {
                var difficulty = difficulties[i];
                bool isDefault = difficulty == defaultDifficulty;
                if (isDefault) {
                    defaultIndex = i;
                }
                
                toggleOptions[i] = new ToggleOption(difficulty.EnumName, difficulty.Name, isDefault, true);
            }
            
            _options = new EnumArrowsOption(GetHeroDependentID(), SettingName, toggleOptions[defaultIndex], true, toggleOptions);
            _options.AddTooltip(LocTerms.DifficultySettingTooltip.Translate);
        }
        
        public void SetCurrentDifficulty(Difficulty difficulty) {
            var relatedOption = _options.Options.FirstOrDefault(toggleOption => toggleOption.ID == difficulty.EnumName);
            _options.Option = relatedOption;
            Apply(out _);
        }

        static string GetHeroDependentID() => (Hero.Current == null) ? null : Hero.Current.HeroID + PrefId;
    }
}