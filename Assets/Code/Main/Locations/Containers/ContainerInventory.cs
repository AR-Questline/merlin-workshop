using Awaken.Utility;
using System;
using System.Collections.Generic;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments.Interfaces;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Relations;

namespace Awaken.TG.Main.Locations.Containers {
    public partial class ContainerInventory : Element<AbstractLocationAction>, IInventory, IItemOwner {
        public override ushort TypeForSerialization => SavedModels.ContainerInventory;

        public IEnumerable<Item> Items => OwnedItems;
        RelatedList<Item> OwnedItems => RelatedList(IItemOwner.Relations.Owns);

        public IInventory Inventory => this;
        public ICharacter Character => null;
        public IEquipTarget EquipTarget => null;

        protected override void OnFullyInitialized() {
            this.ListenTo(IItemOwner.Relations.Owns.Events.Changed, TriggerChange, this);
        }

        public Item Add(Item item, bool allowStacking = true) {
            if (!item.IsInitialized) {
                World.Add(item);
            }
            
            // check if item is already owned by this
            if (OwnedItems.Contains(item)) {
                return item;
            }
            
            if (item.Inventory != null) {
                throw new Exception("Item still has a reference to its old inventory. This is not allowed!");
            }
            
            if (allowStacking && Items.TryStackItem(item, out var stackedTo)) {
                // successfully stacked item
                return stackedTo;
            }
            OwnedItems.Add(item);

            return item;
        }

        public void Remove(Item item, bool discard = true) {
            // check if item is owned by this
            if (!OwnedItems.Contains(item)) return;
            
            OwnedItems.Remove(item);
            World.EventSystem.RemoveAllListenersBetween(item, this);

            if (discard) {
                item.Discard();
            } else {
                TriggerChange();
            }
        }

        public bool CanBeTheft => true;
    }
}