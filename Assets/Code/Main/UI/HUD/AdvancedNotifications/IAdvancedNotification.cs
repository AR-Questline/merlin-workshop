using Awaken.TG.MVC.Elements;
using FMODUnity;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications {
    /// <summary>
    /// Marker interface that any advanced notification has to implement. It's used in Advanced Notification Buffer
    /// </summary>
    public interface IAdvancedNotification : IElement {
        bool IsValid => true;
        void Show() { } //TODO: remove this method when we rewrite advanced notifications to UIToolkit
    }

    public interface IAdvancedNotificationsView {
        public EventReference NotificationSound { get; }
    }
    
}