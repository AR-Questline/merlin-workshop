using Awaken.TG.Main.UI.HUD.AdvancedNotifications.BufferBlockers;
using Awaken.TG.MVC.Attributes;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen {
    [SpawnsView(typeof(VMiddleScreenNotificationBuffer))]
    public partial class MiddleScreenNotificationBuffer : AdvancedNotificationBuffer<AdvancedNotification>, IAdvancedBufferWithBlocker<MiddleScreenBufferBlocker> {
    }
}