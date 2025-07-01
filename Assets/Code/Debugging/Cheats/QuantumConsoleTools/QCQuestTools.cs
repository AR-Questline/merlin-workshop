using System.Linq;
using Awaken.TG.Debugging.Cheats.QuantumConsoleTools.Suggestors;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.Stories.Quests.Objectives;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.TG.MVC;
using QFSW.QC;

namespace Awaken.TG.Debugging.Cheats.QuantumConsoleTools {
    public static class QCQuestTools {
        [Command("quest.add", "Adds a quest to the hero")] [UnityEngine.Scripting.Preserve]
        static void QuestAdd([TemplateSuggestion(typeof(QuestTemplate))] QuestTemplate questTemplate) {
            bool alreadyTaken = QuestUtils.AlreadyTaken(World.Services.Get<GameplayMemory>(), new(questTemplate.GUID));
            if (alreadyTaken) {
                QuantumConsole.Instance.LogToConsoleAsync($"Quest ({questTemplate.DebugName} {questTemplate.GUID}) is already in progress/completed");
                return;
            }

            Quest quest = new(questTemplate);
            World.Add(quest);
        }

        [Command("quest.objective-complete", "Completes an objective of the currently tracked quest")] [UnityEngine.Scripting.Preserve]
        static void QuestCompleteObjective([QuestObjectiveSuggestion]string objectiveName) {
            if (GetObjectiveFromTrackedQuest(objectiveName, out Objective objective)) {
                return;
            }
            QuestUtils.ChangeObjectiveState(objective, ObjectiveState.Completed);
        }

        [Command("quest.objective-activate", "Activate an objective of the currently tracked quest")]
        static void QuestActivateObjective([QuestObjectiveSuggestion]string objectiveName) {
            if (GetObjectiveFromTrackedQuest(objectiveName, out Objective objective)) {
                return;
            }
            QuestUtils.ChangeObjectiveState(objective, ObjectiveState.Active);
        }

        [Command("quest.objective-fail", "Fails an objective of the currently tracked quest")]
        static void QuestFailObjective([QuestObjectiveSuggestion]string objectiveName) {
            if (GetObjectiveFromTrackedQuest(objectiveName, out Objective objective)) {
                return;
            }
            QuestUtils.ChangeObjectiveState(objective, ObjectiveState.Failed);
        }

        [Command("quest.objective-deactivate", "Fails an objective of the currently tracked quest")]
        static void QuestDeactivateObjective([QuestObjectiveSuggestion]string objectiveName) {
            if (GetObjectiveFromTrackedQuest(objectiveName, out Objective objective)) {
                return;
            }
            QuestUtils.ChangeObjectiveState(objective, ObjectiveState.Inactive);
        }

        static bool GetObjectiveFromTrackedQuest([QuestObjectiveSuggestion]string objectiveName, out Objective objective) {
            var quest = Hero.Current.Element<QuestTracker>().ActiveQuest;
            objective = quest.Objectives.FirstOrDefault(o => o.Name == objectiveName);
            if (objective == null) {
                QuantumConsole.Instance.LogToConsoleAsync($"Objective {objectiveName} not found");
                return true;
            }
            return false;
        }

        [Command("quest.set-flag", "Sets a story flag")]
        static void QuestSetFlag([QuestFlagSuggestion] string flagName, bool value) {
            StoryFlags.Set(flagName, value);
            QuantumConsole.Instance.LogToConsoleAsync($"Set {flagName} to {value}");
        }
    }
}
