using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.General;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Development;
using Awaken.TG.Main.Heroes.Stats.StatConfig;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Stories.Quests.Objectives;
using Awaken.TG.Main.Stories.Quests.Objectives.Specs;
using Awaken.TG.Main.Stories.Quests.Objectives.Trackers;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.TG.Main.Stories.Quests.UI;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility.Extensions;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Quests {
    public static class QuestUtils {

        // === Context IDs
        static bool s_autoRunningObjectives;
        static readonly Stack<AutoRunWaitingStackEntry> AutoRunObjectivesWaitingStack = new();
        
        public static void EDITOR_RuntimeReset() {
            s_autoRunningObjectives = false;
            AutoRunObjectivesWaitingStack.Clear();
        }

        public static string ContextID(TemplateReference questRef) => questRef.GUID;
        public static string ContextID(Quest quest) => quest.Template.GUID;
        public static string ContextID(TemplateReference questTemp, string objectiveGuid) => $"{ContextID(questTemp)}/{objectiveGuid}";
        public static string ContextID(Objective objective) => $"{ContextID(objective.ParentModel)}/{objective.Guid}";
        
        // === Events
        public static class Events {
            public static readonly Event<Quest, QuestStateChange> QuestAdded = new(nameof(QuestAdded));
            public static readonly Event<Quest, QuestStateChange> QuestStateChanged = new(nameof(QuestStateChanged));
            public static readonly Event<Quest, QuestStateChange> QuestCompleted = new(nameof(QuestCompleted));
            public static readonly Event<Quest, QuestStateChange> QuestFailed = new(nameof(QuestFailed));
            public static readonly Event<Quest, ObjectiveStateChange> ObjectiveChanged = new(nameof(ObjectiveChanged));
            public static readonly Event<Objective, Objective> ObjectiveCompleted = new(nameof(ObjectiveCompleted));
            public static readonly Event<Objective, bool> ObjectiveRelatedStoryFlagChanged = new(nameof(ObjectiveRelatedStoryFlagChanged));
            public static readonly Event<Quest, Quest> ActiveObjectivesEstablished = new(nameof(ActiveObjectivesEstablished));
        }

        public class QuestStateChange {
            public Quest quest;
            [UnityEngine.Scripting.Preserve] public QuestState oldState;
            public QuestState newState;
        }

        public class ObjectiveStateChange {
            public Objective objective;
            public ObjectiveState oldState;
            public ObjectiveState newState;
        }
        
        struct AutoRunWaitingStackEntry {
            public Quest quest;
            public IEnumerable<ObjectiveChange> objectiveChanges;
            
            public AutoRunWaitingStackEntry(Quest quest, IEnumerable<ObjectiveChange> objectiveChanges) {
                this.quest = quest;
                this.objectiveChanges = objectiveChanges;
            }
        }
        
        // === Quests
        
        public static Quest Find(TemplateReference questRef) =>
            World.All<Quest>().FirstOrDefault(q => q.Template.GUID == ContextID(questRef));
        
        static void SetState(TemplateReference questRef, QuestState state) {
            Quest quest = Find(questRef);
            if (quest != null) {
                SetState(quest, state);
            } else {
                World.Services.Get<GameplayMemory>().Context(ContextID(questRef)).Set("state", state);
            }
        }

        static void SetState(Quest quest, QuestState state) {
            QuestState previousState = quest.State;
            World.Services.Get<GameplayMemory>().Context(ContextID(quest)).Set("state", state);
            RunStateChangeEvents(quest, previousState, state);
        }

        static void RunStateChangeEvents(Quest quest, QuestState oldState, QuestState newState) {
            QuestStateChange stateChange = new() {
                quest = quest,
                oldState = oldState,
                newState = newState,
            };
            quest.Trigger(Events.QuestStateChanged, stateChange);
            if (oldState == QuestState.NotTaken) {
                quest.Trigger(Events.QuestAdded, stateChange);
            }
            if (newState == QuestState.Completed) {
                quest.Trigger(Events.QuestCompleted, stateChange);
            }
            if (newState == QuestState.Failed) {
                quest.Trigger(Events.QuestFailed, stateChange);
            }
            quest.TriggerChange();
        }
        
        static void Activate(Quest quest) {
            SetState(quest, QuestState.Active);
            AutoRunObjectives(quest, quest.Template.AutoRunObjectives.Select(o => new ObjectiveChange() {
                objective = o,
                changeTo = ObjectiveState.Active,
            }));
        }

        static void Fail(TemplateReference questRef) {
            FinishActiveObjectives(questRef, asFailed: true);
            SetState(questRef, QuestState.Failed);
        }

        static void Fail(Quest quest) {
            FinishActiveObjectives(quest, asFailed: true);
            SetState(quest, QuestState.Failed);
        }

        static void Complete(Quest quest, bool completeActiveObjectives) {
            bool failObjectives = !completeActiveObjectives && !quest.Template.AutoCompleteLeftObjectives;
            FinishActiveObjectives(quest, failObjectives);
            SetState(quest, QuestState.Completed);
        }

        public static void Complete(TemplateReference questRef, bool completeActiveObjectives) {
            QuestTemplateBase temp = questRef.Get<QuestTemplateBase>();
            bool failObjectives = !completeActiveObjectives && !temp.AutoCompleteLeftObjectives;
            FinishActiveObjectives(questRef, failObjectives);
            SetState(questRef, QuestState.Completed);
        }

        public static void SetQuestState(Quest quest, QuestState questState) {
            switch (questState) {
                case QuestState.NotTaken:
                    SetState(quest, QuestState.NotTaken);
                    break;
                case QuestState.Active:
                    Activate(quest);
                    break;
                case QuestState.Completed:
                    Complete(quest, true);
                    break;
                case QuestState.Failed:
                    Fail(quest);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(questState), questState, null);
            }
        }
        
        public static void SetQuestState(TemplateReference questRef, QuestState questState) {
            switch (questState) {
                case QuestState.NotTaken:
                    SetState(questRef, QuestState.NotTaken);
                    break;
                case QuestState.Active:
                    throw new InvalidOperationException("Cannot set quest state to active without quest instance");
                case QuestState.Completed:
                    Complete(questRef, true);
                    break;
                case QuestState.Failed:
                    Fail(questRef);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(questState), questState, null);
            }
        }
        
        public static void TryAutocomplete(Quest quest) {
            QuestTemplateBase template = quest.Template;
            if (!template.AutoCompletion) return;

            bool completed = template.AutoCompleteAfter
                .All(autoObj => FindObjective(quest, autoObj.Guid)?.State == ObjectiveState.Completed);

            if (completed) {
                Complete(quest, false);
            }
        }

        public static bool AlreadyTaken(IMemory memory, TemplateReference questRef) =>
            StateOfQuestWithId(memory, questRef) != QuestState.NotTaken;

        public static QuestState StateOfQuestWithId(IMemory memory, TemplateReference questRef) =>
            memory.Context(ContextID(questRef)).Get("state", QuestState.NotTaken);
        
        // === Objectives
        
        static void FinishActiveObjectives(Quest quest, bool asFailed) {
            foreach (Objective obj in quest.Elements<Objective>()) {
                if (obj.State == ObjectiveState.Active) {
                    ChangeObjectiveState(obj, asFailed ? ObjectiveState.Failed : ObjectiveState.Completed);
                }
            }
        }

        static void FinishActiveObjectives(TemplateReference questRef, bool asFailed) {
            GameplayMemory memory = World.Services.Get<GameplayMemory>();
            using var objectiveSpecs = questRef.Get<QuestTemplateBase>().ObjectiveSpecs;
            foreach (ObjectiveSpecBase spec in objectiveSpecs.value) {
                if (StateOfObjective(memory, questRef, spec.Guid) == ObjectiveState.Active) {
                    ChangeObjectiveState(questRef, spec.Guid, asFailed ? ObjectiveState.Failed : ObjectiveState.Completed);
                }
            }
        }

        public static void AutoRunTrackerFulfillmentObjectives(Quest quest, BaseTracker tracker) {
            AutoRunObjectives(quest, tracker.ObjectiveChangesOnFulfilled);
        }
        
        public static void AutoRunTrackerFulfillmentLostObjectives(Quest quest, BaseTracker tracker) {
            AutoRunObjectives(quest, tracker.ObjectiveChangesOnLoseFulfillment);
        }

        static void AutoRunObjectives(Quest quest, IEnumerable<ObjectiveChange> objectives) {
            AutoRunObjectivesWaitingStack.Push(new AutoRunWaitingStackEntry(quest, objectives));
            if (s_autoRunningObjectives) {
                return;
            }
            AutoRunObjectivesInternal();
        }

        static void AutoRunObjectivesInternal() {
            s_autoRunningObjectives = true;
            while (AutoRunObjectivesWaitingStack.TryPop(out var entry)) {
                foreach (var objectiveChange in entry.objectiveChanges) {
                    var objectiveSpec = objectiveChange.objective;
                    Objective objective = FindObjective(entry.quest, objectiveSpec.Guid);
                    if (objectiveChange.ShouldChangeFrom(objective.State)) {
                        ChangeObjectiveState(objective, objectiveChange.changeTo);
                    }
                }
            }
            s_autoRunningObjectives = false;
        }

        static Objective FindObjective(Quest quest, string quid) => quest.Elements<Objective>().FirstOrDefault(o => o.Guid == quid);
        
        static void RefreshQuestAfterObjectiveChange(Objective objective, ObjectiveState oldState, ObjectiveState newState) {
            var quest = objective?.ParentModel;
            if (quest != null) {
                if (newState == ObjectiveState.Active) {
                    quest.DisplayedByPlayer = false;
                } else if (newState == ObjectiveState.Completed) {
                    AutoRunObjectives(quest, objective.AutoRunAfterCompletion);
                } else if (newState == ObjectiveState.Failed) {
                    AutoRunObjectives(quest, objective.AutoRunAfterFailure);
                }
                
                quest.Trigger(Events.ObjectiveChanged, new ObjectiveStateChange {objective = objective, oldState = oldState, newState = newState});
                quest.TriggerChange();
            }
        }

        public static void ChangeObjectiveState(Objective objective, ObjectiveState newState) {
            string contextId = ContextID(objective);
            ObjectiveState oldState = objective.State;
            World.Services.Get<GameplayMemory>().Context(contextId).Set("state", newState);

            RefreshQuestAfterObjectiveChange(objective, oldState, newState);
        }

        public static void ChangeObjectiveState(TemplateReference questRef, string objectiveGuid, ObjectiveState newState, bool allowChangeFromFinalStates = false) {
            Quest quest = Find(questRef);
            string contextId = ContextID(questRef, objectiveGuid);
            Objective objective = quest?.Objectives.FirstOrDefault(obj => obj.ContextID == contextId);
            var oldState = objective?.State;
            if (!allowChangeFromFinalStates && (oldState == ObjectiveState.Completed || oldState == ObjectiveState.Failed)) {
                return;
            }
            World.Services.Get<GameplayMemory>().Context(contextId).Set("state", newState);

            if (objective != null && oldState != newState) {
                RefreshQuestAfterObjectiveChange(objective, oldState.Value, newState);
            }
        }
        
        // === Helpers
        public static string GetGainedXPInfo(float gainedXP) {
            return $"+{gainedXP:F0} {LocTerms.ExperienceShort.Translate()}";
        }

        public static ObjectiveState StateOfObjective(IMemory memory, TemplateReference templateRef, string objectiveGuid) {
            return memory.Context(ContextID(templateRef, objectiveGuid)).Get("state", ObjectiveState.Inactive);
        }
        
        public static float CalculateXp(int questLvl, StatDefinedRange questMultiRange, float customPoints) {
            if (questMultiRange == StatDefinedRange.Custom) return customPoints;
            
            float randomPick = CalculateXpRange(questLvl, questMultiRange, customPoints).RandomPick();
            return HeroDevelopment.RoundExp(randomPick);
        }
        
        public static FloatRange CalculateXpRange(int questLvl, StatDefinedRange questMultiRange, float customPoints) {
            if (questMultiRange == StatDefinedRange.Custom) {
                return new FloatRange(customPoints, customPoints);
            }
            FloatRange multiplierRange = StatDefinedValuesConfig.GetRange(HeroStatType.XP, questMultiRange) ?? new FloatRange(0f, 0f);
            float expForLvl = HeroDevelopment.RequiredExpFor(Mathf.Max(questLvl, 2));
            return multiplierRange * expForLvl;
        }

        public static bool HasThisState(this ObjectiveStateFlag flags, ObjectiveState state) {
            ObjectiveStateFlag stateAsFlag = (ObjectiveStateFlag)(1 << (int)state);
            return flags.HasFlagFast(stateAsFlag);
        }

        public static string ToTranslatedString(this QuestType type) {
            return type switch {
                QuestType.Main => LocTerms.QuestTypeMain.Translate(),
                QuestType.Side => LocTerms.QuestTypeSide.Translate(),
                QuestType.Misc => LocTerms.QuestTypeMisc.Translate(),
                _ => "Unknown",
            };
        }
    }
}