using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Bag;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Loadouts;
using Awaken.TG.Main.Heroes.Items.Loadouts;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.Item;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Relations;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using FMODUnity;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Items {
    public partial class HeroItems : Element<Hero>, ICharacterInventory {
        public override ushort TypeForSerialization => SavedModels.HeroItems;
        public static readonly AudioConfig AudioConfig = CommonReferences.Get.AudioConfig;

        // === Fields & Properties
        [Saved] public int CurrentLoadoutIndex { get; private set; } = -1;
        [Saved] Item MainHandFist { get; set; }
        [Saved] Item OffHandFist { get; set; }
        [Saved(0)] int _selectedQuickSlot;
        [Saved] ItemInSlots _itemInSlots;
        bool _loadoutLocked;
        bool _equippingLocked;
        
        int ICharacterInventory.EquippingSemaphore { get; set; }
        public IItemOwner Owner => ParentModel;
        public float CurrentWeight => Items.Sum(i => i.Weight * i.Quantity);
        public bool CanBeTheft => false;
        public EquipmentSlotType SelectedQuickSlotType => EquipmentSlotType.QuickSlots[_selectedQuickSlot];

        /// <summary>
        /// Items that lie in inventory (not equipped)
        /// </summary>
        public IEnumerable<Item> Inventory => ContainedItems;
        public IEnumerable<Item> SellableInventory(Func<Item, bool> additionalCondition) => Inventory.Where(item => !item.HiddenOnUI && !item.Locked && !item.CannotBeDropped && !Loadouts.Any(l => l.Contains(item)) && (additionalCondition == null || additionalCondition(item)));
        public IEnumerable<Item> StashableInventory => Items.Where(item => !item.HiddenOnUI && !item.Locked && !item.CannotBeDropped && !item.IsQuestItem);
        RelatedList<Item> ContainedItems => RelatedList(ICharacterInventory.Relations.Contains);

        /// <summary>
        /// All items owned by this hero.
        /// </summary>
        public IEnumerable<Item> Items => OwnedItems;
        RelatedList<Item> OwnedItems => Owner.RelatedList(IItemOwner.Relations.Owns);

        public ModelsSet<HeroLoadout> Loadouts => Elements<HeroLoadout>();
        public ILoadout CurrentLoadout => LoadoutAt(CurrentLoadoutIndex);
        public bool AllowEquipping => !_equippingLocked;
        public ref readonly ItemInSlots ItemInSlots => ref _itemInSlots;

        // === Events

        public new class Events {
            public static readonly Event<Hero, Item> PickedUpEquippable = new(nameof(PickedUpEquippable));
            public static readonly Event<Hero, EquipmentSlotType> QuickSlotSelected = new(nameof(QuickSlotSelected));
            public static readonly Event<Hero, Hero> QuickSlotUsed = new(nameof(QuickSlotUsed));
            public static readonly Event<Hero, Hero> QuickSlotItemUsedWithDelay = new(nameof(QuickSlotItemUsedWithDelay));
        }

        // === Constructors

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public HeroItems() {}

        // === Initialization

        protected override void OnInitialize() {
            _itemInSlots.Init(this);

            for (int i = 0; i < HeroLoadout.Count; i++) {
                AddElement(new HeroLoadout(i));
            }

            ActivateLoadout(0, false);
            this.ListenTo(Model.Events.AfterFullyInitialized, AfterInit, this);
        }

        protected override void OnRestore() {
            _itemInSlots.Init(this);

            this.ListenTo(Model.Events.AfterFullyInitialized, AfterInit, this);
        }

        void AfterInit() {
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<Item>(), this, model => RemoveFromInventory((Item)model));
            this.ListenTo(ICharacterInventory.Events.AfterEquipmentChanged, () => {
                ParentModel.Skills.UpdateContext();
            }, this);

            this.ListenTo(ICharacterInventory.Relations.Contains.Events.Changed, TriggerChange, this);
            
            Hero.Current.Trigger(Events.QuickSlotSelected, SelectedQuickSlotType);
            
            // Arrows refill
            this.ListenTo(ICharacterInventory.Events.PickedUpItem, EquipPickedArrows, this);
            this.ListenTo(ICharacterInventory.Events.SlotUnequipped(EquipmentSlotType.Quiver), EquipArrows, this);
            
            // Food refill
            this.ListenTo(ICharacterInventory.Events.PickedUpItem, EquipPickedFood, this);
            this.ListenTo(ICharacterInventory.Events.SlotUnequipped(EquipmentSlotType.FoodQuickSlot), EquipFood, this);
            
            // Quick slots logic
            this.ListenTo(ICharacterInventory.Events.SlotEquipped(EquipmentSlotType.QuickSlot2), item => EquipQuickSlot(EquipmentSlotType.QuickSlot2, item), this);
            this.ListenTo(ICharacterInventory.Events.SlotEquipped(EquipmentSlotType.QuickSlot3), item => EquipQuickSlot(EquipmentSlotType.QuickSlot3, item), this);
            
            this.ListenTo(ICharacterInventory.Events.SlotUnequipped(EquipmentSlotType.QuickSlot2), () => FindNextEquippedQuickSlot(EquipmentSlotType.QuickSlot2), this);
            this.ListenTo(ICharacterInventory.Events.SlotUnequipped(EquipmentSlotType.QuickSlot3), () => FindNextEquippedQuickSlot(EquipmentSlotType.QuickSlot3), this);
        }
        
        [UnityEngine.Scripting.Preserve]
        public void LockEquipping(bool locked) {
            _equippingLocked = locked;
        }

        void EquipArrows(Item _) {
            if (World.HasAny<LoadoutsUI>() || World.HasAny<BagUI>()) {
                return;
            }
            var loadout = (HeroLoadout)CurrentLoadout;
            if (loadout.IsSlotLocked(EquipmentSlotType.Quiver)) {
                return;
            }

            Item item = loadout.SecondaryItem;
            if (item == null || item.HasBeenDiscarded) {
                item = Inventory.Where(i => i.IsArrow && !i.Locked).MaxBy(i => i.Quality.Priority, true);
            }

            if (item is { IsArrow: true }) {
                this.Equip(item, EquipmentSlotType.Quiver);
            }
        }

        void EquipPickedArrows(Item item) {
            if (item is not { IsArrow : true, IsSpectralWeapon: false } || World.HasAny<LoadoutsUI>() || World.HasAny<BagUI>()) {
                return;
            }

            foreach (HeroLoadout loadout in Loadouts) {
                if (loadout.PrimaryItem is { IsRanged: true, IsSpectralWeapon: false } && loadout[EquipmentSlotType.Quiver] == null) {
                    loadout.InternalAssignItem(EquipmentSlotType.Quiver, item);
                    if (loadout == CurrentLoadout) {
                        loadout.EquipLoadoutItems();
                    }
                }
            }
        }

        void EquipFood(Item unEquippedItem) {
            if (unEquippedItem?.WasDiscardedFromDomainDrop ?? false) {
                return;
            }
            
            if (World.HasAny<LoadoutsUI>()) {
                return;
            }
            if (_itemInSlots.IsEquipped(EquipmentSlotType.FoodQuickSlot)) {
                return;
            }
            var item = Inventory.Where(i => i.IsPlainFood).MaxBy(i => i.GetHealValue(), true);
            if (item != null) {
                this.Equip(item, EquipmentSlotType.FoodQuickSlot);
            } else {
                SelectNextQuickSlot();
            }
        }

        void EquipQuickSlot(EquipmentSlotType slotType, Item item) {
            if (_itemInSlots.IsEquipped(SelectedQuickSlotType)) {
                return;
            }

            if (item != null) {
                this.Equip(item, slotType);
                SelectNextQuickSlot();
            }
        }

        void FindNextEquippedQuickSlot(EquipmentSlotType slotType) {
            if (World.HasAny<LoadoutsUI>()) {
                return;
            }

            if (_itemInSlots.IsEquipped(slotType)) {
                return;
            }
            
            SelectNextQuickSlot();
        }

        void EquipPickedFood(Item item) {
            if (item is { IsPlainFood : true } && !World.HasAny<LoadoutsUI>()) {
                if (!_itemInSlots.IsEquipped(EquipmentSlotType.FoodQuickSlot)) {
                    this.Equip(item, EquipmentSlotType.FoodQuickSlot);
                }
            }
        }

        // === Loadouts
        public HeroLoadout LoadoutAt(int index) {
            foreach (var loadout in Elements<HeroLoadout>()) {
                if (loadout.LoadoutIndex == index) {
                    return loadout;
                }
            }
            throw new IndexOutOfRangeException($"There is no loadout with index {index}");
        }

        [UnityEngine.Scripting.Preserve]
        public void LockLoadouts(bool locked) {
            _loadoutLocked = locked;
        }
        
        public void ActivateLoadout(int index, bool manualActivate = true) {
            if (_loadoutLocked) {
                return;
            }
            
            if (CurrentLoadoutIndex != index) {
                int previousLoadout = CurrentLoadoutIndex;
                CurrentLoadoutIndex = index;
                CurrentLoadout.Activate();
                if (manualActivate) {
                     RewiredHelper.VibrateHighFreq(VibrationStrength.Medium, VibrationDuration.VeryShort);
                }

                this.Trigger(HeroLoadout.Events.LoadoutChanged, new Change<int>(previousLoadout, CurrentLoadoutIndex));
            }
        }

        public bool IsEquipped(EquipmentSlotType slotType) {
            return _itemInSlots.IsEquipped(slotType);
        }
        
        public bool TryGetSelectedQuickSlotItem(out Item selectedItem) {
            selectedItem = _itemInSlots[SelectedQuickSlotType];
            return selectedItem != null;
        }
        
        public void TryGetAllNextQuickSlotItems(ref Item[] items) {
            for (int i = 1; i < EquipmentSlotType.QuickSlots.Length; i++) {
                EquipmentSlotType slotType = EquipmentSlotType.QuickSlots[(_selectedQuickSlot + i) % EquipmentSlotType.QuickSlots.Length];
                Item item = _itemInSlots[slotType];
                items[i - 1] = item;
            }
        }

        public void SelectNextQuickSlot() {
            for (int i = 0; i < EquipmentSlotType.QuickSlots.Length; i++) {
                _selectedQuickSlot = (_selectedQuickSlot + 1) % EquipmentSlotType.QuickSlots.Length;
                if (_itemInSlots.IsEquipped(SelectedQuickSlotType)) {
                    Hero.Current.Trigger(Events.QuickSlotSelected, SelectedQuickSlotType);
                    PlayQuickSlotSwitchSound();
                    return;
                }
            }
        }

        public void SelectQuickSlot(EquipmentSlotType equipmentSlotType) {
            _selectedQuickSlot = Array.IndexOf(EquipmentSlotType.QuickSlots, equipmentSlotType);
            if(_selectedQuickSlot == -1) {
                Log.Critical?.Error($"Invalid quick slot type: {equipmentSlotType}");
                _selectedQuickSlot = 0;
                equipmentSlotType = EquipmentSlotType.QuickSlots[_selectedQuickSlot];
            }
            Hero.Current.Trigger(Events.QuickSlotSelected, equipmentSlotType);
        }

        public Item GetMainHandFist() {
            if (MainHandFist == null) {
                ItemTemplate mainHandFist = Services.Get<CommonReferences>().DefaultMainHandFistsTemplate;
                MainHandFist = World.Add(new Item(mainHandFist));
                Add(MainHandFist);
            }

            return MainHandFist;
        }

        public Item GetOffHandFist() {
            if (OffHandFist == null) {
                ItemTemplate offHandFist = Services.Get<CommonReferences>().DefaultOffHandFistsTemplate;
                OffHandFist = World.Add(new Item(offHandFist));
                Add(OffHandFist);
            }

            return OffHandFist;
        }
        
        // === Operations
        [UnityEngine.Scripting.Preserve]
        public void AddWithoutNotification(Item item, bool allowStacking = true) {
            using var suspendNotification = new AdvancedNotificationBuffer.SuspendNotifications<ItemNotificationBuffer>();
            Add(item, allowStacking);
        }
        
        public Item Add(Item item, bool allowStacking = true) {
            if (!item.IsInitialized) {
                World.Add(item);
            }
            // check if item is already owned by hero
            if (OwnedItems.Contains(item)) return item;

            var hookResult = ICharacterInventory.Events.ItemToBeAddedToInventory.RunHooks(item, new ICharacterInventory.AddingItemInfo(this, item));
            if (hookResult.Prevented) {
                return item;
            }
            item = hookResult.Value.Item;
            
            this.Trigger(ICharacterInventory.Events.BeforePickedUpItem, item);

            // --- Play Audio
            EventReference eventReference = ItemAudioType.PickupItem.RetrieveFrom(item);
            if (!eventReference.IsNull) {
                //RuntimeManager.PlayOneShot(eventReference);
            }
            
            if (allowStacking && Items.TryStackItem(item, out var stackedTo)) {
                this.Trigger(ICharacterInventory.Events.PickedUpItem, stackedTo);
                return stackedTo;
            }

            if (item.Inventory != null) {
                throw new Exception("Item still has a reference to its old inventory. This is not allowed!");
            }

            if (!OwnedItems.Add(item)) {
                return item;
            }
            ItemUtils.AnnounceGettingItem(item.Template, item.Quantity, ParentModel);
            AddNewItemToInventory(item);

            if (item.IsEquippable) {
                ParentModel.Trigger(Events.PickedUpEquippable, item);
            }

            return item;
        }

        public void Remove(Item item, bool discard = true) {
            if (!OwnedItems.Contains(item)) return;

            PostponeEquipmentChange? postponeEquipmentChange = item.IsUsedInLoadout() ? new PostponeEquipmentChange(this) : null;
            
            if (item.IsEquipped) {
                this.Unequip(item);
            }

            RemoveFromInventory(item);
            OwnedItems.Remove(item);
            ItemUtils.AnnounceGettingItem(item.Template, -item.Quantity, ParentModel);
            if (discard) {
                item.Discard();
            }

            postponeEquipmentChange?.Dispose();
        }

        public void Drop(Item item, int quantity) {
            var data = new DroppedItemData {
                item = item,
                quantity = quantity,
                elementsData = item.TryGetRuntimeData()
            };
            this.Trigger(ICharacterInventory.Events.ItemDropped, data);
            ItemUtils.RemoveItem(item, quantity);
        }

        void ICharacterInventory.EquipSlotInternal(EquipmentSlotType slotType, Item item) {
            _itemInSlots.Equip(slotType, item);
        }

        void ICharacterInventory.EquipItemInternal(Item item) {
            RemoveFromInventory(item);
        }

        void ICharacterInventory.UnequipSlotInternal(EquipmentSlotType slot) {
            _itemInSlots.Unequip(slot);
        }

        void ICharacterInventory.UnequipItemInternal(Item item) {
            ReturnToInventory(item);
        }

        bool HasAnyNextSlotItems() {
            for (int i = 1; i < EquipmentSlotType.QuickSlots.Length; i++) {
                EquipmentSlotType slotType = EquipmentSlotType.QuickSlots[(_selectedQuickSlot + i) % EquipmentSlotType.QuickSlots.Length];
                if (_itemInSlots.IsEquipped(slotType)) {
                    return true;
                }
            }
            return false;
        }
        
        void PlayQuickSlotSwitchSound() {
            FMODManager.PlayOneShot(HasAnyNextSlotItems() ? AudioConfig.SwitchSlotSound : AudioConfig.LightNegativeFeedbackSound);
        }
        
        /// <summary>
        /// Move item to specific place in inventory. Returns if slot is already taken.
        /// </summary>
        void MoveInInventory(Item item, int newIndex) {
            Item replacedItem = Inventory.FirstOrDefault(i => 
                i != item && i.Element<ItemSlot>().Index == newIndex);

            // do we need to swap
            if (replacedItem != null) {
                // swap in inventory
                if (item.HasElement<ItemSlot>()) {
                    SwapInInventory(item, replacedItem);
                    return;
                }

                throw new ArgumentException($"Can't put item {item.ID} into inventory slot {newIndex}");
            }

            // moving item to empty slot
            if (item.IsEquipped) {
                this.Unequip(item);
            }
            AssignInventoryIndex(item, newIndex);
            TriggerChange();
        }

        // === Inventory management
        
        void AddNewItemToInventory(Item item) {
            AddItemToInventory(item);
            item.SetPickedTimestamp();

            this.Trigger(ICharacterInventory.Events.PickedUpNewItem, item);
            this.Trigger(ICharacterInventory.Events.PickedUpItem, item);
        }

        void AddItemToInventory(Item item) {
            int freeSlot = FirstFreeSlot();
            AssignInventoryIndex(item, freeSlot);
        }
        
        Item ReturnToInventory(Item item) {
            if (Items.TryStackItem(item, out var stackedTo)) {
                // successfully stacked item
                this.Trigger(ICharacterInventory.Events.PickedUpItem, item);
                return stackedTo;
            }
            AddItemToInventory(item);
            return item;
        }

        void RemoveFromInventory(Item item) {
            if (!ContainedItems.Contains(item)) return;
            
            item.RemoveElementsOfType<ItemSlot>();
            ContainedItems.Remove(item);

            World.EventSystem.RemoveAllListenersBetween(item, this);
        }

        void SwapInInventory(Item a, Item b) {
            if (a.IsEquippable != b.IsEquippable) {
                Log.Important?.Error("Cannot swap with other item");
                return;
            }
            int slotA = a.Element<ItemSlot>().Index;
            int slotB = b.Element<ItemSlot>().Index;
            RemoveFromInventory(b);
            MoveInInventory(a, slotB);
            MoveInInventory(b, slotA);
        }

        void AssignInventoryIndex(Item item, int index) {
            if (item.CharacterInventory != this) {
                Log.Important?.Error("Item still has a reference to its old inventory. This is not allowed!");
            }
            
            ItemSlot itemSlot = item.TryGetElement<ItemSlot>();
            if (itemSlot == null) {
                item.AddElement(new ItemSlot(index));
            } else {
                itemSlot.AssignIndex(index);
            }

            if (!ContainedItems.Contains(item)) {
                ContainedItems.Add(item);
            }
        }

        // === Helpers
        int FirstFreeSlot() {
            int i = 0;
            var items = Inventory.ToArray();
            HashSet<int> takenIndices = new HashSet<int>(items.Select(item => item.Element<ItemSlot>().Index));
            while (takenIndices.Contains(i)) {
                i++;
            }

            return i;
        }

        // === Discard
        protected override void OnDiscard(bool fromDomainDrop) {
            _itemInSlots.Teardown();
        }
    }
}
