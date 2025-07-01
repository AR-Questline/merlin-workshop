using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.LocationDiscovery {
    public partial class NewLocationNotification : Element<LocationDiscoveryBuffer>, IAdvancedNotification {
        public sealed override bool IsNotSaved => true;

        public readonly NewLocationNotificationData locationNotificationData;
        
        public NewLocationNotification(NewLocationNotificationData data) {
            locationNotificationData = data;
        }
    }
}