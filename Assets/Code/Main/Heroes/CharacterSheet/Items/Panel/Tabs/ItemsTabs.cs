using System;
using System.Linq;
using Awaken.TG.Main.General.NewThings;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Bag;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Tabs {
    public partial class ItemsTabs : Tabs<ItemsUI, VItemsTabs, ItemsTabType, ItemsListUI> {
        protected override KeyBindings Previous => World.HasAny<BagUI>() 
            ? KeyBindings.UI.Generic.DecreaseValueAlt 
            : KeyBindings.UI.Generic.PreviousAlt;
        protected override KeyBindings Next => World.HasAny<BagUI>() 
            ? KeyBindings.UI.Generic.IncreaseValueAlt
            : KeyBindings.UI.Generic.NextAlt;

        protected override void OnInitialize() {
            base.OnInitialize();
            Refresh();
        }

        public void Refresh() {
            int currentTabIndex = VisibleTabs.IndexOf(ParentModel.CurrentType);
            TriggerChange();
            
            if (!ParentModel.CurrentType.IsVisible(ParentModel)) {
                var visibleTabs = VisibleTabs.ToArray();
                
                if (visibleTabs.Length == 0) {
                    return;
                }
                
                currentTabIndex = Math.Max(0, currentTabIndex - 1);
                ChangeTab(visibleTabs[currentTabIndex]);
            } 
        }
    }

    public class ItemsTabType : ItemsTabs.TabTypeEnum {
        readonly Func<Item, bool> _filter;
        readonly Func<Item, bool> _gridException;

        // All or AllWithRecent are always visible to correctly handle the empty state
        public override bool IsVisible(ItemsUI target) => target.Tabs.Contains(this) && target.Items.Any(Contains) 
                                                          || AllVisible(target)
                                                          || AllWithRecentVisible(target); 
        public override ItemsListUI Spawn(ItemsUI target) => new(this);
        
        public bool Contains(Item item) => _filter(item);
        public bool ContainsInGrid(Item item) => _filter(item) && !_gridException(item);
        public ItemsTabType[] SubTabs { get; }

        bool AllVisible(ItemsUI target) => this == All && target.Tabs.Contains(All) && !target.Tabs.Contains(AllWithRecent);
        bool AllWithRecentVisible(ItemsUI target) => this == AllWithRecent && target.Tabs.Contains(AllWithRecent) && !target.Tabs.Contains(All);

        ItemsTabType(string enumName, Func<Item, bool> filter, string titleID = "", ItemsTabType[] subTabs = null, Func<Item, bool> gridException = null) : base(enumName, titleID) {
            _filter = filter;
            _gridException = gridException ?? (_ => false);
            SubTabs = subTabs;
        }
        
        public bool ContainsSubTab(ItemsTabType tab) {
            // All and None are special cases that cannot contain other tabs, but are always considered to contain any tab
            if (this == All || this == None) return true;
            if (SubTabs == null) return false;
            return SubTabs.Contains(tab) || SubTabs.Any(subTab => subTab.ContainsSubTab(tab));
        }
        
        [UnityEngine.Scripting.Preserve]
        public static readonly ItemsTabType
            None = new (nameof(None), _ => true, LocTerms.ItemsTabAll),
            All = new(nameof(All), _ => true, LocTerms.ItemsTabAll),
            Recent = new(nameof(Recent), i => !World.Services.Get<NewThingsTracker>().WasSeen(i.NewThingId), LocTerms.ItemsTabRecent),
            AllWithRecent = new(nameof(AllWithRecent), _ => true, LocTerms.ItemsTabAll, new[] { All, Recent }),

            OneHanded = new(nameof(OneHanded), i => i.IsOneHanded && i.IsMelee, LocTerms.ItemsTabOneHanded, gridException: i => i.IsShield),
            TwoHanded = new(nameof(TwoHanded), i => i.IsTwoHanded && i.IsMelee, LocTerms.ItemsTabTwoHanded),
            Magic = new(nameof(Magic), i => i.IsMagic, LocTerms.ItemsTabMagic),
            Arrows = new(nameof(Arrows), i => i.IsArrow, LocTerms.ItemsTabArrows),
            Ranged = new(nameof(Ranged), i => i.IsRanged, LocTerms.ItemsTabRanged),
            Shields = new(nameof(Shields), i => i.IsShield, LocTerms.ItemsTabShields),
            
            CommonComponents = new(nameof(CommonComponents), i => i.IsCommonComponent, LocTerms.Common),
            CookingComponents = new(nameof(CookingComponents), i => i.IsCookingComponent, LocTerms.Cooking, gridException: i => i.IsCommonComponent),
            CraftingComponents = new(nameof(CraftingComponents), i => i.IsCraftingComponent, LocTerms.Handcrafting, gridException: i => i.IsCommonComponent),
            AlchemyComponents = new(nameof(AlchemyComponents), i => i.IsAlchemyComponent, LocTerms.Alchemy, gridException: i => i.IsCommonComponent),
            
            Dish = new(nameof(Dish), i => i.IsDish, LocTerms.ItemTypeDish),
            Potion = new(nameof(Potion), i => i.IsPotion, LocTerms.ItemTypePotion),
            BuffApplier = new(nameof(BuffApplier), i => i.IsBuffApplier, LocTerms.ItemTypeBuffApplier),
            RestConsumables = new(nameof(RestConsumables), i => i.IsConsumable && !i.IsPotion && !i.IsCrafting && !i.IsDish && !i.IsBuffApplier, LocTerms.ItemTypeOther),

            Crafting = new(nameof(Crafting), i => i.IsCrafting, LocTerms.ItemsTabCrafting, new [] { All, CommonComponents, CookingComponents, CraftingComponents, AlchemyComponents }),
            Consumable = new(nameof(Consumable), i => i.IsConsumable && !i.IsPotion && !i.IsCrafting, LocTerms.ItemsTabConsumable, new [] { All, Dish, BuffApplier, RestConsumables }, i => i.IsDish || i.IsBuffApplier),
            EquippableConsumable = new(nameof(EquippableConsumable), i => i.IsConsumable, LocTerms.ItemsTabConsumable, new [] { All, Dish, Potion, BuffApplier }, i => i.IsDish || i.IsPotion || i.IsBuffApplier),
            Readable = new(nameof(Readable), i => i.IsReadable && !i.IsRecipe, LocTerms.ItemsTabReadable),
            Keys = new(nameof(Keys), i => i.IsKey, LocTerms.ItemsTabKeys),
            QuestItems = new(nameof(QuestItems), i => i.IsQuestItem, LocTerms.ItemsTabQuestItems, new []{ All, Keys }, i => i.IsKey),
            RestOthers = new(nameof(RestOthers), i => i.IsOther() && !i.IsKey, LocTerms.ItemTypeOther),
            Others = new(nameof(Others), i => i.IsOther(), LocTerms.OtherItems, new []{ All, Keys, RestOthers }, i => i.IsKey),
            
            Cuirass = new(nameof(Cuirass), i => i.EquipmentType == EquipmentType.Cuirass, LocTerms.ItemTypeCuirass),
            Helmet = new(nameof(Helmet), i => i.EquipmentType == EquipmentType.Helmet, LocTerms.ItemTypeHelmet),
            Gauntlets = new(nameof(Gauntlets), i => i.EquipmentType == EquipmentType.Gauntlets, LocTerms.ItemTypeGauntlets),
            Greaves = new(nameof(Greaves), i => i.EquipmentType == EquipmentType.Greaves, LocTerms.ItemTypeGreaves),
            Boots = new(nameof(Boots), i => i.EquipmentType == EquipmentType.Boots, LocTerms.ItemTypeBoots),
            Back = new(nameof(Back), i => i.EquipmentType == EquipmentType.Back, LocTerms.ItemTypeBack),

            Weapons = new(nameof(Weapons), i => (i.IsWeapon && !i.IsMagic) || i.IsArrow , LocTerms.ItemsTabWeapons, new[] { All, OneHanded, TwoHanded, Shields, Ranged, Arrows, }),
            EquippableWeapons = new(nameof(EquippableWeapons), i => i.IsWeapon || i.IsArrow , LocTerms.ItemsTabWeapons, new[] { All, OneHanded, TwoHanded, Magic, Shields, Ranged, Arrows, }),
            UpgradableWeapons = new(nameof(UpgradableWeapons), i => i.IsWeapon && !i.IsMagic && !i.IsArrow , LocTerms.ItemsTabWeapons, new[] { All, OneHanded, TwoHanded, Shields, Ranged, }),
            Armor = new(nameof(Armor), i => i.IsArmor, LocTerms.ItemsTabArmor, new[] { All, Helmet, Cuirass, Gauntlets, Greaves, Boots, Back, }),
            Gear = new(nameof(Gear), i => i.IsArmor || i.IsWeapon, LocTerms.ItemsTabGear),
            ArmorGemSet = new(nameof(ArmorGemSet), i => i.IsArmor || i.IsArmorGem, string.Empty),
            GearGemSet = new(nameof(GearGemSet), i => i.IsWeapon || i.IsWeaponGem, string.Empty),
            
            Ring = new(nameof(Ring), i => i.EquipmentType == EquipmentType.Ring, LocTerms.ItemTypeRing),
            Amulet = new(nameof(Amulet), i => i.EquipmentType == EquipmentType.Amulet, LocTerms.ItemTypeAmulet),
            Jewelry = new(nameof(Jewelry), i => i.IsJewelry, LocTerms.ItemsTabJewelry, new[] { All, Ring, Amulet }),
            ArmorGem = new(nameof(ArmorGem), i => i.IsArmorGem, LocTerms.ItemTypeArmorGem),
            WeaponGem = new(nameof(WeaponGem), i => i.IsWeaponGem, LocTerms.ItemTypeWeaponGem),
            Gem = new(nameof(Gem), i => i.IsGem, LocTerms.ItemsTabGems, new[] { All, ArmorGem, WeaponGem }),
            Recipes = new(nameof(Recipes), i => i.IsRecipe, LocTerms.ItemsTabRecipes),
            
            HorseArmor = new(nameof(HorseArmor), i => i.EquipmentType == EquipmentType.HorseArmor, LocTerms.ItemTypeHorseArmor),
            TransmogChooseSortingTab = new(nameof(TransmogChooseSortingTab), _ => true, LocTerms.ItemsTabAll);
                
        public static readonly ItemsTabType[] MainHands = { All, OneHanded, TwoHanded, Magic, Ranged, Shields, };
        public static readonly ItemsTabType[] OffHands = { All, OneHanded, TwoHanded, Magic, Shields, };
        public static readonly ItemsTabType[] Quivers = { All, };
        public static readonly ItemsTabType[] Throwables = { All, };
        public static readonly ItemsTabType[] Armors = { All, };
        public static readonly ItemsTabType[] Amulets = { All, };
        public static readonly ItemsTabType[] Rings = { All, };
        public static readonly ItemsTabType[] FoodQuickSlots = { All, Dish, };
        public static readonly ItemsTabType[] QuickSlots = { All, Dish, Potion, BuffApplier, };

        public static readonly ItemsTabType[] Bag = { AllWithRecent, Weapons, Magic, Armor, Jewelry, Gem, Potion, Consumable, Crafting, Readable, Recipes, QuestItems, Others };
        public static readonly ItemsTabType[] Shop = { All, Weapons, Magic, Armor, Jewelry, Gem, Potion, Consumable, Crafting, Readable, Recipes, Others };
        public static readonly ItemsTabType[] Storage = { All, Weapons, Magic, Armor, Jewelry, Gem, Potion, Consumable, Crafting, Readable, Recipes, Others };
        public static readonly ItemsTabType[] Relics = { All, UpgradableWeapons, Armor };
        public static readonly ItemsTabType[] Transmog = { UpgradableWeapons, Armor };
        public static readonly ItemsTabType[] Identify = { All };
    }
}