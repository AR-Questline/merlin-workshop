using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.Utility;

namespace Awaken.TG.Main.Crafting.HandCrafting.RecipeView {
    public partial class RecipeTabs : Tabs<RecipeGridUI, VRecipeTabs, RecipeTabType, RecipeTabContents> {
        readonly RecipeTabType[] _allowedTypes;
        protected override KeyBindings Previous => KeyBindings.UI.Generic.PreviousAlt;
        protected override KeyBindings Next => KeyBindings.UI.Generic.NextAlt;
        
        public RecipeTabs(IEnumerable<RecipeTabType> tabs) {
            _allowedTypes = tabs.ToArray();
        }
        
        protected override VCTabButton[] GetButtons(VRecipeTabs view) => 
            base.GetButtons(view).Where(b => _allowedTypes.Any(t => t == b.Type)).ToArray();
    }

    public class RecipeTabType : RecipeTabs.TabTypeEnum {
        readonly Func<ItemTemplate, bool> _filter;
        
        public bool Contains(IRecipe slot) => _filter(slot.Outcome);
        
        public override bool IsVisible(RecipeGridUI target) => target.RecipeCrafting.AllowedTabTypes().Contains(this) && target.AllRecipes.Any(Contains);

        public override RecipeTabContents Spawn(RecipeGridUI target) => new(this, target);

        RecipeTabType(string enumName, Func<ItemTemplate, bool> filter, string titleID = "") : base(enumName, titleID) {
            _filter = filter;
        }

        [UnityEngine.Scripting.Preserve]
        public static readonly RecipeTabType
            All = new(nameof(All), _ => true, LocTerms.ItemsTabAll),
            Component = new(nameof(Component), i => i.IsComponent),
            Consumable = new(nameof(Consumable), i => i.IsConsumable, LocTerms.ItemsTabConsumable),
            Quest = new(nameof(Quest), i => i.IsQuestItem(), LocTerms.ItemsTabQuestItems),
            Other = new(nameof(Other), i => i.ConsumablePotionOther, LocTerms.ItemsTabOther),
            
            Weapon = new(nameof(Weapon), i => i.IsWeapon && !i.IsMagic, LocTerms.ItemsTabWeapons),
            OneHanded = new(nameof(OneHanded), i => i.IsOneHanded && i.IsMelee),
            TwoHanded = new(nameof(TwoHanded), i => i.IsTwoHanded && i.IsMelee),
            Ranged = new(nameof(Ranged), i => i.IsRanged, LocTerms.ItemsTabRanged),
            Arrows = new(nameof(Arrows), i => i.IsArrow, LocTerms.ItemsTabArrows),
            Shields = new(nameof(Shields), i => i.IsShield),
            BuffApplier = new(nameof(BuffApplier), i => i.IsBuffApplier, LocTerms.ItemTypeBuffApplier),
            HPConsumable = new(nameof(HPConsumable), i => i.ConsumableModifiesHealth, LocTerms.Health),
            MPConsumable = new(nameof(MPConsumable), i => i.ConsumableModifiesMana, LocTerms.Mana),
            SPConsumable = new(nameof(SPConsumable), i => i.ConsumableStamina, LocTerms.Stamina),
            StatConsumable = new(nameof(StatConsumable), i => i.ConsumableModifiesStat, LocTerms.CharacterStatsSummary),
            
            Armor = new(nameof(Armor), i => i.IsArmor, LocTerms.ItemsTabArmor),
            Jewelry = new (nameof(Jewelry), i => i.IsJewelry, LocTerms.ItemsTabJewelry);

        public static readonly RecipeTabType[]
            CookingTabs = {All, Quest},
            AlchemyTabs = {All, HPConsumable, MPConsumable, SPConsumable, BuffApplier, Quest, Other},
            HandCraftingTabs = {All, Weapon, Arrows, Armor, Jewelry, Quest, Other};
    }
}