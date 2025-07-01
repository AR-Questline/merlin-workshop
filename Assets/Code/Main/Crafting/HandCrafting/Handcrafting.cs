using System;
using System.Collections.Generic;
using Awaken.TG.Main.Crafting.HandCrafting.RecipeView;
using Awaken.TG.Main.Heroes;

namespace Awaken.TG.Main.Crafting.HandCrafting {
    public partial class Handcrafting : RecipeCrafting<HandcraftingTemplate> {
        public const string RequiredKind = "craft";

        public sealed override bool IsNotSaved => true;
        
        public override Type TabView => typeof(VRecipeCrafting);
        public override IEnumerable<RecipeTabType> AllowedTabTypes() => RecipeTabType.HandCraftingTabs;
        
        public Handcrafting(Hero hero, HandcraftingTemplate template) : base(hero, template) { }
    }
}