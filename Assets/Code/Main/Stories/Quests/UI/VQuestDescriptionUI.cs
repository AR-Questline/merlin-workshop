using System;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Stories.Quests.Objectives;
using Awaken.TG.Main.UI.Helpers;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Animations;
using Awaken.Utility.GameObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Stories.Quests.UI {
    [UsesPrefab("Quest/" + nameof(VQuestDescriptionUI))]
    public class VQuestDescriptionUI : View<QuestLogUI> {
        [SerializeField] GameObject questObjectivesParent;
        [SerializeField] TextMeshProUGUI questTypeText;
        [SerializeField] TMP_Text activeObjectiveRegionText;
        [SerializeField] TMP_Text activeObjectiveSceneText;
        [SerializeField] GameObject activeRegionSpaceSeparator;
        [SerializeField] GameObject activeObjectiveScenePipeDecor;
        [SerializeField] TextMeshProUGUI questTitle;
        [SerializeField] TextMeshProUGUI questDescription;
        [SerializeField] Image questDescriptionIcon;
        [SerializeField] Transform activeObjectivesParent;
        [SerializeField] Transform completedObjectiveParent;
        [SerializeField] Transform failedObjectiveParent;
        
        Quest _previousQuest;
        SpriteReference _questDescriptionIconRef;
        
        public Transform ActiveObjectivesParent => activeObjectivesParent;

        protected override void OnInitialize() {
            ResetDescription();
            Target.ListenTo(QuestLogUI.Events.QuestSelected, Refresh, this);
        }
    
        void Refresh(QuestUI questUI) {
            var newQuest = questUI?.QuestData;
            if (newQuest == null) {
                _previousQuest = null;
                ResetDescription();
                return;
            }
            
            if (_previousQuest == newQuest) {
                return;
            }

            _previousQuest = newQuest;
            SetObjectivesVisibility(true);
            SetupQuestDescriptionTexts(quest: newQuest);
            SetupQuestDescriptionIcon(questUI);
            
            foreach (var view in Target.Views.OfType<VQuestObjectiveUI>().ToList()) {
                view.Discard();
            }

            // spawn quest objective from newest to oldest
            Objective[] questObjectives = SpawnQuestObjectives(newQuest: newQuest);
            activeObjectivesParent.gameObject.SetActive(questObjectives.Any(q => q.State == ObjectiveState.Active));
            completedObjectiveParent.gameObject.SetActive(questObjectives.Any(q => q.State == ObjectiveState.Completed));
            failedObjectiveParent.gameObject.SetActive(questObjectives.Any(q => q.State == ObjectiveState.Failed));
        }

        Objective[] SpawnQuestObjectives(Quest newQuest) {
            var questObjectives = newQuest.Objectives.GetManagedEnumerator().OrderByDescending(q => (int) q.State).ToArray();
            foreach (Objective objective in questObjectives) {
                switch (objective.State) {
                    case ObjectiveState.Active:
                        SpawnObjectiveView(objective, activeObjectivesParent);
                        break;
                    case ObjectiveState.Completed:
                        SpawnObjectiveView(objective, completedObjectiveParent);
                        break;
                    case ObjectiveState.Failed:
                        SpawnObjectiveView(objective, failedObjectiveParent);
                        break;
                    case ObjectiveState.Inactive:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return questObjectives;
        }

        void SetupQuestDescriptionTexts(Quest quest) {
            questTypeText.SetText(quest.QuestType.ToTranslatedString());
            questTitle.SetText(quest.DisplayName);
            questDescription.SetActiveAndText(string.IsNullOrEmpty(quest.Description) == false, quest.Description);
            TryToSetQuestSceneName(quest);
        }

        void TryToSetQuestSceneName(Quest quest) {
            var firstActiveObjective = quest.Objectives.FirstOrDefault(q => q.State == ObjectiveState.Active);
            bool anyActiveObjectiveWithValidScene = firstActiveObjective is {MainTargetScene: { IsSet: true }};
            string sceneName = anyActiveObjectiveWithValidScene ? LocTerms.GetSceneName(firstActiveObjective.MainTargetScene) : string.Empty;
            
            bool shouldShowRegion = firstActiveObjective?.MainTargetOpenWorldScene != null 
                                    && firstActiveObjective.MainTargetOpenWorldScene != firstActiveObjective.MainTargetScene;
            string regionName = shouldShowRegion ? LocTerms.GetSceneName(firstActiveObjective.MainTargetOpenWorldScene) : string.Empty;
            
            activeObjectiveScenePipeDecor.SetActiveOptimized(anyActiveObjectiveWithValidScene);
            activeRegionSpaceSeparator.SetActiveOptimized(shouldShowRegion);
            activeObjectiveRegionText.SetActiveAndText(shouldShowRegion, regionName);
            activeObjectiveSceneText.SetActiveAndText(anyActiveObjectiveWithValidScene, sceneName);
        }

        void SetupQuestDescriptionIcon(QuestUI questUI) {
            _questDescriptionIconRef?.Release();
            _questDescriptionIconRef = questUI.QuestData.Template.iconDescriptionReference.TrySetupSpriteReference(questDescriptionIcon);
        }

        void SpawnObjectiveView(Objective objective, Transform objectiveParent) {
            VQuestObjectiveUI vQuestObjective = World.SpawnView<VQuestObjectiveUI>(Target, forcedParent: objectiveParent);
            vQuestObjective.Refresh(objective);

        }

        void SetObjectivesVisibility(bool show) {
            questObjectivesParent.SetActive(show);
        }
        
        void ResetDescription() {
            SetObjectivesVisibility(false);
            questTitle.SetText(string.Empty);
            questDescription.SetActiveAndText(false, string.Empty);
        }

        protected override IBackgroundTask OnDiscard() {
            _questDescriptionIconRef?.Release();
            return base.OnDiscard();
        }
    }
}
