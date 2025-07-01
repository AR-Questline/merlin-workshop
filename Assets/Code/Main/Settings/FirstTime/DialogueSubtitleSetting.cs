using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Settings.FirstTime {
    public partial class DialogueSubtitleSetting : Setting {
        const string PrefId = "Dialogue";
        readonly List<ToggleOption> _toggleOptions = new();
        
        public sealed override string SettingName => LocTerms.SettingsDialogueSubtitlesOption.Translate();
        public override IEnumerable<PrefOption> Options => EnumOption.Yield();
        EnumArrowsOption EnumOption { get; }
        
        public DialogueSubtitleSetting() {
            foreach (DialogueSubtitleType preset in RichEnum.AllValuesOfType<DialogueSubtitleType>()) {
                var option = new ToggleOption($"{PrefId}_{preset.EnumName}", preset.DisplayName, preset == DialogueSubtitleType.Dialogue, true); 
                _toggleOptions.Add(option);
            }

            ToggleOption defaultOption = _toggleOptions.FirstOrDefault(option => option.DefaultValue);
            EnumOption = new EnumArrowsOption(PrefId, SettingName, defaultOption, true, _toggleOptions.ToArray());
        }

        protected override void OnApply() {
            if (EnumOption.Option.ID == $"{PrefId}_{DialogueSubtitleType.Dialogue.EnumName}") {
                ApplySettings(true, false);
            } else if (EnumOption.Option.ID == $"{PrefId}_{DialogueSubtitleType.DialogueEnvironment.EnumName}") {
                ApplySettings(true, true);
            } else {
                ApplySettings(false, false);
            }
        }

        static void ApplySettings(bool dialogue, bool enviro) {
            World.Only<SubtitlesSetting>().SetDialoguesSubtitle(dialogue, enviro);
        }
    }
    
    public class DialogueSubtitleType : RichEnum {
        readonly string _nameKey;
        public string DisplayName => _nameKey.Translate();

        [UnityEngine.Scripting.Preserve] 
        public static readonly DialogueSubtitleType
            Off = new(nameof(Off), LocTerms.Off),
            Dialogue = new(nameof(Dialogue), LocTerms.SettingsDialogueSubtitles),
            DialogueEnvironment = new(nameof(DialogueEnvironment), LocTerms.SettingsDialogueEnvironmentSubtitles);
        
        DialogueSubtitleType(string enumName, string nameKey) : base(enumName) {
            _nameKey = nameKey;
        }
    }
}