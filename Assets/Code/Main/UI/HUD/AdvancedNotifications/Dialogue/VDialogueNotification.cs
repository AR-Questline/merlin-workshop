using Awaken.TG.MVC.Attributes;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.Dialogue {
    [UsesPrefab("HUD/AdvancedNotifications/VDialogueNotification")]
    public class VDialogueNotification : VAdvancedNotification<DialogueNotification> {
        [SerializeField] TextMeshProUGUI textThatDictatesSize;
        [SerializeField] TextMeshProUGUI text;
        DialogueData DialogueData => Target.dialogueData;

        protected override void OnInitialize() {
            UpdateDialogueToDisplay(DialogueData.dialogueToDisplay);
        }

        public void UpdateDialogueToDisplay(string dialogueToDisplay) {
            textThatDictatesSize.text = dialogueToDisplay;
            text.text = dialogueToDisplay;
        }
    }
}