using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Utility.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Stories.Quests.UI {
    public class QuestLogButton : MonoBehaviour {
        [SerializeField] ButtonConfig buttonConfig;
        [SerializeField] QuestListType questListType;
        [SerializeField] Color32 selectedColor;
        [SerializeField] Color32 defaultColor;
        [SerializeField] TextMeshProUGUI questTabText;
        [SerializeField, LocStringCategory(Category.UI)] LocString questTabName;
        [SerializeField] Image icon;

        public int TabIndex { get; set; }
        
        public ButtonConfig ButtonConfig => buttonConfig;
        public QuestListType QuestListType => questListType;
        public string QuestTabName => questTabName;

        protected void Awake() {
            questTabText.SetText(questTabName);
        }

        public void ChangeColor(bool selected) {
            icon.color = selected ? selectedColor : defaultColor;
        }
    }
}
