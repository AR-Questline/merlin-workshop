namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.Proficiency {
    public partial class ProficiencyNotification : AdvancedNotification {
        public ProficiencyData proficiencyData;

        public override bool IsMergeable => true;

        public ProficiencyNotification(ProficiencyData proficiencyData) {
            this.proficiencyData = proficiencyData;
        }
        
        public void OverrideProficiencyLevel(int level) {
            proficiencyData = new ProficiencyData(proficiencyData.skillName, level, proficiencyData.proficiencyIcon);
        }
    }
}