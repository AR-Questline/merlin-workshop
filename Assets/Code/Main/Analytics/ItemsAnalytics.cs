#if !UNITY_GAMECORE && !UNITY_PS5
using System;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Crafting;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.CharacterCreators;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Items.Loadouts;
using Awaken.TG.Main.Locations.Gems;
using Awaken.TG.Main.Locations.Gems.GemManagement;
using Awaken.TG.Main.Locations.Shops;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility.Extensions;

namespace Awaken.TG.Main.Analytics {
    /// <summary>
    /// Used number of events: ItemsCount * Events
    /// </summary>
    public partial class ItemsAnalytics : Element<GameAnalyticsController> {
        public sealed override bool IsNotSaved => true;

        public static string ItemName(ItemTemplate template) => NiceName(template != null ? template.name : template.ItemName);
        static string NiceName(string name) => AnalyticsUtils.EventName(name);
        float PlayTime => AnalyticsUtils.PlayTime;

        // === Initialization
        protected override void OnInitialize() {
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<Hero>(), this, OnHeroAdded);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelFullyInitialized<Hero>(), this, OnHeroInit);
            World.EventSystem.ListenTo(EventSelector.AnySource, Item.Events.BeforeActionPerformed, this, OnItemAction);
            World.EventSystem.ListenTo(EventSelector.AnySource, Item.Events.ItemSharpened, this, OnItemSharpened);
            World.EventSystem.ListenTo(EventSelector.AnySource, GemManagementUI.Events.GemDetached, this, OnRelicChanged);
            World.EventSystem.ListenTo(EventSelector.AnySource, GemManagementUI.Events.GemAttached, this, OnRelicChanged);
        }

        // === Callbacks
        void OnHeroAdded(Model model) {
            Hero hero = (Hero) model;
            foreach (var item in hero.Inventory.Items) {
                AnalyticsUtils.TrySendDesignEvent($"Items:AtStart:{ItemName(item.Template)}", item.Quantity);
            }
        }

        void OnHeroInit(Model model) {
            Hero hero = (Hero) model;
            hero.HeroItems.ListenTo(ICharacterInventory.Events.PickedUpItem, OnItemAcquired, this);
            hero.HeroItems.ListenTo(ICharacterInventory.Events.ItemDropped, OnItemDropped, this);
            
            hero.ListenTo(Awaken.TG.Main.Crafting.Crafting.Events.Created, OnItemCrafted, this);
            
            hero.ListenTo(IMerchant.Events.ItemSold, OnItemSold, this);
            hero.ListenTo(IMerchant.Events.ItemBought, OnItemBought, this);
            
            hero.ListenTo(Hero.Events.Died, OnHeroDeath, this);
            
            hero.AfterFullyInitialized(DelayedHeroInit, this);
        }

        void DelayedHeroInit() {
            var slots = EquipmentSlotType.All;
            var heroItems = Hero.Current.HeroItems;
            foreach (var slot in slots) {
                if (slot == EquipmentSlotType.MainHand ||
                    slot == EquipmentSlotType.OffHand ||
                    slot == EquipmentSlotType.Quiver) {
                    continue;
                }
                heroItems.ListenTo(ICharacterInventory.Events.SlotEquipped(slot), OnNonWeaponEquip, this);
                heroItems.ListenTo(ICharacterInventory.Events.SlotUnequipped(slot), OnNonWeaponUnequip, this);
            }

            var loadouts = Hero.Current.HeroItems.Elements<HeroLoadout>();
            foreach (var loadout in loadouts) {
                loadout.ListenTo(HeroLoadout.Events.ItemInLoadoutChanged, OnWeaponLoadoutChange, this);
            }
        }

        void OnItemSold(Item item) {
            if (!IsImportantItem(item)) {
                return;
            }
            string evt = $"Sold:{ItemType(item)}:{ItemName(item.Template)}";
            AnalyticsUtils.TrySendDesignEvent($"Items:{evt}", item.Quantity);
        }

        void OnItemBought(Item item) {
            if (!IsImportantItem(item)) {
                return;
            }
            string evt = $"Bought:{ItemType(item)}:{ItemName(item.Template)}";
            AnalyticsUtils.TrySendDesignEvent($"Items:{evt}", item.Quantity);
        }
        
        //Triggers on unequip. Other events ignore stackable items and require much more complicated system. Maybe it's good enough.
        void OnItemAcquired(Item item) {
            if (CharacterCreator.applyingBuildPreset) {
                return;
            }

            if (!IsImportantItem(item)) {
                return;
            }
            string evt = $"Acquired:{ItemType(item)}:{ItemName(item.Template)}";
            AnalyticsUtils.TrySendDesignEvent($"Items:{evt}", item.Quantity);
        }

        void OnItemDropped(DroppedItemData data) {
            Item item = data.item;
            if (!IsImportantItem(item)) {
                return;
            }
            string evt = $"Dropped:{ItemName(item.Template)}";
            AnalyticsUtils.TrySendDesignEvent($"Items:{evt}", data.quantity);
        }

        //Inactive loadouts not registering. Changing loadouts triggers this as well. Maybe it's fine.
        void OnWeaponLoadoutChange(HeroLoadout.LoadoutItemChange change) {
            if (CharacterCreator.applyingBuildPreset) {
                return;
            }

            var previousItem = change.from;
            var newItem = change.to;
            if (previousItem is {WasDiscardedFromDomainDrop: true} || newItem is {WasDiscardedFromDomainDrop: true}) {
                // Domain Drop discard items that is equipped and forces unequip event.
                // It should be ignored because entering main menu or loading game is not a valid game state.
                return;
            }

            bool previousTemplateIsInvalid = previousItem != null && 
                                             previousItem.Template.TemplateType is not TemplateType.Regular;
            bool newTemplateIsInvalid = newItem != null && 
                                        newItem.Template.TemplateType is not TemplateType.Regular;
            if (previousTemplateIsInvalid || newTemplateIsInvalid) {
                return;
            }

            if (previousItem != null) {
                bool twoHandedOffhand = change.slot == EquipmentSlotType.OffHand && previousItem.IsTwoHanded;
                bool fists = previousItem.Template.IsFists;
                bool unimportant = !IsImportantItem(previousItem);
                if (twoHandedOffhand || fists || unimportant) {
                    previousItem = null;
                }
            }
            if (newItem != null) {
                bool twoHandedOffhand = change.slot == EquipmentSlotType.OffHand && newItem.IsTwoHanded;
                bool fists = newItem.Template.IsFists;
                bool unimportant = !IsImportantItem(newItem);
                if (twoHandedOffhand || fists || unimportant) {
                    newItem = null;
                }
            }

            if (previousItem != null) {
                OnUnequipInternal(previousItem);
            }
            if (newItem != null) {
                OnEquipInternal(newItem);
            }
        }
        
        void OnNonWeaponEquip(Item equippedItem) {
            if (equippedItem == null || equippedItem.WasDiscardedFromDomainDrop) {
                return;
            }
            if (CharacterCreator.applyingBuildPreset) {
                return;
            }

            if (equippedItem.IsWeapon || equippedItem.IsMagic || equippedItem.IsArrow || !IsImportantItem(equippedItem)) {
                return;
            }
            OnEquipInternal(equippedItem);
        }
        
        void OnNonWeaponUnequip(Item unequippedItem) {
            if (unequippedItem == null || unequippedItem.WasDiscardedFromDomainDrop) {
                return;
            }
            if (unequippedItem.IsWeapon || unequippedItem.IsMagic || unequippedItem.IsArrow || !IsImportantItem(unequippedItem)) {
                return;
            }
            OnUnequipInternal(unequippedItem);
        }

        void OnEquipInternal(Item item) {
            string evt = $"Equipped:{ItemType(item)}:{ItemName(item.Template)}";
            AnalyticsUtils.TrySendDesignEvent($"Items:{evt}:PlayTime", PlayTime);
        }

        void OnUnequipInternal(Item item) {
            string evt = $"Unequipped:{ItemType(item)}:{ItemName(item.Template)}";
            AnalyticsUtils.TrySendDesignEvent($"Items:{evt}:PlayTime", PlayTime);
        }
        
        void OnItemCrafted(CreatedEvent createdEvent) {
            var item = createdEvent.Item;
            int itemLevel = item.Level.ModifiedInt;
            int practicalityStat = Hero.Current.HeroRPGStats.Practicality.ModifiedInt;
            string evt = $"CraftedItem:{ItemType(item)}:{ItemName(item.Template)}";
            AnalyticsUtils.TrySendDesignEvent($"Items:{evt}:Practicality", practicalityStat);
            if (!item.Template.CanHaveItemLevel) {
                return;
            }
            AnalyticsUtils.TrySendDesignEvent($"Items:{evt}:ItemLevel", itemLevel);
            
            string craftingType = NiceName(createdEvent.CraftingTemplate.name);
            string levelString = ItemLevel(itemLevel);
            
            evt = $"{craftingType}:ItemLevel:{levelString}";
            AnalyticsUtils.TrySendDesignEvent($"Crafting:{evt}:Practicality", practicalityStat);
        }

        void OnItemAction(ItemActionEvent itemActionEvent) {
            var item = itemActionEvent.Item;
            if (!IsImportantItem(item, 0.2f)) {
                return;
            }
            if ((itemActionEvent.ActionType == ItemActionType.Use || itemActionEvent.ActionType == ItemActionType.Eat)) {
                string evt = $"UsedInExploration:{ItemType(item)}:{ItemName(item.Template)}";
                AnalyticsUtils.TrySendDesignEvent($"Items:{evt}:PlayTime", PlayTime);
            }
        }

        void OnItemSharpened(SharpeningUI.SharpeningChangeData data) {
            string itemType = ItemType(data.item);
            string evt = $"Sharpened:{itemType}:{ItemName(data.item.Template)}:{ItemLevel(data.newLevel)}";
            AnalyticsUtils.TrySendDesignEvent($"Items:{evt}", data.newLevel);
            foreach ((ItemTemplate itemTemplate, int quantity) in data.itemsUsed) {
                evt = $"UsedInSharpening:{itemType}:{ItemName(itemTemplate)}";
                AnalyticsUtils.TrySendDesignEvent($"Items:{evt}:Count", quantity);
            }
        }

        void OnRelicChanged(GemManagementUI.GemAttachmentChange change) {
            string itemType = ItemType(change.item);
            string changeType = change.attached ? "InsertedRelic" : "RemovedRelic";
            string evt = $"{changeType}:{itemType}:{ItemName(change.item.Template)}:{ItemName(change.gem.Template)}";
            AnalyticsUtils.TrySendDesignEvent($"Items:{evt}");
        }

        void OnHeroDeath(DamageOutcome outcome) {
            if (outcome.Target is not Hero hero) {
                return;
            }
            var heroItems = hero.HeroItems;
            foreach (var item in heroItems.DistinctEquippedItems()) {
                AnalyticsUtils.TrySendDesignEvent($"Items:DiedWhileEquipped:{ItemType(item)}:{ItemName(item.Template)}");
            }
        }

        public static string ItemType(Item item) {
            if (item.IsQuestItem) return "QuestItems";
            if (item.IsMagic) return "Magic";
            if (item.IsWeapon) return "Weapons";
            if (item.IsJewelry) return "Jewelry";
            if (item.IsArmor) return "Armors";
            if (item.Template.IsGem) return "Relics";
            if (item.IsConsumable) return "Consumable";
            if (item.IsCrafting) return "Crafting";
            if (item.Template.IsReadable) return "Readable";
            return "Other";
        }

        /// <summary>
        /// To limit the number of possible strings some item level values are grouped together.
        /// The higher the number the bigger group size is used
        /// 0-9 are not grouped: "1", "2", [...], "9"
        /// 10-29 are grouped by 5: "10-14", "15-19", "20-24", "25-29"
        /// 30-99 are grouped by 10: "30-39", "40-49", [...], "90-99"
        /// 100+ are grouped all together: "100+"
        /// </summary>
        static string ItemLevel(int itemLevel) {
            switch (itemLevel) {
                case < 10:
                    return itemLevel.ToString();
                case < 30: {
                    int rangeMin = (int) (Math.Floor(itemLevel / 5f) * 5);
                    return $"{rangeMin}-{rangeMin + 4}";
                }
                case < 100: {
                    int rangeMin = (int) (Math.Floor(itemLevel / 10f) * 10);
                    return $"{rangeMin}-{rangeMin + 9}";
                }
                default: {
                    return "100+";
                }
            }
        }

        static bool IsImportantItem(Item item, float bonusScore = 0f) {
            if (item == null || item.Template == null || item.DisplayName.IsNullOrWhitespace() || item.HiddenOnUI) {
                return false;
            }
            if (item.Template.TemplateType is not TemplateType.Regular) {
                return false;
            }
            if (item.Quality == ItemQuality.Garbage) {
                return false;
            }
            if (item.Quality == ItemQuality.Quest || item.Quality == ItemQuality.Story) {
                return true;
            }

            float score = bonusScore;
            
            score += item.Quality == ItemQuality.Normal ? 0f : 0.5f;
            
            if (item.CanStack || item.IsConsumable) {
                score -= 0.25f;
            }

            if (item.Template.IsEquippable) {
                score += 0.25f;
            }

            switch (item.CrimeValue) {
                case CrimeItemValue.None:
                    break;
                case CrimeItemValue.Low:
                    break;
                case CrimeItemValue.Medium:
                    score += 0.25f;
                    break;
                case CrimeItemValue.High:
                    score += 1.5f;
                    break;
            }

            score += ((float) item.Price).Remap(10f, 500f, 0f, 2f);
            
            return score >= 1f;
        }
    }
}
#endif