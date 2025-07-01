using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Tabs;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Items.Loadouts;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Enums;
using JetBrains.Annotations;

namespace Awaken.TG.Main.Heroes.Items {
    [RichEnumDisplayCategory("EQ-Slots")]
    public class EquipmentSlotType : RichEnum {

        /// <summary>
        /// Index by which Rules of EquipmentSlotType are retrieved. See Rules.ByIndex array. <br/>
        /// It is done that way to prevent cycling reference from EquipmentType and EquipmentSlotType. <br/>
        /// It is also used to retrieve Slot from ICharacterInventory. <br/>
        /// </summary>
        readonly int _index;
        
        public ItemsTabType[] FilterTabs { get; }
        public ItemsTabType SortingTab { get; }
        public int Index => _index;

        public Event<ICharacterInventory, Item> SlotEquipped { get; }
        public Event<ICharacterInventory, Item> SlotUnequipped { get; }
        public Event<ICharacterInventory, ICharacterInventory> SlotChanged { get; }

        public static readonly EquipmentSlotType
            MainHand = new(nameof(MainHand), 0, ItemsTabType.MainHands, ItemsTabType.EquippableWeapons),
            OffHand = new(nameof(OffHand), 1, ItemsTabType.OffHands, ItemsTabType.EquippableWeapons),
            Quiver = new(nameof(Quiver), 2, ItemsTabType.Quivers, ItemsTabType.All),

            Helmet = new(nameof(Helmet), 3, ItemsTabType.Armors, ItemsTabType.Armor),
            Cuirass = new(nameof(Cuirass), 4, ItemsTabType.Armors, ItemsTabType.Armor),
            Gauntlets = new(nameof(Gauntlets), 5, ItemsTabType.Armors, ItemsTabType.Armor ),
            Greaves = new(nameof(Greaves), 6, ItemsTabType.Armors, ItemsTabType.Armor),
            Boots = new(nameof(Boots), 7, ItemsTabType.Armors, ItemsTabType.Armor),
            Back = new(nameof(Back), 8, ItemsTabType.Armors, ItemsTabType.Armor),

            Amulet = new(nameof(Amulet), 9, ItemsTabType.Amulets, ItemsTabType.All),
            Ring1 = new(nameof(Ring1), 10, ItemsTabType.Rings, ItemsTabType.All),
            Ring2 = new(nameof(Ring2), 11, ItemsTabType.Rings, ItemsTabType.All),

            FoodQuickSlot = new(nameof(FoodQuickSlot), 12, ItemsTabType.FoodQuickSlots, ItemsTabType.EquippableConsumable),
            QuickSlot2 = new(nameof(QuickSlot2), 13, ItemsTabType.QuickSlots, ItemsTabType.EquippableConsumable),
            QuickSlot3 = new(nameof(QuickSlot3), 14, ItemsTabType.QuickSlots, ItemsTabType.EquippableConsumable),

            Throwable = new(nameof(Throwable), 15, ItemsTabType.Throwables, ItemsTabType.All),
            
            AdditionalMainHand = new(nameof(AdditionalMainHand), 16, ItemsTabType.MainHands, ItemsTabType.EquippableWeapons),
            AdditionalOffHand = new(nameof(AdditionalOffHand), 17, ItemsTabType.OffHands, ItemsTabType.EquippableWeapons),
        
            HorseArmor = new(nameof(HorseArmor), 18, ItemsTabType.Armors, ItemsTabType.All);

        EquipmentSlotType(string enumName, int index, ItemsTabType[] filterTabs, ItemsTabType sortingTab) : base(enumName) {
            _index = index;
            FilterTabs = filterTabs;
            SortingTab = sortingTab;

            SlotEquipped = new Event<ICharacterInventory, Item>($"{nameof(SlotEquipped)}<{enumName}>");
            SlotUnequipped = new Event<ICharacterInventory, Item>($"{nameof(SlotUnequipped)}<{enumName}>");
            SlotChanged = new Event<ICharacterInventory, ICharacterInventory>($"{nameof(SlotChanged)}<{enumName}>");
        }

        Rules MyRules => Rules.ByIndex[_index];

        public bool Accept(Item item, [CanBeNull] HeroLoadout loadout = null) {
            var equip = item.TryGetElement<ItemEquip>();
            return equip != null &&
                   (MyRules.AcceptedTypes?.Contains(equip.EquipmentType) ?? false) &&
                   (MyRules.AdditionalFilter?.Invoke(item) ?? true) &&
                   HeroLoadoutCondition(item, loadout);
        }
        
        public EquipmentCategory EquipmentCategory => MyRules.AcceptedTypes.First().Category;

        public static readonly EquipmentSlotType[] All = RichEnum.AllValuesOfType<EquipmentSlotType>().OrderBy(slot => slot._index).ToArray();

