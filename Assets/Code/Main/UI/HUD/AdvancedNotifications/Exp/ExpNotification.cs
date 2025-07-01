using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.Exp {
    public partial class ExpNotification : Element<ExpNotificationBuffer>, IAdvancedNotification {
        public sealed override bool IsNotSaved => true;

        public readonly float gainedXP;
        
        public ExpNotification(float gainedXP) {
            this.gainedXP = gainedXP;
        }
    }
}