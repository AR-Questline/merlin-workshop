using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Journal {
    [UsesPrefab("CharacterSheet/Journal/" + nameof(VJournalUI))]
    public class VJournalUI : VTabParent<JournalUI>, IAutoFocusBase  {
        [SerializeField] TMP_Text entriesCountText;
        [SerializeField] TMP_Text entriesLabel;

        public void SetEntriesCount(int known, int all, bool showAll) {
            entriesCountText.text = showAll ? $"{known}/{all}" : $"{known}";
            entriesLabel.text = LocTerms.JournalEntries.Translate();
        }
        
        public void SetCountActive(bool active) {
            entriesCountText.gameObject.SetActive(active);
            entriesLabel.gameObject.SetActive(active);
        }
    }
}