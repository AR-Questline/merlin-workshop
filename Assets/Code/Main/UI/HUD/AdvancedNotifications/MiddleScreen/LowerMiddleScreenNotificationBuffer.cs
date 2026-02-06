using Awaken.TG.MVC.Attributes;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen {
    [SpawnsView(typeof(VLowerMiddleScreenNotificationBuffer))]
    public partial class LowerMiddleScreenNotificationBuffer : AdvancedNotificationBuffer<AdvancedNotification> {        
        protected override bool HideWhenMapNotInteractive => true;
    }
}