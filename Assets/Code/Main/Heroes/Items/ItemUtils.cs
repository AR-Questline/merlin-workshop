using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Heroes.Items.Loadouts;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Heroes.Items.Weapons;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Locations.Shops;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.Item;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.Recipe;
using Awaken.TG.Main.Utility.TokenTexts;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Awaken.Utility.LowLevel.Collections;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;
using ItemNotificationData = Awaken.TG.Main.UI.HUD.AdvancedNotifications.Item.ItemData;

namespace Awaken.TG.Main.Heroes.Items {
    public static class ItemUtils {
        static readonly Color ColorWorse = ARColor.MainRed;
        public static readonly Color ColorEqual = ARColor.MainWhite;
        static readonly Color ColorBetter = ARColor.MainGreen;
        
        /// <summary>
        /// Fail-proof method for changing quantity of item, given only it's template.
        /// </summary>
        public static void ChangeQuantity(this ItemTemplate itemTemplate, IInventory inventory, int quantity, Item sourceItem = null, Item targetItem = null) {
            if (quantity == 0 || itemTemplate == null || inventory == null) {
                return;
            }
            
            // Adding items
            if (quantity > 0) {
                var itemData = new ItemSpawningDataRuntime(itemTemplate) {
                    quantity = quantity
                };
                if (sourceItem != null) {
                    itemData.elementsData = sourceItem.TryGetRuntimeData();
                }
                AddItems(inventory, itemData);
            } else {
                quantity = -quantity;
                
                targetItem ??= inventory.Items.FirstOrDefault(item => item.Template == itemTemplate);
                if (targetItem == null) return;

                RemoveItem(targetItem, quantity);
            }
        }
        /// <summary>
        /// Fail-proof method for changing quantity of item, given runtime spawning data
        /// </summary>
        public static void ChangeQuantity(this ItemSpawningDataRuntime spawningData, IInventory inventory, bool onlyStolen = false) {
            if (spawningData == null || spawningData.quantity == 0 || inventory == null) {
                return;
            }
            
            if (spawningData.quantity > 0) {
                AddItems(inventory, spawningData);
            } else {
                int quantityToRemove = -spawningData.quantity;
                RemoveItems(inventory, quantityToRemove, GetSimilarItemsInInventory(spawningData, inventory, onlyStolen));
            }
        }
        
        public static bool HasItem(this IInventory inventory, ItemSpawningDataRuntime spawningData, int count = 1) {
            return GetSimilarItemsInInventory(spawningData, inventory, false).Sum(i => i.Quantity) >= count;
        }

        public static void RemoveItem(Item targetItem, int quantity = 1) {
            if (targetItem.CanStack) {
                targetItem.ChangeQuantity(-quantity);
            } else {
                RemoveItems(targetItem.Inventory, quantity, GetSimilarItemsInInventory(targetItem));
            }
        }

        static void RemoveItems(IInventory targetInventory, int quantity, IEnumerable<Item> listOfItemsToRemoveFrom) {
            Queue<Item> heroItems = new(listOfItemsToRemoveFrom);
            while (quantity > 0 && heroItems.Count > 0) {
                Item item = heroItems.Dequeue();
                if (item.CanStack) {
                    int itemQuantity = item.Quantity;
                    item.ChangeQuantity(-quantity);

                    int quantityChange = itemQuantity - item.Quantity;
                    quantity -= quantityChange;
                } else {
                    targetInventory.Remove(item, false);
                    item.Discard();
                    quantity--;
                }
            }
        }

        public static void AddItems(this IInventory inventory, params ItemSpawningDataRuntime[] itemsToAdd) {
            foreach (ItemSpawningDataRuntime itemData in itemsToAdd) {
                if (itemData.ItemTemplate.CanStack) {
                    Item item = new Item(itemData);
                    World.Add(item);
                    inventory.Add(item);
                } else {
                    for (int i = 0; i < itemData.quantity; i++) {
                        Item item = new Item(itemData);
                        World.Add(item);
                        inventory.Add(item);
                    }
                }
            }
        }

