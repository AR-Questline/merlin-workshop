using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications {
    public abstract class VAdvancedNotificationBuffer : View<AdvancedNotificationBuffer>, IViewNotificationBuffer {
        [SerializeField] RectTransform notificationParent;
        [SerializeField] CanvasGroup bufferCanvasGroup;
        
        public RectTransform NotificationParent => notificationParent;
        public CanvasGroup BufferCanvasGroup => bufferCanvasGroup;
        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnHUD();

        protected override void OnInitialize() {
            Target.ListenTo(AdvancedNotificationBuffer.Events.BeforePushingFirstNotification, OnBeforePushingFirstNotification, this);
            Target.ListenTo(AdvancedNotificationBuffer.Events.AfterPushingLastNotification, OnAfterPushingLastNotification, this);
        }

        protected virtual void OnBeforePushingFirstNotification() { }
        protected virtual void OnAfterPushingLastNotification() { }
    }

    public interface IViewNotificationBuffer : IView {
        public RectTransform NotificationParent { get; }
    }
}