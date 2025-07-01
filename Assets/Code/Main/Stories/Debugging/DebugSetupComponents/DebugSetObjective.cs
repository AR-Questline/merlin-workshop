using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.Stories.Quests.Objectives;
using Awaken.TG.Main.Stories.Quests.Templates;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Stories.Debugging.DebugSetupComponents {
    public class DebugSetObjective : MonoBehaviour, IDebugComponent {
        public TextMeshProUGUI textComponent;
        public Toggle toggle;

        CEditorQuestObjective _cEditorObj;

        QuestTemplate Quest => _cEditorObj.questRef.Get<QuestTemplate>();

        public void Init(Story story, CEditorQuestObjective element) {
            _cEditorObj = element;
            textComponent.text = $"{Quest.displayName} - {_cEditorObj.objectiveGuid} = {_cEditorObj.requiredState}";
            toggle.isOn = QuestUtils.StateOfObjective(story.Memory, _cEditorObj.questRef, _cEditorObj.objectiveGuid) == _cEditorObj.requiredState;
        }

        public void Apply(Story story) {
            QuestUtils.ChangeObjectiveState(_cEditorObj.questRef, _cEditorObj.objectiveGuid, _cEditorObj.requiredState);
        }
    }
}