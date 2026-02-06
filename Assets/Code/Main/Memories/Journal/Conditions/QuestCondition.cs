using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Memories.Journal.Conditions.Models;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Main.Memories.Journal.Conditions {
    [Serializable]
    public class QuestCondition : Condition {
        [SerializeField]
        QuestConditionData[] conditions = Array.Empty<QuestConditionData>();

        public IEnumerable<QuestTemplate> Quests => conditions.Select(c => c.quest.Get<QuestTemplate>());
        
        public override bool InvalidSetup() => conditions.IsEmpty();
        
        public override void Initialize(Model owner) {
            if (IsMet()) return;
            if (InvalidSetup()) {
                Log.Important?.Info("Invalid setup for QuestCondition");
                return;
            }
            
            if (CheckIfConditionsAreMet()) {
                ConditionsMet();
                return;
            }
            
            if (!owner.TryGetElement(out QuestAndObjectivesRuntime dataModel)) {
                dataModel = owner.AddElement<QuestAndObjectivesRuntime>();
            }
            dataModel.RegisterCondition(this);
        }

        bool CheckIfConditionsAreMet() {
            int conditionsCount = conditions.Length;
            foreach (var quest in World.All<Quest>()) {
                foreach (var condition in conditions) {
                    if (quest.Template.Equals(condition.Quest)) {
                        if (condition.state == QuestState.Active) {
                            if (quest.State == QuestState.NotTaken) {
                                return false;
                            }
                        } else if (!quest.State.Equals(condition.state)) {
                            return false;
                        }
                        conditionsCount--;
                    }
                }
            }
            return conditionsCount == 0;
        }
        
        public void OnQuestChanged(QuestAndObjectivesRuntime dataModel) {
            if (CheckIfConditionsAreMet()) {
                ConditionsMet();
                dataModel.UnregisterCondition(this);
            }
        }
        
#if UNITY_EDITOR
        public override string EDITOR_PreviewInfo() {
            if (InvalidSetup()) return "!!! Invalid setup !!!: " + base.EDITOR_PreviewInfo();
            string targetNames = string.Join(", ", Quests.Select(q => q.DebugName));
            return $"Have all Quests set to selected States: {targetNames}";
        }
#endif
    }

    [Serializable]
    public struct QuestConditionData {
        [TemplateType(typeof(QuestTemplate))]
        public TemplateReference quest;
        public QuestState state;
        
        public QuestTemplate Quest => quest.Get<QuestTemplate>();
    }
}