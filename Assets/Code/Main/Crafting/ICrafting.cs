using System;
using System.Collections.Generic;
using Awaken.TG.Main.Crafting.Cooking;
using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.Crafting.Slots;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Tooltips;
using Awaken.TG.Main.UI.EmptyContent;
using FMODUnity;

namespace Awaken.TG.Main.Crafting {
    public interface ICrafting : ICraftingTabContents {
        IRecipe CurrentRecipe { get; }
        bool ButtonInteractability { get; }
        IEnumerable<Item> FilteredHeroItems { get; }
        List<SimilarItemsData> SimilarItemsData { get; }
        CraftingItemTooltipUI PossibleResultTooltipUI { get; set; }
        IEnumerable<CraftingItem> WorkbenchCraftingItems { get; }
        EventReference CraftCompletedSound { get; }
        
        void Create(int quantityMultiplier = 1);
        void CreateMany(Action callback, int quantity = 1);
        int CraftableItemsCount();
        void RefreshTooltipDescriptor(int level);
        void ShowEmptyInfo(bool active);
        void OverrideLabels(IEmptyInfo infoView);
    }
}