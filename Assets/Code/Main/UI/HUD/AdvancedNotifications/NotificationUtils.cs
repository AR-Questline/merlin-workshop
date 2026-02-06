using Awaken.TG.MVC;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications {
    public static class NotificationUtils {
        public static void Push<TNotification>(TNotification notification) where TNotification : AdvancedNotification {
            World.Any<AdvancedNotificationBuffer<TNotification>>()?.PushNotification(notification);
        }
        
        public static void PushWithFiltering<TNotification>(TNotification notification) where TNotification : AdvancedNotification {
            var buffer = World.Any<AdvancedNotificationBufferPresenter<TNotification>>();
            if (buffer == null) {
                return;
            }
            if (buffer.FilterNotification?.Invoke(notification) ?? true) {
                buffer.PushNotification(notification);
            }
        }

        public static void PushExplicitly<TBuffer, TNotification>(TNotification notification) where TBuffer : AdvancedNotificationBuffer<AdvancedNotification> where TNotification : AdvancedNotification {
            World.Any<TBuffer>()?.PushNotification(notification);
        }
    }
}