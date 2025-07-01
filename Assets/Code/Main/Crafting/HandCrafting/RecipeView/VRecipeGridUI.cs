using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.MVC.Attributes;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.HandCrafting.RecipeView {
    [UsesPrefab("Crafting/Handcrafting/VRecipeGridUI")]
    public class VRecipeGridUI : VTabParent<RecipeGridUI> {
        [SerializeField] TextMeshProUGUI headerNameText;
        
        public void SetHeaderTabName(string description) {
            if (headerNameText) {
                headerNameText.SetText(description);
            }
        }
    }
}