        public static Item AddSingleItem(this IInventory inventory, ItemSpawningDataRuntime itemToAdd) {
            Item item = new(itemToAdd);
            World.Add(item);
            return inventory.Add(item, false);
        }
        
        public static bool TryStackItem(this IEnumerable<Item> inventory, Item item, out Item stackedTo) {
            stackedTo = null;
            if (!item.CanStack) return false;

            inventory = inventory.Where(i => i != item && i.Template == item.Template);

            inventory = item.HasElement<StolenItemElement>()
                ? inventory.Where(static i => i.HasElement<StolenItemElement>())
                : inventory.Where(static i => !i.HasElement<StolenItemElement>());

            if (item.Template.CanHaveItemLevel) {
                inventory = inventory.Where(i => i.Template.CanHaveItemLevel && i.Level.ModifiedInt == item.Level.ModifiedInt);
            } else {
                inventory = inventory.Where(static i => !i.Template.CanHaveItemLevel);
            }

            stackedTo = inventory.FirstOrDefault();
            if (stackedTo == null) return false;
            
            stackedTo.ChangeQuantity(item.Quantity);
            item.Discard();
            return true;
        }
        
        public static List<Item> GetSimilarItemsInInventory(this IInventory inventory, Func<Item, bool> selection, int quantity) {
            var allItems = inventory.GetSimilarItemsInInventory(selection);
            var selectedItems = new List<Item>();
            foreach (var item in allItems) {
                if (item.Quantity <= quantity) {
                    selectedItems.Add(item);
                    quantity -= item.Quantity;
                    if (quantity <= 0) {
                        return selectedItems;
                    }
                } else {
                    var takenQuantity = item.Quantity;
                    selectedItems.Add(item.TakeSome(quantity));
                    quantity -= takenQuantity;
                    if (quantity <= 0) {
                        return selectedItems;
                    }
                }
            }
            return selectedItems;
        }
        
        static List<Item> GetSimilarItemsInInventory(Item targetItem) =>
            targetItem.Inventory.Items
                      .Where(i => i.Template == targetItem.Template)
                      .OrderBy(i => i == targetItem ? -1 : 1) //We want the provided item to be first so that further operations always include it
                      .ThenBy(i => i.Quality == targetItem.Quality)
                      .ToList();
        
        public static List<Item> GetSimilarItemsInInventory(this IInventory inventory, Func<Item, bool> selection) =>
            inventory.Items
                     .Where(selection)
                     .OrderBy(i => i.Quality)
                     .ToList();

        static IEnumerable<Item> GetSimilarItemsInInventory(ItemSpawningDataRuntime spawningData, IInventory inventory, bool onlyStolen) =>
            inventory.Items
                     .Where(i => (!onlyStolen || i.IsStolen) && i.Template == spawningData.ItemTemplate)
                     .ToList();

        public static void MoveTo(this Item itemToMove, IInventory newOwner, int quantity) {
            if (quantity <= 0) throw new ArgumentException("Quantity to move needs to be greater than 0");
            
            if (itemToMove.CanStack) {
                if (quantity > itemToMove.Quantity) throw new ArgumentException("Quantity cannot be greater than existing amount");
                newOwner.Add(itemToMove.TakeSome(quantity));
            } else {
                List<Item> itemsForTransfer = GetSimilarItemsInInventory(itemToMove);
                if (quantity > itemsForTransfer.Count) throw new ArgumentException("Quantity cannot be greater than existing amount");
                
                Queue<Item> heroItems = new(itemsForTransfer);
                while (quantity > 0 && heroItems.Count > 0) {
                    Item item = heroItems.Dequeue();
                    item.MoveTo(newOwner);
                    quantity--;
                }
            }
        }
        /// <summary>
        /// Moves entire stack/item
        /// </summary>
        /// <returns>The item at new location or null if it was discarded</returns>
        [CanBeNull]
        public static Item MoveTo(this Item itemToMove, IInventory newOwner) {
            itemToMove.Inventory?.Remove(itemToMove, false);
            Item itemInNewInventory = newOwner.Add(itemToMove);
            return itemInNewInventory == null || itemInNewInventory.HasBeenDiscarded 
                       ? null 
                       : itemInNewInventory;
        }

