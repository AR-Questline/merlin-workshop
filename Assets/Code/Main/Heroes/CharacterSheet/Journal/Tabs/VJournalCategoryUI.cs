using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.Utility.GameObjects;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Journal.Tabs {
    [UsesPrefab("CharacterSheet/Journal/" + nameof(VJournalCategoryUI))]
    public class VJournalCategoryUI : View<IJournalCategoryUI>, IVJournalCategoryUI, IAutoFocusBase  {
        [field: SerializeField] public Transform EntryContent { [UnityEngine.Scripting.Preserve] get; private set; }
        [field: SerializeField] public Transform Preview { [UnityEngine.Scripting.Preserve] get; private set; }
        [field: SerializeField] public Transform Description { [UnityEngine.Scripting.Preserve] get; private set; }
        [field: SerializeField] public Transform EntriesParent { get; private set;}
        [SerializeField] GameObject noEntriesInfo;
        [SerializeField] Scrollbar scrollbar;

        protected override void OnInitialize() {
            ShowNoEntriesInfo(false);
            Target.ListenTo(IJournalCategoryUI.Events.EntrySelected, () => scrollbar.value = 1, Target);
        }
        
        public void ShowNoEntriesInfo(bool show) {
            noEntriesInfo.SetActiveOptimized(show);
        }
    }
}
