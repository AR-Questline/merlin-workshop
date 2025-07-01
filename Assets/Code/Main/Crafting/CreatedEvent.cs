using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.Heroes.Items;

namespace Awaken.TG.Main.Crafting {
    public struct CreatedEvent {
        public CreatedEvent(CraftingTemplate craftingTemplate, Item item, IRecipe recipe, ItemTemplate[] usedIngredients) {
            CraftingTemplate = craftingTemplate;
            Item = item;
            Recipe = recipe;
            UsedIngredients = usedIngredients;
        }

        public CraftingTemplate CraftingTemplate { get; }
        public Item Item { get; }
        public IRecipe Recipe { get; }
        public ItemTemplate[] UsedIngredients { get; }
    }
}