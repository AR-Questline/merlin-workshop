using System;
using Awaken.TG.Graphics.FloatingTexts;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.UI.HUD.Notifications;
using Awaken.TG.MVC;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.ActionLogs {
    /// <summary>
    /// Contains all ways to show log
    /// </summary>
    public class ActionLogDisplayTarget : RichEnum {
        public Action<ARLog, object> showLog;

        public static readonly ActionLogDisplayTarget HUDFloatText = new(nameof(HUDFloatText), HUDFloatTextAction);

        public static readonly ActionLogDisplayTarget Notification = new(nameof(Notification), NotificationAction);

        ActionLogDisplayTarget(string enumName, Action<ARLog, object> showLog) : base(enumName) {
            this.showLog = showLog;
        }

        // === Show methods
        static void HUDFloatTextAction(ARLog log, object payload) {
            World.Only<HUD>().View<VFloatingTextHUD>()
                .ShowText("<color=#AF9257>" + log.Content + "</color>", (byte) payload);
        }
        
        static void NotificationAction(ARLog log, object payload) {
            if (payload is NotificationData data) {
                data.Push(log.Content);
            } else {
                World.Only<NotificationBuffer>().PushNotification(log.Content, false);
            }
        }
    }
}