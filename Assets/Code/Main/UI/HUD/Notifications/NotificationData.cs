using Awaken.TG.Assets;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.UI.HUD.Notifications {
    public class NotificationData {
        public string message;
        public SpriteReference iconRef;
        public string iconString;
        public int? amount;
        public bool buffered = true;
        
        [UnityEngine.Scripting.Preserve]
        public NotificationData() { }
        public NotificationData(string message) {
            this.message = message;
        }
        
        [UnityEngine.Scripting.Preserve]
        public NotificationData(string message, SpriteReference iconRef) : this(message) {
            this.iconRef = iconRef;
        }

        public void Push(string title) {
            var buffer = World.Only<NotificationBuffer>();
            if (string.IsNullOrWhiteSpace(message)) {
                buffer.PushNotification(title, buffered);
            } else {
                buffer.PushNotification(title, iconRef, iconString, message, amount, buffered);
            }
        }
    }
}