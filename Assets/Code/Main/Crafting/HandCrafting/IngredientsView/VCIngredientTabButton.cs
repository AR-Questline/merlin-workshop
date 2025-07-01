using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Utility;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.HandCrafting.IngredientsView {
    public class VCIngredientTabButton : IngredientTabs.VCSelectableTabButton {
        [RichEnumExtends(typeof(IngredientTabType))]
        [SerializeField] RichEnumReference type;

        public override IngredientTabType Type => type.EnumAs<IngredientTabType>();
        
        protected override void Refresh(bool selected) {
            base.Refresh(selected);
            if (selected) {
                var x = Target.ParentModel.ParentModel;
                Target.ParentModel.View<VIngredientGridUI>().SetHeaderTabName($"{LocTerms.AvailableIngredients.Translate().ColoredText(ARColor.MainGrey).FontLight()} {Type.Title.ColoredText(ARColor.MainWhite).FontSemiBold()}");
            }
        }
    }
}