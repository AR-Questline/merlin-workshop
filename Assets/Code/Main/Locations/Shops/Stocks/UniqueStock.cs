using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Stories;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Locations.Shops.Stocks {
    /// <summary>
    /// Shop stock which doesn't change on restock
    /// Used to store important items that can be bought by player only once (like quest item)
    /// </summary>
    public partial class UniqueStock : Stock {
        public override ushort TypeForSerialization => SavedModels.UniqueStock;

        [Saved] StructList<string> _unlockedFlags;
        [Saved] List<ItemSpawningDataRuntime> _compressedItems = new List<ItemSpawningDataRuntime>();
        Dictionary<string, IEventListener> _flagListeners;
        
        protected override List<ItemSpawningDataRuntime> CompressedItems => _compressedItems;
        protected LockedUniqueItems[] AllLockedItems => ParentModel.Template.lockedUniqueItems;
        
        protected override void OnInitialize() {
            foreach (var itemTemplateReference in ParentModel.Template.uniqueItems) {
                var data = itemTemplateReference.ToRuntimeData(this);
                if (data.ItemTemplate == null) continue;
                AddItemData(data);
            }
            HandleLockedUniqueItems();
        }

        protected override void OnRestore() {
            HandleLockedUniqueItems();
        }

        void HandleLockedUniqueItems() {
            foreach (var lockedItems in AllLockedItems) {
                CheckLockedUniqueItems(lockedItems);
            }
        }

        void CheckLockedUniqueItems(LockedUniqueItems lockedItems) {
            if (_unlockedFlags.IsCreated) {
                // Flag already unlocked and items are granted.
                foreach (var unlockedFlag in _unlockedFlags) {
                    if (unlockedFlag.Equals(lockedItems.flagToUnlock)) {
                        return;
                    }
                }
            }

            // Flag already unlocked and items are not granted.
            if (StoryFlags.Get(lockedItems.flagToUnlock)) {
                UnlockItems(lockedItems);
                return;
            }
            
            // Flag is not unlocked.
            _flagListeners ??= new Dictionary<string, IEventListener>();
            if (!_flagListeners.ContainsKey(lockedItems.flagToUnlock)) {
                var listener = World.EventSystem.ListenTo(EventSelector.AnySource, StoryFlags.Events.UniqueFlagChanged(lockedItems.flagToUnlock), this, flag => OnFlagChange(flag, lockedItems));
                _flagListeners[lockedItems.flagToUnlock] = listener;
            }
        }

        void OnFlagChange(string flag, LockedUniqueItems lockedItems) {
            if (!StoryFlags.Get(flag)) { 
                return; 
            }
            
            UnlockItems(lockedItems);
            
            if (_flagListeners.TryGetValue(flag, out var listener)) {
                World.EventSystem.RemoveListener(listener);
                _flagListeners.Remove(flag);
                if (_flagListeners.IsEmpty()) {
                    _flagListeners = null;
                }
            }
        }

        void UnlockItems(LockedUniqueItems lockedItems) {
            foreach (var itemTemplateReference in lockedItems.lockedUniqueItems) {
                var data = itemTemplateReference.ToRuntimeData(this);
                if (data.ItemTemplate == null) continue;
                AddItemData(data);
            }
            
            if (_unlockedFlags.IsCreated == false) {
                _unlockedFlags = new StructList<string>(1);
            }
            _unlockedFlags.Add(lockedItems.flagToUnlock);
        }
    }
}