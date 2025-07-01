using Awaken.TG.Main.Crafting.HandCrafting;
using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.Slots {
    public abstract partial class CraftingSlot : Element<Crafting> {
        public sealed override bool IsNotSaved => true;

        public CraftingItem CraftingItem => TryGetElement<CraftingItem>();
        public Item Item => CraftingItem?.Item;
        public ItemTemplate WantedItemTemplate => CraftingItem?.WantedItemTemplate();

        public virtual Transform GhostItemSlot { get; [UnityEngine.Scripting.Preserve] protected set; }
        public virtual Transform ItemSlot { get; [UnityEngine.Scripting.Preserve] protected set; }
        
        public virtual Transform DeterminedHost => null;
        public virtual bool DiscardWhenEmpty => false;
        public bool IsRecipeCrafting => ParentModel is IRecipeCrafting;
        
        public virtual void UpdateSlot(Ingredient ingredient, int itemQuantity, bool isCraftable) { }

        public void Refresh() {
            TriggerChange();
        }

        public abstract void Submit();
    }
}