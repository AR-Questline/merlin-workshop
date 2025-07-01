using System.Collections.Generic;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Tooltips;
using Awaken.TG.Main.Locations.Shops;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Locations.Gems {
    public interface IGemBase: IModel {
        Item ClickedItem { get; }
        int ServiceCost { get; }
        int CobwebServiceCost { get; }
        void RefreshPrompt(bool state);
        List<SimilarItemsData> SimilarItemsData { get; }
        IEnumerable<Item> AllHeroItems { get; }
        IEnumerable<CountedItem> Ingredients { get; }
        bool CanAfford(CurrencyType currencyType);
        ItemTooltipUI ItemTooltipUI { get; }
        ItemTooltipUI IngredientTooltipUI { get; }
        ItemsUI ItemsUI { get; }
        
        public static class Events {
            public static readonly Event<IGemBase, bool> GemActionPerformed = new(nameof(GemActionPerformed));
            public static readonly Event<IGemBase, bool> AfterRefreshed = new(nameof(AfterRefreshed));
            [UnityEngine.Scripting.Preserve] public static readonly Event<IGemBase, bool> HoveredItemChanged = new(nameof(HoveredItemChanged));
            public static readonly Event<IGemBase, Item> ClickedItemChanged = new(nameof(ClickedItemChanged));
            public static readonly Event<IGemBase, bool> CostRefreshed = new(nameof(CostRefreshed));
        }
    }
}