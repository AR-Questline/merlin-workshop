using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.Quest {
    public partial class QuestNotification : Element<QuestNotificationBuffer>, IAdvancedNotification {
        public sealed override bool IsNotSaved => true;
        public readonly QuestData questData;
        bool IAdvancedNotification.IsValid => questData.questState == questData.quest.State;

        public QuestNotification(QuestData questData) {
            this.questData = questData;
        }
    }
}