using System;
using System.Collections.Generic;
using Awaken.TG.Main.Crafting.HandCrafting;
using Awaken.TG.Main.Crafting.HandCrafting.RecipeView;
using Awaken.TG.Main.Heroes;

namespace Awaken.TG.Main.Crafting.AlchemyCrafting {
    public partial class Alchemy : RecipeCrafting<AlchemyTemplate> {
        public const string RequiredKind = "alchemy";

        public sealed override bool IsNotSaved => true;
        
        public override Type TabView => typeof(VRecipeAlchemy);
        public override IEnumerable<RecipeTabType> AllowedTabTypes() => RecipeTabType.AlchemyTabs;

        public Alchemy(Hero hero, AlchemyTemplate template) : base(hero, template) { }
    }
}