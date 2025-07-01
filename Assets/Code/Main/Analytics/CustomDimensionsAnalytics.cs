#if !UNITY_GAMECORE && !UNITY_PS5
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.Stories.Quests.Objectives;
using Awaken.TG.Main.Stories.Quests.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Sirenix.Utilities;

namespace Awaken.TG.Main.Analytics {
    public partial class CustomDimensionsAnalytics : Element<GameAnalyticsController> {
        static string[] MainQuestsOrder = {
            "IA_TheGreatEscape",
            "MQ_ShadowOfTheHorns"
        };

        static string[] TrackedObjectives = {
            //Prologue
            "FndCaradoc", 
            "Escape",
            "Boat",
            //Hos
            "GoToHorns", 
            "CheckCemetery",
            "ReportCemetaryToFaerghas",
            "CheckBodies",
            "ReportStronholdToFaerghas",
            "GoToAllmother",
            "GoToSewal",
            "FixGong",
            "TakeExcalibur"
        };

        public sealed override bool IsNotSaved => true;
        
        // === Initialization
        protected override void OnInitialize() {
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelFullyInitialized<Hero>(), this, UpdateAllDimensions);
            
            //Dificulty
            World.Only<DifficultySetting>().ListenTo(Setting.Events.SettingRefresh, SetDifficultyDimension, this);
            
            //QuestProgress
            World.EventSystem.ListenTo(EventSelector.AnySource, QuestUtils.Events.QuestCompleted, this, OnQuestCompleted);
            World.EventSystem.ListenTo(EventSelector.AnySource, QuestUtils.Events.ObjectiveCompleted, this, OnObjectiveCompleted);
        }

        // === Callbacks
        void OnQuestCompleted(QuestUtils.QuestStateChange change) {
            if (change.quest.QuestType == QuestType.Main && MainQuestsOrder.Contains(change.quest.Template.name)) {
                SetMainStoryProgressDimension(change.quest);
            }
        }
        
        void OnObjectiveCompleted(Objective objective) {
            if (objective.ParentModel.QuestType == QuestType.Main && TrackedObjectives.Contains(objective.Name))  {
                SetMainStoryProgressDimension(objective);
            }
        }

        void UpdateAllDimensions() {
            SetDifficultyDimension();
            SetMainStoryProgressDimension();
            SetPlaythroughDimension();
        }

        // --- Dimeension01
        void SetDifficultyDimension() {
            string dimension = World.Only<DifficultySetting>().Difficulty.EnumName;
            //GameAnalytics.SetCustomDimension01(dimension);
        }
        
        // --- Dimeension02
        void SetMainStoryProgressDimension() {
            IEnumerable<Quest> mainQuests = World.All<Quest>().Where(t => t.Template.TypeOfQuest == QuestType.Main)
                .Where(t => t.Objectives.Any(o => o.State == ObjectiveState.Completed && TrackedObjectives.Contains(o.Name)));
            string lastCompletedQuestName = MainQuestsOrder.LastOrDefault(q => mainQuests.Any(t => t.Template.name == q));
            Quest lastCompletedQuest = mainQuests.LastOrDefault(q => q.Template.name == lastCompletedQuestName);
            SetMainStoryProgressDimension(lastCompletedQuest);
        }
        
        void SetMainStoryProgressDimension(Quest quest) {
            if (quest == null) {
                SetMainStoryProgressDimension((Objective) null);
                return;
            }
            string lastCompletedObjectiveName = TrackedObjectives.LastOrDefault(t => quest.Objectives.Any(o => o.Name == t));
            Objective lastCompletedObjective = quest?.Objectives.LastOrDefault(o => o.Name == lastCompletedObjectiveName);
            SetMainStoryProgressDimension(lastCompletedObjective);
        }
        
        void SetMainStoryProgressDimension(Objective completedObjective) {
            string dimension = GetDimensionNameFromObjective(completedObjective);
            if (dimension.IsNullOrWhitespace()) {
                return;
            }
            //GameAnalytics.SetCustomDimension02(dimension);
        }

        string GetDimensionNameFromObjective(Objective completedObjective) {
            if (completedObjective == null) {
                return MainStoryProgress.ms01_Prologue.ToString();
            }
            
            string completedObjectiveName = completedObjective.Name;
            Quest quest = completedObjective.ParentModel;
            
            if (quest.Template.name == "IA_TheGreatEscape") { //IA_TheGreatEscape
                if (completedObjectiveName == "FndCaradoc" || completedObjectiveName == "Escape") {
                    return MainStoryProgress.ms01_Prologue.ToString();
                }
                if (completedObjectiveName == "Boat") {
                    return MainStoryProgress.ms02_HosBeforeEnter.ToString();
                }
            } 
            
            if (quest.Template.name == "MQ_ShadowOfTheHorns") { //MQ_ShadowOfTheHorns
                if (completedObjectiveName == "GoToHorns") {
                    return MainStoryProgress.ms03_HoSEntered.ToString();
                }
                if (completedObjectiveName == "CheckCemetery" || completedObjectiveName == "ReportCemetaryToFaerghas") {
                    return MainStoryProgress.ms04_HoSCemetary.ToString();
                }
                if (completedObjectiveName == "CheckBodies" || completedObjectiveName == "ReportStronholdToFaerghas") {
                    return MainStoryProgress.ms05_HoSStronghold.ToString();
                }
                if (completedObjectiveName == "GoToAllmother") {
                    return MainStoryProgress.ms06_HoSAllmother.ToString();
                }
                if (completedObjectiveName == "GoToSewal") {
                    return MainStoryProgress.ms07_HoSSewal.ToString();
                }
                if (completedObjectiveName == "FixGong") {
                    return MainStoryProgress.ms08_HoSGong.ToString();
                }
                if (completedObjectiveName == "TakeExcalibur") {
                    return MainStoryProgress.ms09_HosExcalibur.ToString();
                }
            }

            return null;
        }
        
        // --- Dimeension03
        void SetPlaythroughDimension() {
            return;
            //TODO Implement Playthrough Count
            // int playthroughNumber = 1;
            // string dimension = playthroughNumber switch {
            //     < 10 => playthroughNumber.ToString(),
            //     < 15 => "10-14",
            //     < 20 => "15-19",
            //     _ => "20+"
            // };
            // GameAnalytics.SetCustomDimension02(dimension);
        }

        enum MainStoryProgress : byte {
            ms01_Prologue,
            ms02_HosBeforeEnter,
            ms03_HoSEntered,
            ms04_HoSCemetary,
            ms05_HoSStronghold,
            ms06_HoSAllmother,
            ms07_HoSSewal,
            ms08_HoSGong,
            ms09_HosExcalibur,
            [UnityEngine.Scripting.Preserve] ms20_StoryFinished
        }
    }
}
#endif