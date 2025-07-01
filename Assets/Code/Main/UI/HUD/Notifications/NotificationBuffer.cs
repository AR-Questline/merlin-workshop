using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.UI.HUD.Notifications {
    /// <summary>
    /// Allows to turn multiple notifications with the same title in the same frame into one notification with multiple messages.
    /// </summary>
    [SpawnsView(typeof(VNotificationBuffer))]
    public partial class NotificationBuffer : Element<HUD> {
        public sealed override bool IsNotSaved => true;
        
        // hacky way to prevent notifications to appear at the start of the game
        bool _blocked;

        public bool Blocked {
            get => _blocked || AdvancedNotificationBuffer.AllNotificationsSuspended;
            [UnityEngine.Scripting.Preserve] set => _blocked = value;
        }

        Dictionary<string, Notification> _buffer = new Dictionary<string, Notification>();

        public void ClearBuffer() {
            _buffer.Clear();
        }

        // push empty notification
        public Notification PushNotification(string title, bool buffered = true) {
            if (Blocked) {
                return null;
            }
            
            if (buffered && _buffer.TryGetValue(title, out Notification bufferedNotification)) {
                return bufferedNotification;
            } else {
                var notification = new Notification(title);
                AddElement(notification);
                if (buffered) {
                    _buffer[title] = notification;
                }

                return notification;
            }
        }

        // push notification with item with amount value
        public Notification PushNotification(string title, SpriteReference icon, string iconString, string message, int? amount = null, bool buffered = true) {
            var notification = PushNotification(title, buffered);
            if (notification == null) {
                return null;
            }
            
            if (!string.IsNullOrEmpty(message)) {
                NotificationItem item = null;
                if (amount != null) {
                    item = notification.Elements<NotificationItem>()
                        .FirstOrDefault(e => e.amount != null && e.message == message);
                }
                if (item == null) {
                    item = new NotificationItem(icon, iconString, message, amount);
                    notification.AddElement(item);
                } else {
                    item.amount += amount;
                    item.TriggerChange();
                }
            }

            return notification;
        }
    }
}