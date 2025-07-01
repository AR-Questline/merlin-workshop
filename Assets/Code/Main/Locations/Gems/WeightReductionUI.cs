using System;
using System.Collections.Generic;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Tooltips.Views;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Shops;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Gems {
    public partial class WeightReductionUI : SharpeningUI<VWeightReductionUI> {
        public override string ContextTitle => LocTerms.ArmorWeightReductionTab.Translate();
        public override IEnumerable<CountedItem> Ingredients => ClickedItem == null ? null : GetIngredientsFromConfig(ItemUpgradeConfigConfig, ClickedItem.WeightLevel.ModifiedInt);
        public override int ServiceCost {
            get {
                int price = ItemUpgradeConfigConfig?.GetPrice(CurrencyType.Money, ClickedItem.WeightLevel.ModifiedInt) ?? 0;
                return (int)((1 - Hero.HeroStats.UpgradeDiscount.ModifiedValue) * price);
            }
        }
        public override int CobwebServiceCost {
            get {
                int price = ItemUpgradeConfigConfig?.GetPrice(CurrencyType.Cobweb, ClickedItem.WeightLevel.ModifiedInt) ?? 0;
                return (int)((1 - Hero.HeroStats.UpgradeDiscount.ModifiedValue) * price);
            }
        }
        protected override ItemUpgradeConfigData ItemUpgradeConfigConfig => ClickedItem != null ? GetWeightReductionConfig(ClickedItem) : null;
        protected override int CostMultiplier => Mathf.Max(_upgradedItem?.WeightLevel.ModifiedInt ?? 1, 1);
        protected override Type TooltipType => typeof(VWeightReductionTooltipSystemUI);
        protected override Func<Item, bool> ItemFilter => item => GetSharpeningConfig(item) != null &&
                                                                  !item.HiddenOnUI && item.IsArmor && item.Weight > 0f;
        protected override string GemActionName => LocTerms.RelicsPromptReduceWeight.Translate();

        protected override ItemUpgradeConfigData GetSharpeningConfig(Item item) {
            return item?.Template.WeightReductionConfig ?? GetWeightReductionConfig(item);
        }

        protected override bool CanRunAction(Item item) {
            return base.CanRunAction(item) && item.Weight > 0f;
        }

        protected override void GemAction() {
            if (!ClickedItem) {
                return;
            }

            PayForService();
            ClickedItem.WeightLevel.IncreaseBy(1);
            var heroItems = Hero.HeroItems;
            heroItems.Trigger(ICharacterInventory.Events.AfterEquipmentChanged, heroItems);
            
            ReequipIfNecessary();
            Refresh();
        }

        protected override void CreateUpgradedItemPreview(Item item) {
            _upgradedItem?.Discard();
            _upgradedItem = World.Add(new Item(item.Template, 1, item.Level.ModifiedInt));
            ItemUtils.CopyItemStats(item, _upgradedItem);
            _upgradedItem.WeightLevel.IncreaseBy(1);
        }

        static ItemUpgradeConfigData GetWeightReductionConfig(Item item) {
            return item?.Template.WeightReductionConfig;
        }
    }
}