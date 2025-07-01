using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.Objective {
    public partial class ObjectiveNotification : Element<ObjectiveNotificationBuffer>, IAdvancedNotification {
        public readonly ObjectiveData objectiveData;
        
        bool IAdvancedNotification.IsValid => objectiveData.quest.State != QuestState.Completed;
        
        public ObjectiveNotification(ObjectiveData objectiveData) {
            this.objectiveData = objectiveData;
        }
    }
}