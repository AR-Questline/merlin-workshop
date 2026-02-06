using System.Collections.Generic;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Memories.Journal.Conditions.Models {
    public class QuestAndObjectivesRuntime : ConditionRuntime {
        public override bool IsNotSaved => true;

        Dictionary<QuestTemplateBase, List<QuestCondition>> _questAndConditions = new(10);
        Dictionary<string, List<ObjectiveCondition>> _objectiveAndConditions = new(10);

        protected override void OnInitialize() {
            base.OnInitialize();
            World.EventSystem.ListenTo(EventSelector.AnySource, QuestUtils.Events.QuestStateChanged, this, OnQuestChange);
            World.EventSystem.ListenTo(EventSelector.AnySource, QuestUtils.Events.ObjectiveChanged, this, OnObjectiveChange);
        }

        public void RegisterCondition(QuestCondition condition) {
            Register(_questAndConditions, condition, condition.Quests);
        }
        
        public void RegisterCondition(ObjectiveCondition condition) {
            Register(_objectiveAndConditions, condition, condition.ObjectiveGUIDs);
        }

        void Register<T1, T2>(Dictionary<T1, List<T2>> dictionary, T2 condition, IEnumerable<T1> conditionValues) {
            foreach (var value in conditionValues) {
                if (dictionary.TryGetValue(value, out var conditions)) {
                    conditions.Add(condition);
                } else {
                    dictionary[value] = new List<T2> {
                        condition
                    };
                }
            }
        }

        public void UnregisterCondition(QuestCondition condition) {
            Unregister(_questAndConditions, condition, condition.Quests);
            if (_questAndConditions.Count == 0 && _objectiveAndConditions.Count == 0) {
                Discard();
            }
        }
        
        public void UnregisterCondition(ObjectiveCondition condition) {
            Unregister(_objectiveAndConditions, condition, condition.ObjectiveGUIDs);
            if (_questAndConditions.Count == 0 && _objectiveAndConditions.Count == 0) {
                Discard();
            }
        }
        
        void Unregister<T1, T2>(Dictionary<T1, List<T2>> dictionary, T2 condition, IEnumerable<T1> conditionValues) {
            foreach (var value in conditionValues) {
                if (dictionary.TryGetValue(value, out var conditions)) {
                    if (conditions.Count <= 1) {
                        dictionary.Remove(value);
                    } else {
                        conditions.Remove(condition);
                    }
                }
            }
        }

        void OnQuestChange(QuestUtils.QuestStateChange change) {
            if (_questAndConditions.TryGetValue(change.quest.Template, out var conditions)) {
                for (int i = conditions.Count - 1; i >= 0; i--) {
                    conditions[i].OnQuestChanged(this);
                }
            }
        }

        void OnObjectiveChange(QuestUtils.ObjectiveStateChange change) {
            if (_objectiveAndConditions.TryGetValue(change.objective.Guid, out var conditions)) {
                for (int i = conditions.Count - 1; i >= 0; i--) {
                    conditions[i].OnObjectiveChanged(this);
                }
            }
        }
    }
}