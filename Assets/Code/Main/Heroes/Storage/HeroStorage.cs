using Awaken.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments.Interfaces;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Relations;
using Awaken.TG.Utility.Attributes;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Heroes.Storage {
    public partial class HeroStorage : Element<Hero>, IInventory, IItemOwner {
        public override ushort TypeForSerialization => SavedModels.HeroStorage;

        [Saved] List<ItemSpawningDataRuntime> _stashedItems = new List<ItemSpawningDataRuntime>();
        int _storageItemsUsers = 0;
        
        RelatedList<Item> OwnedItems => RelatedList(IItemOwner.Relations.Owns);
        public IEnumerable<Item> Items => OwnedItems.Where(i => !i.HiddenOnUI);
        public IEnumerable<ItemSpawningDataRuntime> StashedItems => _stashedItems;
        public IEnumerable<Item> SellableInventory(Func<Item, bool> additionalCondition) => OwnedItems.Where(item => !item.HiddenOnUI && !item.Locked && !item.CannotBeDropped && (additionalCondition == null || additionalCondition(item)));

        public IInventory Inventory => this;
        public ICharacter Character => null;
        public IEquipTarget EquipTarget => null;
        public bool IsStashed => _storageItemsUsers <= 0;

        protected override void OnFullyInitialized() {
            DelayedInitialize().Forget();
        }
        
        async UniTaskVoid DelayedInitialize() {
            if (!await AsyncUtil.DelayFrame(this)) {
                return;
            } 
            StashAllItems();
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

        public void RemoveCompressedItemsOfTemplate(ItemTemplate template) {
            for (int i = _stashedItems.Count - 1; i >= 0; i--) {
                if (_stashedItems[i].ItemTemplate == template) {
                    _stashedItems.RemoveAt(i);
                }
            }
        }
        
        public bool CanBeTheft => false;

        public Model Open() {
            var previous = World.Any<HeroStorageUI>();
            if (previous) {
                previous.Close();
            }
            return World.Add(new HeroStorageUI(this));
        }

        public void Close() {
            ReleaseItems();
        }
        
        void PlayStashedItemAudio(Item item) => FMODManager.PlayOneShot(ItemAudioType.DropItem.RetrieveFrom(item));

        public void RequestItems() {
            if (_storageItemsUsers <= 0) {
                _storageItemsUsers = 1;
                CreateAllItems();
                return;
            }
            _storageItemsUsers++;
        }

        public void ReleaseItems() {
            if (_storageItemsUsers <= 1) {
                _storageItemsUsers = 0;
                StashAllItems();
                return;
            }
            _storageItemsUsers--;
        }
        
        void StashAllItems() {
            for (int i = OwnedItems.Count - 1; i >= 0; i--) {
                StashItem(OwnedItems[i]);
            }
            StatTweak.CleanupObsoleteStatTweaks();
        }
        
        void StashItem(Item item) {
            _stashedItems.Add(new ItemSpawningDataRuntime(item));
            item.Discard();
        }

        void CreateAllItems() {
            foreach (var spawningDataRuntime in _stashedItems) {
                CreateItem(spawningDataRuntime);
            }
            _stashedItems.Clear();
        }

        void CreateItem(ItemSpawningDataRuntime spawningDataRuntime) {
            if (spawningDataRuntime?.ItemTemplate == null) return;
            if (spawningDataRuntime.ItemTemplate.hiddenOnUI) return;
            var item = World.Add(new Item(spawningDataRuntime));
            bool addToHeroInstead = IsInvalidItemInStash(spawningDataRuntime.ItemTemplate);
            if (addToHeroInstead) {
                ParentModel.HeroItems.AddWithoutNotification(item);
            } else { 
                OwnedItems.Add(item);
            }
        }
        
        bool IsInvalidItemInStash(ItemTemplate template) {
            if (template is not { cannotBeDropped: false }) {
                return true;
            }
            return false;
        }
    }
}