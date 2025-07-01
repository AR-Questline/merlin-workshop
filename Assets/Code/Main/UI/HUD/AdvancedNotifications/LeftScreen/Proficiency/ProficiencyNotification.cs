using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.Proficiency {
    public partial class ProficiencyNotification : Element<ProficiencyNotificationBuffer>, IAdvancedNotification {
        public sealed override bool IsNotSaved => true;

        public readonly ProficiencyData proficiencyData;
        
        public ProficiencyNotification(ProficiencyData proficiencyData) {
            this.proficiencyData = proficiencyData;
        }
    }
}