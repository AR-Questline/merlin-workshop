#if !UNITY_GAMECORE && !UNITY_PS5
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.Stories.Quests.Objectives;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using UnityEngine.Localization.Settings;

namespace Awaken.TG.Main.Analytics {
    public partial class QuestAnalytics : Element<GameAnalyticsController> {
        public sealed override bool IsNotSaved => true;

        string QuestName(Quest quest) {
            string name = quest.Template.name;
            return NiceName(name);
        }
        string ObjectiveName(Objective objective) {
            string name = objective.Name;
            return NiceName(name);
        }
        string NiceName(string name) => AnalyticsUtils.EventName(name);
        int HeroLevel => AnalyticsUtils.HeroLevel;
        
        protected override void OnInitialize() {
            World.EventSystem.ListenTo(EventSelector.AnySource, QuestUtils.Events.QuestAdded, this, OnQuestAdded);
            World.EventSystem.ListenTo(EventSelector.AnySource, QuestUtils.Events.QuestCompleted, this, OnQuestCompleted);
            World.EventSystem.ListenTo(EventSelector.AnySource, QuestUtils.Events.QuestFailed, this, OnQuestFailed);
            World.EventSystem.ListenTo(EventSelector.AnySource, QuestUtils.Events.ObjectiveChanged, this, OnObjectiveChanged);
        }

        void OnQuestAdded(QuestUtils.QuestStateChange change) {
            if (string.IsNullOrWhiteSpace(change?.quest?.DisplayName)) {
                return;
            }

            AnalyticsUtils.TrySendProgressionEvent(GAProgressionStatus.Start, "Quests", QuestName(change.quest), "Quest", HeroLevel);
        }
        
        void OnQuestCompleted(QuestUtils.QuestStateChange change) {
            if (string.IsNullOrWhiteSpace(change?.quest?.DisplayName)) {
                return;
            }

            AnalyticsUtils.TrySendProgressionEvent(GAProgressionStatus.Complete, "Quests", QuestName(change.quest), "Quest", HeroLevel);
        }
        
        void OnQuestFailed(QuestUtils.QuestStateChange change) {
            if (string.IsNullOrWhiteSpace(change?.quest?.DisplayName)) {
                return;
            }

            AnalyticsUtils.TrySendProgressionEvent(GAProgressionStatus.Fail, "Quests", QuestName(change.quest), "Quest", HeroLevel);
        }
        
        void OnObjectiveChanged(QuestUtils.ObjectiveStateChange change) {
            Objective objective = change.objective;
            Quest quest = objective?.ParentModel;
            if (change.oldState == change.newState || string.IsNullOrWhiteSpace(objective?.Name)) {
                return;
            }

            switch (change.newState) {
                case ObjectiveState.Active:
                    AnalyticsUtils.TrySendProgressionEvent(GAProgressionStatus.Start, "Quests", QuestName(quest), ObjectiveName(objective), HeroLevel);
                    break;
                case ObjectiveState.Completed:
                    AnalyticsUtils.TrySendProgressionEvent(GAProgressionStatus.Complete, "Quests", QuestName(quest), ObjectiveName(objective), HeroLevel);
                    break;
                case ObjectiveState.Failed:
                    AnalyticsUtils.TrySendProgressionEvent(GAProgressionStatus.Fail, "Quests", QuestName(quest), ObjectiveName(objective), HeroLevel);
                    break;
            }
        }
    }
}
#endif