        public static void ChangeItemQuantityByTags(this IInventory inventory, string[] tags, int quantity, Action<ItemTemplate, int> onQuantityChange = null, bool onlyStolen = false, string[] forbiddenTags = null) {
            if (quantity < 0) {
                DecreaseQuantityByTags(inventory, tags, quantity, onQuantityChange, onlyStolen, forbiddenTags);
            } else if (quantity > 0) {
                IncreaseQuantityByTags(inventory, tags, quantity, onQuantityChange);
            }
        }

        /// <summary>
        /// Randomly decrease quantity of items that contain given tags
        /// </summary>
        static void DecreaseQuantityByTags(IInventory inventory, string[] tags, int quantity, Action<ItemTemplate, int> onQuantityChange = null, bool onlyStolen = false, string[] forbiddenTags = null) {
            List<Item> taggedItems = inventory.Items.Where(i => (!onlyStolen || i.IsStolen) && TagUtils.HasRequiredTags(i, tags) && TagUtils.DoesNotHaveForbiddenTags(i, forbiddenTags)).ToList();
            taggedItems.Shuffle();
            // quantity is negative value
            int toTake = -quantity;
            while (taggedItems.Any() && toTake > 0) {
                Item item = taggedItems[0];
                int quantityToTake = Mathf.Min(toTake, item.Quantity);
                item.ChangeQuantity(-quantityToTake);
                if (item.WasDiscarded) {
                    taggedItems.RemoveAt(0);
                }

                onQuantityChange?.Invoke(item.Template, -quantityToTake);
                toTake -= quantityToTake;
            }
        }

        /// <summary>
        /// Increases quantity of contained items with given tags, or, if none is available, adds random item from resources with given tag
        /// </summary>
        static void IncreaseQuantityByTags(IInventory inventory, string[] tags, int quantity, Action<ItemTemplate, int> onQuantityChange = null) {
            var templates = ItemTemplates(tags).ToList();

            if (!templates.Any()) {
                return;
            }

            while (quantity > 0) {
                var template = RandomUtil.UniformSelect(templates);
                template.ChangeQuantity(inventory, 1);
                quantity--;
                onQuantityChange?.Invoke(template, 1);
            }
        }

        public static IEnumerable<ItemTemplate> GetRandomItemsByTag(string[] tags, int count) {
            var templates = ItemTemplates(tags).ToList();
            return RandomUtil.UniformSelectMultiple(templates, count);
        }
        
        public static IEnumerable<ItemTemplate> GetRandomItemsByTag(string[] tags) {
            var templates = ItemTemplates(tags).ToList();
            templates.Shuffle();
            return templates;
        }

        static IEnumerable<ItemTemplate> ItemTemplates(ICollection<string> tags) {
            return World.Services.Get<TemplatesProvider>().GetAllOfType<ItemTemplate>(tags);
        }
        
        public static string GetTemplateDescription(ItemTemplate itemTemplate, ICharacter owner = null) {
            return GetTemplateDescription(itemTemplate, new TokenText(itemTemplate.Description), owner);
        }
        
        public static string GetTemplateDescription(ItemTemplate itemTemplate, TokenText token, ICharacter owner = null) {
            Item tempItem = new(itemTemplate);
            World.Add(tempItem);
            var description = token.GetValue(owner, tempItem);
            tempItem.Discard();
            return description;
        }
        
