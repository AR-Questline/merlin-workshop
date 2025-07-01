using System;
using System.Linq;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.Utility;

namespace Awaken.TG.Main.Crafting.HandCrafting.IngredientsView {
    public partial class IngredientTabs : Tabs<IngredientsGridUI, VIngredientTabs, IngredientTabType, IngredientTabContents> {
        protected override KeyBindings Previous => KeyBindings.UI.Generic.PreviousAlt;
        protected override KeyBindings Next => KeyBindings.UI.Generic.NextAlt;
    }
    
    public class IngredientTabType : IngredientTabs.TabTypeEnum {
        readonly Func<Item, bool> _filter;

        public override IngredientTabContents Spawn(IngredientsGridUI target) => new(this);

        public override bool IsVisible(IngredientsGridUI target) => target.Tabs.Contains(this) && target.Items.Any(Contains);

        public bool Contains(Item item) => _filter(item);
        
        public IngredientTabType[] SubTabs { [UnityEngine.Scripting.Preserve] get; }

        public IngredientTabType(string enumName, Func<Item, bool> filter, string title, IngredientTabType[] subTabs = null) : base(enumName, title) {
            _filter = filter;
            SubTabs = subTabs;
        }
        
        [UnityEngine.Scripting.Preserve]
        public static readonly IngredientTabType
            None = new (nameof(None), _ => true, LocTerms.ItemsTabAll),
            All = new(nameof(All), _ => true, LocTerms.ItemsTabAll),
            QuestItems = new(nameof(QuestItems), i => i.IsQuestItem, LocTerms.ItemsTabQuestItems),
            Others = new(nameof(Others), i => i.IsOther(), LocTerms.ItemsTabOther);
        
        public static readonly IngredientTabType[] ExperimentalCooking = { All, QuestItems, Others, };
    }
}