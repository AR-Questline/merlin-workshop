using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Crafting.AlchemyCrafting;
using Awaken.TG.Main.Crafting.HandCrafting;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.Utility;

namespace Awaken.TG.Main.Crafting.Cooking {
    public partial class CraftingTabs : Tabs<CraftingTabsUI, VCraftingTabs, CraftingTabTypes, ICraftingTabContents> {
        public sealed override bool IsNotSaved => true;

        readonly CraftingTabTypes[] _allowedTypes;
        protected override KeyBindings Previous => KeyBindings.UI.Generic.Previous;
        protected override KeyBindings Next => KeyBindings.UI.Generic.Next;
        
        public CraftingTabs(IEnumerable<CraftingTabTypes> tabs) {
            _allowedTypes = tabs.ToArray();
        }

        protected override VCTabButton[] GetButtons(VCraftingTabs view) => 
            base.GetButtons(view).Where(b => _allowedTypes.Any(t => t == b.Type)).ToArray();
    }

    public interface ICraftingTabContents : CraftingTabs.ITab { }

    public class CraftingTabTypes : CraftingTabs.DelegatedTabTypeEnum {
        public static readonly CraftingTabTypes
            ExperimentalCooking = new(nameof(ExperimentalCooking), LocTerms.ExperimentalCookingTab,
                target => new ExperimentalCooking(Hero.Current, target.TemplateFromType() as CookingTemplate),
                t => t.TabShouldBeActive(ExperimentalCooking), LocTerms.ExperimentalCookingTabDescription),
            RecipeCooking = new(nameof(RecipeCooking), LocTerms.RecipeCookingTab,
                target => new RecipeCooking(Hero.Current, target.TemplateFromType() as CookingTemplate),
                t => t.TabShouldBeActive(RecipeCooking), LocTerms.RecipeCookingTabDescription),
            RecipeAlchemy = new(nameof(RecipeAlchemy), LocTerms.RecipeAlchemyTab,
                target => new Alchemy(Hero.Current, target.TemplateFromType() as AlchemyTemplate),
                t => t.TabShouldBeActive(RecipeAlchemy), LocTerms.RecipeAlchemyTabDescription),
            RecipeHandcrafting = new(nameof(RecipeHandcrafting), LocTerms.RecipeHandcraftingTab,
                target => new Handcrafting(Hero.Current, target.TemplateFromType() as HandcraftingTemplate),
                t => t.TabShouldBeActive(RecipeHandcrafting), LocTerms.RecipeHandcraftingTabDescription);

        public static readonly CraftingTabTypes[]
            Cooking = { ExperimentalCooking, RecipeCooking },
            Alchemy = { RecipeAlchemy },
            Handcrafting = { RecipeHandcrafting };
        
        CraftingTabTypes(string enumName, string titleID, SpawnDelegate spawn = null, VisibleDelegate visible = null, string descriptionID = "") : base(enumName, titleID, descriptionID) {
            _spawn = spawn;
            _visible = visible ?? Always;
        }
    }
}
