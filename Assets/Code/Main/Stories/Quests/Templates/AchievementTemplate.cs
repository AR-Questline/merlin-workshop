using System.Collections.Generic;
using Awaken.TG.Main.Stories.Quests.Objectives.Specs;
using Awaken.TG.Main.Stories.Quests.UI;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Quests.Templates {
    public class AchievementTemplate : QuestTemplateBase {
        [SerializeField] AchievementObjectiveSpec objective;
        
        public override QuestType TypeOfQuest => QuestType.Achievement;
        public override bool AutoCompleteLeftObjectives => true;
        public override IEnumerable<ObjectiveSpecBase> AutoRunObjectives => objective.Yield();
        public override bool AutoCompletion => true;
        public override IEnumerable<ObjectiveSpecBase> AutoCompleteAfter => objective.Yield();

        void OnValidate() {
            if (!objective) {
                objective = GetComponent<AchievementObjectiveSpec>();
            }
        }
    }
}