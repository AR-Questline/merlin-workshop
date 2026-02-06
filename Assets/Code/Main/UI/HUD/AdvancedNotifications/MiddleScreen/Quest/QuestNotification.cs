namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.Quest {
    public partial class QuestNotification : AdvancedNotification {
        public readonly QuestData questData;
        public override bool IsValid => questData.questState == questData.quest.State;

        public QuestNotification(QuestData questData) {
            this.questData = questData;
        }
    }
}