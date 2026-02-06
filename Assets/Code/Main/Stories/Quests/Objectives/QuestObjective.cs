using System;
using Awaken.TG.Main.Stories.Quests.Objectives.Specs;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.TG.Main.Templates;

namespace Awaken.TG.Main.Stories.Quests.Objectives {
    [Serializable]
    public struct QuestObjective {
        [TemplateType(typeof(QuestTemplate))] public TemplateReference questTemplate;
        public string objectiveGuid;

        public QuestTemplate Quest => questTemplate.Get<QuestTemplate>();

        public ObjectiveSpecBase GetObjectiveSpec() {
            foreach (var objectiveSpec in Quest.ObjectiveSpecs.value) {
                if (objectiveSpec.Guid.Equals(objectiveGuid)) {
                    return objectiveSpec;
                }
            }
            return null;
        }
    }
}
