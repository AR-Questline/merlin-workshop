using Awaken.TG.Main.Heroes.CharacterSheet.Journal.Tabs;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.Utility.GameObjects;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Journal.JournalFishing {
    [UsesPrefab("CharacterSheet/Journal/" + nameof(VJournalFishingUI))]
    public class VJournalFishingUI : View<JournalFish>, IVJournalCategoryUI, IAutoFocusBase {
        [SerializeField] Transform entriesParent;
        [SerializeField] GameObject noEntriesInfo;

        public Transform EntriesParent => entriesParent;

        protected override void OnInitialize() {
            ShowNoEntriesInfo(false);
        }
        
        public void ShowNoEntriesInfo(bool show) {
            noEntriesInfo.SetActiveOptimized(show);
        }
    }
}