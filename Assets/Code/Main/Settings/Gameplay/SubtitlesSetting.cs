using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Settings.Gameplay {
    public partial class SubtitlesSetting : Setting {
        const string PrefId = "Setting_Subtitles";
        
        readonly DependentOption _dependentOption;
        readonly ToggleOption _subs;
        readonly ToggleOption _envrioSubs;
        readonly EnumArrowsOption _actorNames;
        readonly EnumArrowsOption _colorOption;
        readonly SliderOption _backgroundSlider;
        readonly Dictionary<ToggleOption, Color> _presetByOption = new();
        readonly List<ToggleOption> _toggleOptions = new();
        
        public sealed override string SettingName { get; } = LocTerms.SettingsSubtitles.Translate();
        public override IEnumerable<PrefOption> Options => _dependentOption.Yield();
        
        public bool SubsEnabled => _subs?.Enabled ?? true;
        public bool EnviroSubsEnabled => SubsEnabled && (_envrioSubs?.Enabled ?? true);
        public bool AreNamesShownInDialogues => SubsEnabled && _actorNames.Option == Always;
        public bool AreNamesShownOutsideDialogues => SubsEnabled && _actorNames.Option != Never;
        public Color ActiveColor => _presetByOption[_colorOption.Option];
        public float BackgroundIntensity => _backgroundSlider.Value;
        
        static readonly ToggleOption Never = new(nameof(SubtitlesSetting.Never), LocTerms.Never.Translate(), false, true);
        static readonly ToggleOption OnlyOutsideOfDialogues = new(nameof(SubtitlesSetting.OnlyOutsideOfDialogues), LocTerms.OnlyOutsideOfDialogues.Translate(), true, true);
        static readonly ToggleOption Always = new(nameof(SubtitlesSetting.Always), LocTerms.Always.Translate(), false, true);
        
        public SubtitlesSetting(float defaultBackgroundSliderValue = 0.75f) {
            AddColor(ARColor.LightGrey, LocTerms.SubColorDefault.Translate());
            AddColor(ARColor.MainWhite, LocTerms.SubColorPureWhite.Translate());
            AddColor(ARColor.SubtitlesYellow, LocTerms.SubColorYellow.Translate());
            
            ToggleOption defaultColor = _toggleOptions.FirstOrDefault(o => o.DefaultValue);
            _subs = new ToggleOption($"{PrefId}_Subs", SettingName, true, true);
            _colorOption = new EnumArrowsOption($"{PrefId}_SubsColor", LocTerms.DialogueSubtitlesColor.Translate(), defaultColor, true, _toggleOptions.ToArray());
            _backgroundSlider = new SliderOption($"{PrefId}_SubsBackgroundsIntensity", LocTerms.SubtitlesBackgroundOpacity.Translate(), 0f, 1f, false, NumberWithPercentFormat, defaultBackgroundSliderValue, true, 0.05f);
            _actorNames = new EnumArrowsOption($"{PrefId}_ShowNamesInDialogues", LocTerms.SettingsShowNames.Translate(), OnlyOutsideOfDialogues, true, Never, OnlyOutsideOfDialogues, Always);
            _envrioSubs = new ToggleOption($"{PrefId}_EnviroSubs", LocTerms.SettingsEnviroSubtitles.Translate(), false, true);
            _dependentOption = new DependentOption(_subs, _colorOption, _backgroundSlider, _actorNames, _envrioSubs);
            _envrioSubs.AddTooltip(LocTerms.EnviroSubtitlesSettingTooltip.Translate);
            _actorNames.AddTooltip(LocTerms.ShowNamesSettingTooltip.Translate);
        }
        
        void AddColor(ARColor color, string name) {
            ToggleOption option = new($"{PrefId}_{name}", name, color == ARColor.LightGrey, false);
            _toggleOptions.Add(option);
            _presetByOption.Add(option, color);
        }

        public void SetDialoguesSubtitle(bool dialogueEnabled, bool enviroDialogueEnabled) {
            _subs.Enabled = dialogueEnabled;
            _envrioSubs.Enabled = enviroDialogueEnabled;
        }
    }
}