        public static string CreateItemFlavorFromTemplate(ItemTemplate itemTemplate, ICharacter owner = null) {
            Item tempItem = new Item(itemTemplate);
            World.Add(tempItem);
            string flavor = tempItem.Flavor;
            tempItem.Discard();
            return flavor;
        }

        /// <summary>
        /// Only for initialized items!!
        /// </summary>
        public static string ItemQualityText(Item qualitySource) {
            if (!qualitySource.IsInitialized) {
                throw new Exception("Uninitialized item used");
            }
            ItemQuality itemQuality = qualitySource.Quality;
            return ($"\n{LocTerms.Quality.Translate()}: " + itemQuality.DisplayName)
                .ColoredText(itemQuality.BgColor.Hex).PercentSizeText(75);
        }

        [UnityEngine.Scripting.Preserve]
        public static string QualityText(Item item) {
            if (item == null) return "";
            return "\n" + ItemUtils.ItemQualityText(item);
        }

        [UnityEngine.Scripting.Preserve]
        public static string DisplayWeaponStats(ICharacter character, ItemTemplate itemTemplate) {
            Item tempItem = new Item(itemTemplate);
            World.Add(tempItem);
            var statsText = DisplayWeaponStats(character, tempItem);
            tempItem.Discard();
            return statsText;
        }

        public static string DisplayWeaponStats(ICharacter character, Item item) {
            if (!item.IsWeapon) return "";
            
            FloatRange minMax = Damage.PreCalculateDealtDamage(character, item);

            return LocTerms.BaseDamage.Translate() + " " + Mathf.RoundToInt(minMax.min) + "-" + Mathf.RoundToInt(minMax.max);
        }

        public static void AnnounceGettingItem(ItemTemplate itemTemplate, int quantity, IModel relatedModel) {
            if (!itemTemplate.IsQuestItem() && !itemTemplate.HiddenOnUI) {
                var notificationColor = quantity > 0 ? ARColor.MainGrey : ARColor.MainRed;
                var itemData = new ItemNotificationData(itemTemplate, Mathf.Abs(quantity), notificationColor);
                AdvancedNotificationBuffer.Push<ItemNotificationBuffer>(new ItemNotification(itemData));
            }
        }

        public static void AnnounceGettingRecipe(IRecipe recipe, IModel relatedModel) {
            if (recipe == null) {
                Log.Minor?.Error("Null recipe in AnnounceGettingRecipe");
                return;
            }
            var outcome = recipe.Outcome;
            if (outcome == null) {
                Log.Important?.Error($"Null outcome in recipe {recipe.GUID}");
                return;
            }
            
            var recipeData = new RecipeData(recipe);
            AdvancedNotificationBuffer.Push<RecipeNotificationBuffer>(new RecipeNotification(recipeData));
        }

        public static bool IsBetterThanEquipped(this Item item, Hero hero) {
            if (!item.IsEquippable) {
                return false;
            }

            EquipmentType equipmentType = item.EquipmentType;

            Item minItem = null;
            var minLevel = 999999;

            var inventory = hero.Inventory;
            foreach (var slot in EquipmentSlotType.All) {
                var equippedItem = inventory.EquippedItem(slot);
                if (equippedItem == null) { // Only equipped slots
                    continue;
                }
                if (equippedItem.EquipmentType != equipmentType) { // Only items of the same type
                    continue;
                }
                var equippedItemLevel = equippedItem.Level.ModifiedInt;
                if (minLevel > equippedItemLevel) {
                    minItem = equippedItem;
                    minLevel = equippedItemLevel;
                }
            }
            return minItem == null || minItem.Level.BaseInt < item.Level.BaseInt;
        }

