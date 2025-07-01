using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.CharacterInfo.Proficiency {
    [UsesPrefab("CharacterSheet/Overview/VProficiencyCategoryUI")]
    public class VProficiencyCategoryUI : View<ProficiencyCategoryUI> {
        [SerializeField] TextMeshProUGUI categoryNameText;
        [SerializeField] Image categoryIcon;
        [SerializeField] ButtonConfig categoryButtonConfig;
        [SerializeField] GameObject arrowUp, arrowDown;
        
        [field: SerializeField] public Transform ContentHost { get; private set; }

        public override Transform DetermineHost() => Target.ParentModel.View<VProficienciesUI>().CategoriesParent;

        protected override void OnInitialize() {
            SetArrow();
            categoryNameText.SetText(Target.categoryName);
            categoryButtonConfig.InitializeButton(() => {
                Target.ToggleCategory();
                SetArrow();
                categoryButtonConfig.SetSelection(!Target.IsFolded);
            });

            if (Target.categoryIcon is { IsSet: true } icon) {
                icon.RegisterAndSetup(this, categoryIcon);
            }
        }

        protected override void OnMount() {
            Target.SpawnProficiencies();
        }

        void SetArrow() {
            arrowUp.SetActive(!Target.IsFolded);
            arrowDown.SetActive(Target.IsFolded);
        }
    }
}