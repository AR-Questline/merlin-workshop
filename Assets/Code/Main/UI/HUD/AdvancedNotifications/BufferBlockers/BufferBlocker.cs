using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.BufferBlockers {
    /// <summary>
    /// Used for queueing notification buffers that should be displayed. For example when you don't want
    /// to display overlapped quest notifications and recipe notifications in the middle of the screen.
    /// If you want to be more specific about the types of buffer that should be blocked and when, you
    /// can use DependentBuffers property in the AdvancedNotificationBuffer.
    /// </summary>
    public abstract partial class BufferBlocker : Element<AdvancedNotificationBuffer> { }
}