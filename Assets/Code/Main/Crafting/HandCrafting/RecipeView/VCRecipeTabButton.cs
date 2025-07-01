using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Utility;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.HandCrafting.RecipeView {
    public class VCRecipeTabButton : RecipeTabs.VCSelectableTabButton {
        [Space(10f)]
        [RichEnumExtends(typeof(RecipeTabType))]
        [SerializeField] RichEnumReference type;
        public override RecipeTabType Type => type.EnumAs<RecipeTabType>();
        
        protected override void Refresh(bool selected) {
            base.Refresh(selected);
            if (selected) {
                Target.ParentModel.View<VRecipeGridUI>().SetHeaderTabName($"{LocTerms.AvailableRecipes.Translate().ColoredText(ARColor.MainGrey).FontLight()} {Type.Title.ColoredText(ARColor.MainWhite).FontSemiBold()}");
            }
        }
    }
}