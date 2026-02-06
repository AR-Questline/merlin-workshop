using Awaken.TG.Main.Stories.Quests;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.Objective {
    public partial class ObjectiveNotification : AdvancedNotification {
        public readonly ObjectiveData objectiveData;
        
        public override bool IsValid => objectiveData.quest.State != QuestState.Completed;
        
        public ObjectiveNotification(ObjectiveData objectiveData) {
            this.objectiveData = objectiveData;
        }
    }
}