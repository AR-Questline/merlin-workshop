using System;
using System.Collections.Generic;
using Awaken.TG.Main.Crafting.HandCrafting;
using Awaken.TG.Main.Crafting.HandCrafting.RecipeView;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.EmptyContent;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Crafting.Cooking {
    public partial class RecipeCooking : RecipeCrafting<CookingTemplate> {
        public sealed override bool IsNotSaved => true;

        public override Type TabView => typeof(VRecipeCooking);
        public override IEnumerable<RecipeTabType> AllowedTabTypes() => RecipeTabType.CookingTabs;

        public RecipeCooking(Hero hero, CookingTemplate template) : base(hero, template) { }
        
        public override void OverrideLabels(IEmptyInfo infoView) {
            infoView.EmptyInfoView.SetupLabels(LocTerms.EmptyCraftingNoIngredients.Translate(), LocTerms.EmptyCookingGatherMaterials.Translate(), LocTerms.EmptyCookingGatherMaterialsDesc.Translate());
        }
    }
}