        public static int IsGearBetterThanEquipped(this Item item) {
            if (!item.IsGear) {
                return 0;
            }
            
            int itemStat = 0;
            int equippedStat = 0;
            var inventory = Hero.Current.Inventory;
            var equippedItem = inventory.EquippedItem(item.EquipmentType.MainSlotType);

            if (equippedItem == null) {
                return 1;
            }
            
            if (item.IsArmor) {
                equippedStat = equippedItem.ItemStats.Armor.BaseInt;
                itemStat = item.ItemStats.Armor.BaseInt;
            } else if (item.IsBlocking) {
                equippedStat = equippedItem.ItemStats.Block.BaseInt;
                itemStat = item.ItemStats.Block.BaseInt;
            } else if (item.IsWeapon || item.IsArrow) {
                equippedStat = (equippedItem.ItemStats.BaseMinDmg.BaseInt + equippedItem.ItemStats.BaseMaxDmg.BaseInt) / 2;
                itemStat = (item.ItemStats.BaseMinDmg.BaseInt + item.ItemStats.BaseMaxDmg.BaseInt) / 2;
            }

            int diff = itemStat - equippedStat;
            return Math.Sign(diff);
        }

        public static bool IsQuestItem(this ItemTemplate itemTemplate) {
            return TagUtils.HasRequiredTag(itemTemplate, "item:quest");
        }

        public static bool IsOther(this ItemTemplate itemTemplate) {
            return !itemTemplate.IsArmor && !itemTemplate.IsWeapon && !itemTemplate.IsShield && !itemTemplate.IsArrow &&
                   !itemTemplate.IsCrafting && !itemTemplate.IsConsumable && !itemTemplate.IsQuestItem() &&
                   !itemTemplate.IsJewelry && !itemTemplate.IsBuffApplier;
        }

        public static bool IsOther(this Item item) {
            return IsOther(item.Template) && !item.IsReadable && !item.IsGem && !item.IsJewelry;
        }

        public static Item GetStatsItemForBlock(ICharacter character) {
            Item statsItem = character.Inventory.EquippedItem(EquipmentSlotType.OffHand);
            statsItem ??= character.Inventory.EquippedItem(EquipmentSlotType.MainHand);
            return statsItem;
        }

        public static string ItemTypeTranslation(Item item) {
            return ItemTypeTranslation(item, item.Template);
        }
        
        public static string ItemTypeTranslation(Item item, ItemTemplate template) {
            if (template.IsTool) {
                return LocTerms.ItemTypeTool.Translate();
            }

            if (template.IsArmor || template.IsWeapon || template.IsArrow) {
                return string.IsNullOrEmpty(template.EquipmentType.Name.Translate())
                    ? "GENERIC ITEM TYPE NAME! Requires correct setup" 
                    : template.EquipmentType.Name.Translate();
            }

            if (item?.IsWeaponGem ?? false) {
                return LocTerms.ItemTypeWeaponGem.Translate();
            }
            if (item?.IsArmorGem ?? false) {
                return LocTerms.ItemTypeArmorGem.Translate();
            }

            if (template.IsBuffApplier) {
                return LocTerms.ItemTypeBuffApplier.Translate();
            }

            if (template.IsConsumable) {
                return LocTerms.ItemTypeConsumable.Translate();
            }

            if (template.IsCrafting) {
                return LocTerms.ItemTypeCraftingReagent.Translate();
            }

            if (template.IsRecipe) {
                return LocTerms.ItemTypeRecipe.Translate();
            }
            
            if (template.IsReadable) {
                return LocTerms.ItemTypeReadable.Translate();
            }
            
            if (template.IsJewelry) {
                string type = template.EquipmentType.Name.Translate();
                return !string.IsNullOrEmpty(type) ? $"{LocTerms.ItemsTabJewelry.Translate().ColoredText(ARColor.MainAccent)} - {type}" : LocTerms.ItemsTabJewelry.Translate();
            }
            
            if (template.IsKey) {
                return LocTerms.ItemTypeKey.Translate();
            }

            if (template.Quality == ItemQuality.Garbage) {
                return LocTerms.ItemTypeGarbage.Translate();
            }
            
            if (template.Quality == ItemQuality.Quest) {
                return LocTerms.ItemTypeQuest.Translate();
            }

            return LocTerms.ItemTypeOther.Translate();
        }

