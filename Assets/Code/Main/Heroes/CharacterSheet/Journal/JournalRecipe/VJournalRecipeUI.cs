using Awaken.TG.Main.Heroes.CharacterSheet.Journal.Tabs;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.Utility.GameObjects;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Journal.JournalRecipe {
    [UsesPrefab("CharacterSheet/Journal/" + nameof(VJournalRecipeUI))]
    public class VJournalRecipeUI : VTabParent<JournalRecipeUI>, IAutoFocusBase, IVJournalCategoryUI  {
        [field: SerializeField] public Transform EntryDescriptionContent { get; private set; }
        [field: SerializeField] public Transform EntriesScrollListContent { get; private set; }
        [field: SerializeField] public Transform Preview { [UnityEngine.Scripting.Preserve] get; private set; }
        [field: SerializeField] public Transform Description { [UnityEngine.Scripting.Preserve] get; private set; }
        [field: SerializeField] public Transform EntriesParent { get; private set;}
        [field: SerializeField] public Transform RecipesSubTabParent { get; private set;}
        [SerializeField] GameObject noEntriesInfo;

        protected override void OnInitialize() {
            HideTabs();
        }
        
        public void ShowNoEntriesInfo(bool show) {
            noEntriesInfo.SetActiveOptimized(show);
        }

        public override void HideTabs() {
            Target.ParentModel.ShowTabs();
            TabButtonsHost.gameObject.SetActive(false);
            RecipesSubTabParent.gameObject.SetActive(true);
            EntryDescriptionContent.gameObject.SetActive(false);
            EntriesParent.gameObject.SetActive(false);
            EntriesScrollListContent.gameObject.SetActive(false);
            ShowNoEntriesInfo(false);
        }
        
        public override void ShowTabs() {
            Target.ParentModel.HideTabs();
            TabButtonsHost.gameObject.SetActive(true);
            RecipesSubTabParent.gameObject.SetActive(false);
            EntryDescriptionContent.gameObject.SetActive(true);
            EntriesParent.gameObject.SetActive(true);
            EntriesScrollListContent.gameObject.SetActive(true);
        }
    }
}