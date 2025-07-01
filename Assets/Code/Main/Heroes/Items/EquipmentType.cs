using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items.Loadouts;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Heroes.Items {
    public enum EquipmentCategory {
        Armor,
        Weapon,
        Ammo,
        Accessory,
        QuickUse,
    }
    
    [RichEnumDisplayCategory("EQ")]
    public class EquipmentType : RichEnum {
        public EquipmentCategory Category { get; }
        public EquipmentSlotType MainSlotType { get; }
        public LocString Name { get; }

        readonly EquippingPolicy _equipping;

        public static readonly EquipmentType
            Fists = new(nameof(Fists), EquipmentCategory.Weapon, EquipmentSlotType.MainHand, new OneHandedEquipping()),
            OneHanded = new(nameof(OneHanded), EquipmentCategory.Weapon, EquipmentSlotType.MainHand, new OneHandedEquipping(), LocTerms.ItemTypeOneHanded),
            TwoHanded = new(nameof(TwoHanded), EquipmentCategory.Weapon, EquipmentSlotType.MainHand, new TwoHandedEquipping(), LocTerms.ItemTypeTwoHanded),
            Magic = new(nameof(Magic), EquipmentCategory.Weapon, EquipmentSlotType.MainHand, new OneHandedEquipping(), LocTerms.ItemTypeMagic),
            MagicTwoHanded = new(nameof(MagicTwoHanded), EquipmentCategory.Weapon, EquipmentSlotType.MainHand, new TwoHandedEquipping(), LocTerms.ItemTypeMagic),
            Shield = new(nameof(Shield), EquipmentCategory.Weapon, EquipmentSlotType.OffHand, new OneHandedEquipping(), LocTerms.ItemTypeShield),
            Rod = new(nameof(Rod), EquipmentCategory.Weapon, EquipmentSlotType.MainHand, new OneHandedEquipping(), LocTerms.ItemTypeRod),
            Bow = new(nameof(Bow), EquipmentCategory.Weapon, EquipmentSlotType.MainHand, new BowEquipping(), LocTerms.ItemTypeBow),

            Arrow = new(nameof(Arrow), EquipmentCategory.Ammo, EquipmentSlotType.Quiver, new ArrowEquipping(), LocTerms.ItemTypeArrow),

            Cuirass = new(nameof(Cuirass), EquipmentCategory.Armor, EquipmentSlotType.Cuirass, new DefaultEquipping(), LocTerms.ItemTypeCuirass),
            Helmet = new(nameof(Helmet), EquipmentCategory.Armor, EquipmentSlotType.Helmet, new DefaultEquipping(), LocTerms.ItemTypeHelmet),
            Gauntlets = new(nameof(Gauntlets), EquipmentCategory.Armor, EquipmentSlotType.Gauntlets, new DefaultEquipping(), LocTerms.ItemTypeGauntlets),
            Greaves = new(nameof(Greaves), EquipmentCategory.Armor, EquipmentSlotType.Greaves, new DefaultEquipping(), LocTerms.ItemTypeGreaves),
            Boots = new(nameof(Boots), EquipmentCategory.Armor, EquipmentSlotType.Boots, new DefaultEquipping(), LocTerms.ItemTypeBoots),
            Back = new(nameof(Back), EquipmentCategory.Armor, EquipmentSlotType.Back, new DefaultEquipping(), LocTerms.ItemTypeBack),

            Amulet = new(nameof(Amulet), EquipmentCategory.Accessory, EquipmentSlotType.Amulet, new DefaultEquipping(), LocTerms.ItemTypeAmulet),
            Ring = new(nameof(Ring), EquipmentCategory.Accessory, EquipmentSlotType.Ring1, new DefaultEquipping(), LocTerms.ItemTypeRing),

            Throwable = new(nameof(Throwable), EquipmentCategory.QuickUse, EquipmentSlotType.Throwable, new DefaultEquipping(), LocTerms.ItemTypeThrowable),
            QuickUse = new(nameof(QuickUse), EquipmentCategory.QuickUse, EquipmentSlotType.QuickSlot2, new DefaultEquipping(), LocTerms.ItemTypeConsumable),
        
            HorseArmor = new(nameof(HorseArmor), EquipmentCategory.Accessory, EquipmentSlotType.HorseArmor, new DefaultEquipping(), LocTerms.ItemTypeHorseArmor);

        
        EquipmentType(string enumName, EquipmentCategory category, EquipmentSlotType mainSlotType, EquippingPolicy equipping, string nameID = "") : base(enumName, $"{category}") {
            Category = category;
            MainSlotType = mainSlotType;
            _equipping = equipping;
            Name = new LocString { ID = nameID };
        }
        
        public bool IsArmor => Category == EquipmentCategory.Armor;
        public bool IsWeapon => Category == EquipmentCategory.Weapon;
        public bool IsAmmo => Category == EquipmentCategory.Ammo;
        public bool IsAccessory => Category == EquipmentCategory.Accessory;
        [UnityEngine.Scripting.Preserve] public bool IsQuickUse => Category == EquipmentCategory.QuickUse;

        public bool ProvidesCloth => IsArmor || IsAmmo;

        public void ResolveEquipping(Item item, ICharacterInventory inventory, ILoadout loadout, EquipmentSlotType desiredSlot) {
            if (loadout is HeroLoadout heroLoadout && !heroLoadout.CanEquipItem(item, desiredSlot)) {
                return;
            }
            _equipping.Apply(item, inventory, loadout, desiredSlot);
        }
        
        public static readonly EquipmentType[] OneHandedTypes = {
            EquipmentType.Fists,
            EquipmentType.OneHanded,
            EquipmentType.Shield,
            EquipmentType.Magic,
            EquipmentType.Rod
        };

        public static readonly EquipmentType[] CustomMainSlotTypes = {
            EquipmentType.Magic,
            EquipmentType.OneHanded,
            EquipmentType.Fists,
            EquipmentType.Shield,
            EquipmentType.Rod,
            EquipmentType.Ring,
            EquipmentType.QuickUse,
            EquipmentType.Throwable,
            EquipmentType.HorseArmor
        };
    }
}