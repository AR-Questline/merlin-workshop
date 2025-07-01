using System.Collections.Generic;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Gameplay {
    public partial class ShowUIHUD : Setting {
        const string ShowUIHUDPrefId = "Setting_ShowUIHUD";
        const string ShowUICompassPrefId = "Setting_UICompass";
        const string ShowUIQuestsPrefId = "Setting_ShowUIQuests";
        
        readonly DependentOption _mainOption;
        readonly ToggleOption _hudToggle;
        readonly ToggleOption _compassToggle;
        readonly ToggleOption _questsToggle;
        
        public sealed override string SettingName => LocTerms.SettingsShowUIHUD.Translate();
        public override IEnumerable<PrefOption> Options => _mainOption.Yield();
        public bool HUDEnabled => _hudToggle.Enabled;
        public bool CompassEnabled => HUDEnabled && _compassToggle.Enabled;
        public bool QuestsEnabled => HUDEnabled && _questsToggle.Enabled;
        
        public ShowUIHUD() {
            _hudToggle = new ToggleOption(ShowUIHUDPrefId, SettingName, true, true);
            _hudToggle.AddTooltip(LocTerms.ShowUIHUDSettingTooltip.Translate);
            
            _compassToggle = new ToggleOption(ShowUICompassPrefId, LocTerms.SettingsUICompass.Translate(), true, true);
            _compassToggle.AddTooltip(LocTerms.ShowCompassSettingTooltip.Translate);
            
            _questsToggle = new ToggleOption(ShowUIQuestsPrefId, LocTerms.SettingsShowUIQuests.Translate(), true, true);
            _questsToggle.AddTooltip(LocTerms.ShowUIQuestsSettingTooltip.Translate);
            
            _mainOption = new DependentOption(_hudToggle, _compassToggle, _questsToggle);
        }
    }
}