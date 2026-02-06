using System.Collections.Generic;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Management;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.EmptyContent;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Locations.Shops.Tabs {
    public partial class ShopSellUI : ShopHeroUI {
        ShopItemsBatchHandler _sellGarbageHandler;
        ShopItemsBatchBySubTabHandler _sellBySubTabHandler;
        
        public override IEnumerable<Item> Items => Hero.HeroItems.SellableInventory(Shop.AdditionalSellCondition);

        protected override void AfterViewSpawned(VShopVendorBaseUI view) {
            base.AfterViewSpawned(view);
            
            _sellGarbageHandler = AddElement(new ShopItemsBatchHandler());
            _sellGarbageHandler.Initialize(Prompts, ItemsUI, item => TryTrade(item, item.Quantity, false));
            
            _sellBySubTabHandler = AddElement(new ShopItemsBatchBySubTabHandler());
            _sellBySubTabHandler.Initialize(Prompts, ItemsUI, item => TryTrade(item, item.Quantity, false), ParentModel.DoublePromptsHost);
        }
        
        protected override void SetupEmptyInfoLabels() {
            View<IEmptyInfo>().EmptyInfoView.SetupLabels(LocTerms.EmptyShopSellInfo.Translate(), LocTerms.EmptyShopSellDesc.Translate());
        }
    }
}