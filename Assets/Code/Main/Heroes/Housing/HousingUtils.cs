using System;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Housing.Farming;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Localization;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Heroes.Housing {
    public static class HousingUtils {
        public static bool HasRequiredFunds(HousingUnlockRequirement requirement) {
            return Hero.Current.MerchantStats.Wealth >= requirement.unlockPrice;
        }
        
        public static bool HasRequiredResources(HousingUnlockRequirement requirement) {
            Hero hero = Hero.Current;
            bool hasEnoughItems = requirement.requiredResources.All(resource => {
                ItemTemplate itemTemplate = resource.ItemTemplate(hero);
                int heroItemQuantity = hero.Inventory.ContainedItems()
                    .Where(item => item.Template == itemTemplate)
                    .Sum(item => item.Quantity);
                return heroItemQuantity >= resource.quantity;
            });
            
            return hasEnoughItems;
        }
        
        public static void UseRequiredResources(HousingUnlockRequirement requirement) {
            Hero hero = Hero.Current;
            hero.MerchantStats.Wealth.DecreaseBy(requirement.unlockPrice);
            foreach (ItemSpawningData resource in requirement.requiredResources) {
                resource.ItemTemplate(hero).ChangeQuantity(hero.Inventory, -resource.quantity);
            }
        }
        
        public static bool HasRequiredFurnitureItem(HousingUnlockRequirement requirement) {
            if (requirement.requiredFurnitureItem.itemTemplateReference == null) {
                return false;
            }
            
            Hero hero = Hero.Current;
            ItemTemplate itemTemplate = requirement.requiredFurnitureItem.ItemTemplate(hero);
            int heroItemQuantity = hero.Inventory.ContainedItems()
                .Where(item => item.Template == itemTemplate)
                .Sum(item => item.Quantity);

            return heroItemQuantity >= requirement.requiredFurnitureItem.quantity;
        }
        
        public static void UseRequiredFurnitureItem(HousingUnlockRequirement requirement) {
            Hero hero = Hero.Current;
            requirement.requiredFurnitureItem.ItemTemplate(hero).ChangeQuantity(hero.Inventory, -requirement.requiredFurnitureItem.quantity);
        }
        
        public static string GetPlantSizeName(PlantSize plantSize) {
            return plantSize switch {
                PlantSize.Small => LocTerms.FarmingSizeSmall.Translate(),
                PlantSize.Large => LocTerms.FarmingSizeLarge.Translate(),
                _ => LocTerms.None.Translate()
            };
        }
        
        public static string GetGridPlantSize(PlantSize plantSize) {
            return plantSize switch {
                PlantSize.Small => "1x4",
                PlantSize.Large => "4x4",
                _ => throw new ArgumentOutOfRangeException(nameof(plantSize), plantSize, "Plant size not supported.")
            };
        }
        
        public static string GetPlantStateName(PlantState plantState) {
            return plantState switch {
                PlantState.ReadyForPlanting => LocTerms.FarmingPlantStateReadyForPlanting.Translate(),
                PlantState.Growing => LocTerms.FarmingPlantStateGrowing.Translate(),
                PlantState.FullyGrown => LocTerms.FarmingPlantStateFullyGrown.Translate(),
                PlantState.Blocked => LocTerms.FarmingPlantStateBlocked.Translate(),
                _ => LocTerms.None.Translate()
            };
        }
    }
}