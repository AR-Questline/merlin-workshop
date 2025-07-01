using Awaken.TG.Main.Stories.Quests;
using Q = Awaken.TG.Main.Stories.Quests.Quest;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.Quest {
    public readonly struct QuestData {
        public readonly string questName;
        public readonly QuestState questState;
        public readonly float gainedXP;
        public readonly Q quest;
        
        public QuestData(QuestUtils.QuestStateChange questStateChange) {
            questName = questStateChange.quest.DisplayName;
            questState = questStateChange.newState;
            gainedXP = questStateChange.quest.ExperiencePoints;
            quest = questStateChange.quest;
        }
    }
}