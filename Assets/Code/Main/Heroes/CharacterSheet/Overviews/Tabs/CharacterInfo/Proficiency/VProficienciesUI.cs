using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Overviews.Tabs.CharacterInfo.Proficiency {
    [UsesPrefab("CharacterSheet/Overview/VProficienciesUI")]
    public class VProficienciesUI : View<ProficienciesUI> {
        [SerializeField] TextMeshProUGUI proficienciesTitleLabel;
        [SerializeField, LocStringCategory(Category.UI)] LocString proficienciesTitle;

        [field: SerializeField] public Transform CategoriesParent { get; private set; }
        public override Transform DetermineHost() => Target.ParentModel.View<VCharacterInfoUI>().ProficienciesParent;

        protected override void OnInitialize() {
            proficienciesTitleLabel.SetText(proficienciesTitle);
            FocusFirstItem().Forget();
        }
        
        async UniTaskVoid FocusFirstItem() {
            if (await AsyncUtil.DelayFrame(Target)) {
                var firstEntry = Target.Elements<ProficiencyCategoryUI>().FirstOrDefault();
                if (firstEntry) {
                    World.Only<Focus>().Select(firstEntry.View<VProficiencyCategoryUI>().GetComponentInChildren<ARButton>());
                }
            }
        }
    }
}