        public static void CopyItemStats(Item fromItem, Item toItem) {
            var fromItemStats = fromItem.Element<ItemStats>();
            var toItemStats = toItem.Element<ItemStats>();

            toItemStats.Armor.SetTo(fromItemStats.Armor.ModifiedValue);
            toItemStats.Block.SetTo(fromItemStats.Block.ModifiedValue);
            toItemStats.BaseMinDmg.SetTo(fromItemStats.BaseMinDmg.ModifiedValue);
            toItemStats.BaseMaxDmg.SetTo(fromItemStats.BaseMaxDmg.ModifiedValue);
            toItem.WeightLevel.SetTo(fromItem.WeightLevel.ModifiedValue);
        }
        
        public static IEnumerable<ItemSpawningDataRuntime> GetItemSpawningDataFromLootTable(ILootTable lootTable, LocationSpec spec, object target) {
            try {
                return lootTable?.PopLoot(target).items;
            } catch (Exception e) {
                Log.Important?.Error($"Exception below happened on popping loot from LootInteract of LocationSpec ({spec.GetLocationId()})", spec);
                Debug.LogException(e, spec);
                return Enumerable.Empty<ItemSpawningDataRuntime>();
            }
        }
        
        public static bool IsUsedInLoadout(this Item item) {
            return Hero.Current.HeroItems.Loadouts.Any(loadout => loadout.Contains(item));
        }
        
        public static bool IsUsedInLoadout(this Item item, out HeroLoadout loadout) {
            loadout = Hero.Current.HeroItems.Loadouts.FirstOrDefault(loadout => loadout.Contains(item));
            return loadout != null;
        }
        
        public static bool IsUsedInLoadout(this Item item, int index) {
            return Hero.Current.HeroItems.LoadoutAt(index).Contains(item);
        }
        
        public static bool IsPrimaryInLoadout(this Item item) {
            return Hero.Current.HeroItems.Loadouts.Any(loadout => loadout.PrimaryItem == item);
        }

        public static void FillSimilarItemsDataList(this List<SimilarItemsData> similarItemsData, IEnumerable<Item> items) {
            similarItemsData.Clear();
            foreach (var itemData in items.Select(i => (ItemData)i)) {
                bool found = false;
                for (int i = 0; i < similarItemsData.Count; i++) {
                    if (similarItemsData[i].Template == itemData.item.Template) {
                        similarItemsData[i] += itemData;
                        found = true;
                    }
                }

                if (!found) {
                    similarItemsData.Add(new SimilarItemsData(itemData));
                }
            }
        }

        [UnityEngine.Scripting.Preserve]
        public static bool FindSimilarItems(this Item item, out SimilarItemsData similarItemsData) {
            similarItemsData = new SimilarItemsData((ItemData)item);

            IInventory itemInventory = item.Inventory;
            if (itemInventory == null) return false;
            
            bool found = false;
            foreach (var i in itemInventory.Items) {
                if (i != item && i.Template == item.Template) {
                    similarItemsData += (ItemData)i;
                    found = true;
                }
            }

            return found;
        }

        public static bool CheckSimilarItemsPossession(this IEnumerable<CountedItem> requiredItems, IEnumerable<SimilarItemsData> similarItems) {
            if (requiredItems == null) {
                return true;
            }
            
            var possessedItems = similarItems.ToList();
            
            foreach ((ItemTemplate requiredItemTemplate, int requiredQuantity) in requiredItems) {
                SimilarItemsData possessedItemData = possessedItems.FirstOrDefault(x => x.Template == requiredItemTemplate);
                if (possessedItemData.Quantity < requiredQuantity) {
                    return false;
                }
            }

            return true;
        }