        public static readonly EquipmentSlotType[] Hands = { MainHand, OffHand };
        public static readonly EquipmentSlotType[] AdditionalHands = { AdditionalMainHand, AdditionalOffHand };
        public static readonly EquipmentSlotType[] Armors = { Helmet, Cuirass, Gauntlets, Greaves, Boots, Back };
        public static readonly EquipmentSlotType[] Rings = { Ring1, Ring2 };
        public static readonly EquipmentSlotType[] Accessories = { Amulet, Ring1, Ring2, HorseArmor };
        public static readonly EquipmentSlotType[] QuickSlots = { FoodQuickSlot, QuickSlot2, QuickSlot3 };
        public static readonly EquipmentSlotType[] ManualQuickSlots = { QuickSlot2, QuickSlot3 };
        public static readonly EquipmentSlotType[] Loadouts = { MainHand, OffHand, Quiver, Throwable, AdditionalMainHand, AdditionalOffHand };
        public static readonly EquipmentSlotType[] AllHands = { MainHand, OffHand, AdditionalMainHand, AdditionalOffHand };
        
        // === Helpers
        public static bool HeroLoadoutCondition(Item item, [CanBeNull] HeroLoadout loadout) {
            if (loadout?.HasElement<HeroLoadoutSlotLocker>() ?? false) {
                return item.EquipmentType != EquipmentType.TwoHanded &&
                       item.EquipmentType != EquipmentType.Bow &&
                       item.EquipmentType != EquipmentType.MagicTwoHanded;
            }
            return true;
        }
        
        class Rules {
            public EquipmentSlotType EquipmentSlotType { [UnityEngine.Scripting.Preserve] get; }
            public EquipmentType[] AcceptedTypes { get; }
            public Func<Item, bool> AdditionalFilter { get; }

            /// <summary>
            /// Order of elements of the array matches indexing of EquipmentSlotType enum.
            /// </summary>
            public static readonly Rules[] ByIndex = {
                new(MainHand, new [] {EquipmentType.Fists, EquipmentType.OneHanded, EquipmentType.Shield, EquipmentType.Rod, EquipmentType.TwoHanded, EquipmentType.Magic, EquipmentType.MagicTwoHanded, EquipmentType.Bow}),
                new(OffHand, new [] {EquipmentType.Fists, EquipmentType.OneHanded, EquipmentType.Shield, EquipmentType.Rod, EquipmentType.TwoHanded, EquipmentType.Magic, EquipmentType.MagicTwoHanded, EquipmentType.Bow}, AdditionalOffHandFilter),
                new(Quiver, new [] {EquipmentType.Arrow}),
                new(Helmet, new [] {EquipmentType.Helmet}),
                new(Cuirass, new [] {EquipmentType.Cuirass}),
                new(Gauntlets, new [] {EquipmentType.Gauntlets}),
                new(Greaves, new [] {EquipmentType.Greaves}),
                new(Boots, new [] {EquipmentType.Boots}),
                new(Back, new [] {EquipmentType.Back}),
                new(Amulet, new [] {EquipmentType.Amulet}),
                new(Ring1, new [] {EquipmentType.Ring}),
                new(Ring2, new [] {EquipmentType.Ring}),
                new(FoodQuickSlot, new [] {EquipmentType.QuickUse}, AdditionalFoodFilter),
                new(QuickSlot2, new [] {EquipmentType.QuickUse, EquipmentType.Throwable}),
                new(QuickSlot3, new [] {EquipmentType.QuickUse, EquipmentType.Throwable}),
                new(Throwable, new [] {EquipmentType.Throwable}),
                new(AdditionalMainHand, new [] {EquipmentType.Fists, EquipmentType.OneHanded, EquipmentType.Shield, EquipmentType.Rod, EquipmentType.TwoHanded, EquipmentType.Magic, EquipmentType.MagicTwoHanded, EquipmentType.Bow}),
                new(AdditionalOffHand, new [] {EquipmentType.Fists, EquipmentType.OneHanded, EquipmentType.Shield, EquipmentType.Rod, EquipmentType.TwoHanded, EquipmentType.Magic, EquipmentType.MagicTwoHanded, EquipmentType.Bow}, AdditionalOffHandFilter),
                new(HorseArmor, new [] {EquipmentType.HorseArmor}),
            };

            Rules(EquipmentSlotType type, EquipmentType[] acceptedTypes, Func<Item, bool> additionalFilter = null) {
                EquipmentSlotType = type;
                AcceptedTypes = acceptedTypes;
                AdditionalFilter = additionalFilter;
            }

            static bool AdditionalOffHandFilter(Item item) {
                if (item?.Owner is not Hero hero) {
                    return true;
                }
                
                var mainHandEquipmentType = hero.Element<HeroItems>().EquippedItem(MainHand)?.EquipmentType;
                if (mainHandEquipmentType == null || 
                    mainHandEquipmentType == EquipmentType.Fists ||
                    (mainHandEquipmentType != EquipmentType.Magic && mainHandEquipmentType != EquipmentType.OneHanded)) {
                    return true;
                }

                return item.EquipmentType != EquipmentType.TwoHanded &&
                       item.EquipmentType != EquipmentType.Bow &&
                       item.EquipmentType != EquipmentType.MagicTwoHanded;
            }

            static bool AdditionalFoodFilter(Item item) {
                if (item?.Owner is not Hero) {
                    return true;
                }
                
                return item.IsPlainFood;
            }
        }
    }
}