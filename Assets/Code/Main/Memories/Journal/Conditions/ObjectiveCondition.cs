using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Memories.Journal.Conditions.Models;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.Stories.Quests.Objectives;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Memories.Journal.Conditions {
    [Serializable]
    public class ObjectiveCondition : Condition {
        [SerializeField]
        ObjectiveConditionData[] conditions = Array.Empty<ObjectiveConditionData>();

        public IEnumerable<QuestTemplate> Quests => conditions.Select(c => c.Quest);
        public IEnumerable<string> ObjectiveGUIDs => conditions.Select(c => c.ObjectiveGuid);
        
        public override bool InvalidSetup() => conditions.IsEmpty();

        public override void Initialize(Model owner) {
            if (IsMet()) return;
            if (InvalidSetup()) {
                Log.Important?.Info("Invalid setup for ObjectiveCondition");
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
                        string conditionObjectiveGuid = condition.ObjectiveGuid;
                        if (quest.Objectives.Any(o => o.Guid == conditionObjectiveGuid)) {
                            ObjectiveState currentState = quest.Objectives.FirstOrDefault(o => o.Guid == conditionObjectiveGuid).State;
                            ObjectiveStateFlag currentStateFlag = (ObjectiveStateFlag) (1 << (int) currentState);
                            if (!condition.objectiveState.HasFlagFast(currentStateFlag)) {
                                return false;
                            }
                            conditionsCount--;
                        }
                    }
                }
            }
            return conditionsCount == 0;
        }

        public void OnObjectiveChanged(QuestAndObjectivesRuntime dataModel) {
            if (CheckIfConditionsAreMet()) {
                ConditionsMet();
                dataModel.UnregisterCondition(this);
            }
        }
        
#if UNITY_EDITOR
        public override string EDITOR_PreviewInfo() {
            if (InvalidSetup()) return "!!! Invalid setup !!!: " + base.EDITOR_PreviewInfo();
            string targetNames = string.Join(", ", Quests);
            return $"Have all Objectives set to selected States: {targetNames}";
        }
#endif
    }

    [Serializable]
    public struct ObjectiveConditionData {
        public QuestObjective questObjective;
        
        [ReadOnly]
        public ObjectiveState state;
        public ObjectiveStateFlag objectiveState;
        
        public QuestTemplate Quest => questObjective.Quest;
        public string ObjectiveGuid => questObjective.objectiveGuid;
    }
}