        public static void DropHeroSimilarItems(this List<SimilarItemsData> similarItems, ItemTemplate itemTemplate, int quantity, int quantityMultiplier = 1) {
            SimilarItemsData possessedItemData = similarItems.First(x => x.Template == itemTemplate);
            int totalQuantity = quantity * quantityMultiplier;
            if (possessedItemData.Quantity < totalQuantity) {
                Log.Important?.Error($"Similar items insufficient quantity (given: {possessedItemData.Quantity}, required: {totalQuantity}). This should not happen!");
            }

            foreach (Item similarItem in possessedItemData.Items.Where(x => x is {HasBeenDiscarded: false}).OrderByDescending(x => !x.IsStashed)) {
                int quantityDifference = similarItem.Quantity - totalQuantity;
                similarItem.ChangeQuantity(-totalQuantity);
                if (quantityDifference < 0) {
                    totalQuantity = math.abs(quantityDifference);
                } else {
                    break;
                }
            }
        }
        
        public static void MergeStackableItems(List<ItemSpawningDataRuntime> items) {
            var stackableItems = new ListDictionary<(ItemTemplate, int), ItemSpawningDataRuntime>(items.Count);
            var result = new UnsafePinnableList<ItemSpawningDataRuntime>(items.Count);
            
            foreach (var item in items) {
                if (item.ItemTemplate.CanStack) {
                    if (stackableItems.TryGetValue((item.ItemTemplate, item.itemLvl), out var stackableItem)) {
                        stackableItem.quantity += item.quantity;
                    } else {
                        stackableItems.Add((item.ItemTemplate, item.itemLvl), item);
                        result.Add(item);
                    }
                } else {
                    result.Add(item);
                }
            }

            items.Clear();
            
            var span = result.AsSpan();
            for (int i = 0; i < span.Length; i++) {
                items.Add(span[i]);
            }
        }

        /// <summary>
        /// Simple use cases supported only.<br/>
        /// <b>Hero</b><br/>
        ///     - Adding items to hero.<br/>
        ///     - Removing items from hero<br/>
        ///     - Quantity supported.<br/>
        /// <b>Removing items from containers and shops</b><br/>
        ///     - Negative quantity means removal. otherwise ignored<br/>
        ///     - Only checks template
        /// </summary>
        public static void ApplyItemsToModifyOnLoad(List<ItemSpawningData> itemsToModifyOnLoad, object debugSource) {
            if (itemsToModifyOnLoad.IsNullOrEmpty()) {
                return;
            }
            
            // Hero changes
            var inv = Hero.Current?.Inventory;
            
            if (inv != null) {
                foreach (ItemSpawningData item in itemsToModifyOnLoad) {
                    item.ToRuntimeData(debugSource).ChangeQuantity(inv);
                }
            }
            
            var itemsToRemoveOnLoad = itemsToModifyOnLoad.Where(i => i.quantity < 0).Select(i => i.ItemTemplate(debugSource)).ToList();

            // Handle chests
            foreach (var searchAction in World.All<SearchAction>()) {
                itemsToRemoveOnLoad.ForEach(searchAction.RemoveItem);
            }

            // Handle shops
            using var worldShops = World.All<Shop>().GetManagedEnumerator();

            worldShops.SelectMany(s => s.Items.Where(i => itemsToRemoveOnLoad.Any(m => m == i.Template)))
                      .ToArray()
                      .ForEach(i => i.Discard());
            
            // Cleanup
            itemsToModifyOnLoad.Clear();
        }
        
        public static Color StatColor(int stat, int? otherStat) {
            if (otherStat.HasValue) {
                if (stat < otherStat.Value) {
                    return ColorWorse;
                }

                return stat == otherStat.Value ? ColorEqual : ColorBetter;
            }

            return ColorEqual;
        }
        
        public static Color StatColor(float stat, float? otherStat) {
            if (otherStat.HasValue) {
                if (stat < otherStat.Value) {
                    return ColorWorse;
                }

                return Math.Abs(stat - otherStat.Value) < 0.1f ? ColorEqual : ColorBetter;
            }

            return ColorEqual;
        }
    }
}
