using Awaken.TG.Main.Stories.Quests.Objectives;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Quests.UI {
    [UsesPrefab("Quest/VQuestObjectiveUI")]
    public class VQuestObjectiveUI : View<QuestLogUI> {
        public TextMeshProUGUI questObjectiveDesc;
        [SerializeField] GameObject checkmark;
        [SerializeField] Color activeColor = ARColor.LightGrey;
        [SerializeField] Color completedColor = ARColor.MainGreen;
        [SerializeField] Color failedColor = ARColor.MainRed;
        [SerializeField] Color inactiveColor = ARColor.DarkerGrey;

        public override Transform DetermineHost() => Target.View<VQuestDescriptionUI>().ActiveObjectivesParent;

        public void Refresh(Objective objective) {
            checkmark.SetActive(objective.State == ObjectiveState.Completed);

            string objectiveDesc = objective.GetQuestLogDescription();
            
            questObjectiveDesc.SetText(objectiveDesc);
            questObjectiveDesc.color = objective.State switch {
                ObjectiveState.Active => activeColor,
                ObjectiveState.Completed => completedColor,
                ObjectiveState.Failed => failedColor,
                ObjectiveState.Inactive => inactiveColor,
                _ => questObjectiveDesc.color
            };
        }
    }
}
