using Awaken.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments.Interfaces;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Relations;

namespace Awaken.TG.Main.Heroes.Storage {
    public partial class HeroStorage : Element<Hero>, IInventory, IItemOwner {
        public override ushort TypeForSerialization => SavedModels.HeroStorage;

        RelatedList<Item> OwnedItems => RelatedList(IItemOwner.Relations.Owns);
        public IEnumerable<Item> Items => OwnedItems;
        public IEnumerable<Item> SellableInventory(Func<Item, bool> additionalCondition) => OwnedItems.Where(item => !item.HiddenOnUI && !item.Locked && !item.CannotBeDropped && (additionalCondition == null || additionalCondition(item)));

        public IInventory Inventory => this;
        public ICharacter Character => null;
        public IEquipTarget EquipTarget => null;
        
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
            PlayStashedItemAudio(item);

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
        public bool CanBeTheft => false;

        public void Open() {
            var previous = World.Any<HeroStorageUI>();
            if (previous) {
                previous.Close();
            }
            World.Add(new HeroStorageUI(this));
        }
        
        void PlayStashedItemAudio(Item item) => FMODManager.PlayOneShot(ItemAudioType.DropItem.RetrieveFrom(item));
    }
}