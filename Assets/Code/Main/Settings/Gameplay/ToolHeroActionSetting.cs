using System;
using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Gameplay {
    public partial class ToolHeroActionSetting : Setting {
        
        readonly ToggleOption _toggle;
        readonly DependentOption _dependentOption;
        readonly ToggleOption _mining;
        readonly ToggleOption _lumbering;
        readonly ToggleOption _digging;
        readonly ToggleOption _fishing;
        
        public override IEnumerable<PrefOption> Options => _dependentOption.Yield();
        public sealed override string SettingName => LocTerms.SettingsUseToolsWithoutPrompt.Translate();
        string MiningSettingName => LocTerms.SettingsUseToolsWithoutPromptMining.Translate();
        string LumberingSettingName => LocTerms.SettingsUseToolsWithoutPromptLumbering.Translate();
        string DiggingSettingName => LocTerms.SettingsUseToolsWithoutPromptDigging.Translate();
        string FishingSettingName => LocTerms.SettingsUseToolsWithoutPromptFishing.Translate();
        
        public ToolHeroActionSetting() {
            _toggle = new ToggleOption("Setting_ToolHeroAction", SettingName, true, true);
            _mining = new ToggleOption("Setting_ToolHeroAction_Mining", MiningSettingName, false, true);
            _lumbering = new ToggleOption("Setting_ToolHeroAction_Lumbering", LumberingSettingName, false, true);
            _digging = new ToggleOption("Setting_ToolHeroAction_Digging", DiggingSettingName, false, true);
            _fishing = new ToggleOption("Setting_ToolHeroAction_Fishing", FishingSettingName, true, true);

            _dependentOption = new DependentOption(_toggle, _mining, _lumbering, _digging, _fishing);
        }

        public bool AllowHeroAction(ToolType tool) {
            if (!tool.CanHeroActionBeDisabled) {
                return true;
            }
            if (!_toggle.Enabled) {
                return false;
            }
            if (tool == ToolType.Mining) {
                return _mining.Enabled;
            }
            if (tool == ToolType.Lumbering) {
                return _lumbering.Enabled;
            }
            if (tool == ToolType.Digging) {
                return _digging.Enabled;
            }
            if (tool == ToolType.Fishing) {
                return _fishing.Enabled;
            }
            throw new ArgumentOutOfRangeException($"Tool type {tool} is unsupported");
        }
    }
}