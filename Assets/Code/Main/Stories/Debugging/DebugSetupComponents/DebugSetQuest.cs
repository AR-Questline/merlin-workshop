using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.Stories.Quests.Objectives;
using Awaken.TG.Main.Stories.Quests.Templates;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Stories.Debugging.DebugSetupComponents {
    public class DebugSetQuest : MonoBehaviour, IDebugComponent {
        public TextMeshProUGUI textComponent;
        public Toggle toggle;

        CEditorQuestState _cEditorObj;

        QuestTemplate Quest => _cEditorObj.questRef.Get<QuestTemplate>();

        public void Init(Story story, CEditorQuestState element) {
            _cEditorObj = element;
            textComponent.text = $"{Quest.displayName} = {_cEditorObj.requiredState}";
            toggle.isOn = QuestUtils.StateOfQuestWithId(story.Memory, _cEditorObj.questRef) == _cEditorObj.requiredState;
        }

        public void Apply(Story story) {
            QuestUtils.SetQuestState(_cEditorObj.questRef, _cEditorObj.requiredState);
        }
    }
}