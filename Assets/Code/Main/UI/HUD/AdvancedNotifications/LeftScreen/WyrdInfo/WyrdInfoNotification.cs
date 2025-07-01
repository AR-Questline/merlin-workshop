using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.WyrdInfo {
    public partial class WyrdInfoNotification : Element<WyrdInfoNotificationBuffer>, IAdvancedNotification {
        public sealed override bool IsNotSaved => true;

        public readonly string information;

        public WyrdInfoNotification(string information) {
            this.information = information;
        }
    }
}