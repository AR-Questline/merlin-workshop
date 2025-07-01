using Awaken.TG.Assets;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.UI.HUD.Notifications {
    [SpawnsView(typeof(VNotificationItem))]
    public partial class NotificationItem : Element<Notification> {
        public sealed override bool IsNotSaved => true;

        public SpriteReference iconRef;
        public string iconString;
        public string message;
        public int? amount;

        public NotificationItem(SpriteReference iconRef, string iconString, string message, int? amount) {
            this.iconRef = iconRef;
            this.iconString = iconString;
            this.message = message;
            this.amount = amount;
        }
    }
}