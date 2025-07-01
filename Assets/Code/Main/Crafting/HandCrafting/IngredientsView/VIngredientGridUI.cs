using Awaken.TG.Main.Crafting.Cooking;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.MVC.Attributes;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.HandCrafting.IngredientsView {
    [UsesPrefab("Crafting/Handcrafting/VIngredientGridUI")]
    public class VIngredientGridUI : VTabParent<IngredientsGridUI> {
        [SerializeField] TextMeshProUGUI headerNameText;

        public override Transform DetermineHost() => Target.ParentModel.View<VExperimentalCooking>().IngredientsGridUI;

        public void SetHeaderTabName(string description) {
            if (headerNameText) {
                headerNameText.SetText(description);
            }
        }
    }
}