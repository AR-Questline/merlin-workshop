using System.Collections.Generic;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Gameplay {
    public partial class DisableHeroHelmetSetting : Setting {
        readonly ToggleOption _toggle;
        
        public sealed override string SettingName => LocTerms.SettingsDisableHeroHelmet.Translate();
        public bool Enabled => _toggle.Enabled;
        public override IEnumerable<PrefOption> Options => _toggle.Yield();

        public DisableHeroHelmetSetting() {
            _toggle = new ToggleOption("Setting_ShowHeroHelmet", SettingName, false, true);
        }

        protected override void OnApply() {
            var hero = Hero.Current;
            if (hero == null) {
                return;
            }
            
            foreach (var item in hero.HeroItems.DistinctEquippedItems()) {
                var equip = item.Element<ItemEquip>();

                if (equip.EquipmentType != EquipmentType.Helmet) {
                    continue;
                }
                
                equip.ChangeHelmetVisibility(!Enabled);
            }
        }
    }
}