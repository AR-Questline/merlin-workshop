using Awaken.TG.Assets;
using Awaken.TG.Main.General.StatTypes;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.Proficiency {
    public readonly struct ProficiencyData {
        public readonly string skillName;
        public readonly int newSkillLevel;
        public readonly ShareableSpriteReference proficiencyIcon;
        
        public ProficiencyData(ProfStatType profStatType, int newSkillLevel) {
            this.skillName = profStatType.DisplayName;
            this.newSkillLevel = newSkillLevel;
            this.proficiencyIcon = profStatType.GetIcon?.Invoke();
        }
    }
}