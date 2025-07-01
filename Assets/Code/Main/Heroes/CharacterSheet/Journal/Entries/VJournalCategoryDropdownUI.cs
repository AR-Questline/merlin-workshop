using Awaken.TG.Main.Heroes.CharacterSheet.Overviews;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Journal.Entries {
    [UsesPrefab("CharacterSheet/Journal/" + nameof(VJournalCategoryDropdownUI))]
    public class VJournalCategoryDropdownUI : View<JournalCategoryDropdownUI>, IVEntryParentUI {
        [SerializeField] ButtonConfig buttonConfig;
        [SerializeField] TextMeshProUGUI categoryNameLabel;
        [SerializeField] GameObject arrowUp, arrowDown;

        [field: SerializeField] public Transform EntriesParent { get; private set; }
        
        public override Transform DetermineHost() => Target.ParentModel.View<IVEntryParentUI>().EntriesParent;

        protected override void OnInitialize() {
            SetArrow();
            categoryNameLabel.SetText(Target.CategoryName);
            buttonConfig.InitializeButton(() => {
                Target.ToggleCategory();
            });
        }
        
        public void SetArrow() {
            arrowUp.SetActive(!Target.IsFolded);
            arrowDown.SetActive(Target.IsFolded);
        }
        
        public void FocusCategory() {
            World.Only<Focus>().Select(buttonConfig.button);
        }
    }
}