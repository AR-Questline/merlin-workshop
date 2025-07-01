using System;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Specs {
    [Serializable, InlineProperty]
    public class ObjectiveChange {
        [HorizontalGroup, HideLabel]
        public ObjectiveSpecBase objective;
        [HorizontalGroup]
        public ObjectiveStateFlag changeFrom = ObjectiveStateFlag.Inactive;
        [HorizontalGroup]
        public ObjectiveState changeTo = ObjectiveState.Active;

        public bool ShouldChangeFrom(ObjectiveState currentState) {
            if (currentState == changeTo) return false;
            return changeFrom.HasThisState(currentState);
        }
    }
}