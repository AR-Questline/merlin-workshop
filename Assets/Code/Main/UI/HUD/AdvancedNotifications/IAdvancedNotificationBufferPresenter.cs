using Awaken.TG.MVC;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications {
    internal interface IAdvancedNotificationBufferPresenter : IModel {
        void ForceDisplayingNotifications();
    }
}