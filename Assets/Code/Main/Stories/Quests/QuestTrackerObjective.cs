using System;
using Awaken.TG.Main.Stories.Quests.Objectives;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Stories.Quests {
    [SpawnsView(typeof(VQuestTrackerObjective))]
    public sealed partial class QuestTrackerObjective : Element<QuestTracker> {
        public sealed override bool IsNotSaved => true;

        public readonly Objective objective;
        
        public QuestTrackerObjective(Objective objective) {
            this.objective = objective;
        }

        public void TryToDiscard() {
            if (objective.State is ObjectiveState.Active or ObjectiveState.Completed or ObjectiveState.Failed) {
                return;
            }
            Discard();
        }

        public readonly struct ObjectiveComparer : IEquatable<QuestTrackerObjective> {
            public readonly Objective objective;

            public ObjectiveComparer(Objective objective) {
                this.objective = objective;
            }

            public bool Equals(QuestTrackerObjective other) {
                return Equals(objective, other.objective);
            }

            public override int GetHashCode() {
                return (objective != null ? objective.GetHashCode() : 0);
            }
        }
    }
}