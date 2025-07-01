using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.UI.HUD.Notifications {
    [SpawnsView(typeof(VNotification))]
    public partial class Notification : Element<NotificationBuffer> {
        public sealed override bool IsNotSaved => true;

        public string title;

        public Notification(string title) {
            this.title = title;
        }
    }
}