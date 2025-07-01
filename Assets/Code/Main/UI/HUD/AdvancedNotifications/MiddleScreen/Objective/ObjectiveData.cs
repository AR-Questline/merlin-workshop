using Q = Awaken.TG.Main.Stories.Quests;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.Objective {
    public readonly struct ObjectiveData {
        public readonly Q.Objectives.Objective objective;
        public readonly Q.Quest quest;

        public ObjectiveData(Q.Objectives.Objective objective) {
            this.objective = objective;
            quest = objective.ParentModel;
        }
    }
}