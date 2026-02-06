namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.LocationDiscovery {
    public partial class NewLocationNotification : AdvancedNotification {
        public readonly NewLocationNotificationData locationNotificationData;
        
        public NewLocationNotification(NewLocationNotificationData data) {
            locationNotificationData = data;
        }
    }
}