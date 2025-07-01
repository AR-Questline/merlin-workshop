using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments.Interfaces;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Shops.Prices;
using Awaken.TG.Main.Locations.Shops.Stocks;
using Awaken.TG.Main.Locations.Shops.UI;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Times;
using Newtonsoft.Json;
using Unity.Mathematics;

namespace Awaken.TG.Main.Locations.Shops {
    public partial class Shop : Element<Location>, IMerchant, IInventory, IWithDomainMovedCallback, IRefreshedByAttachment<ShopAttachment> {
        public override ushort TypeForSerialization => SavedModels.Shop;

        const int MaxRestockCount = 5;
        public static readonly ARTimeSpan RestockInterval = new() { Days = 1 };

        // === Fields

        [Saved] bool _isRestockDistanceMet;
        [Saved] ARDateTime _lastRestockTime;
        [Saved] public ShopTemplate Template { get; private set; }

        public MerchantStats MerchantStats => Element<MerchantStats>();

        public Func<Item, bool> AdditionalSellCondition => Template.shopCantBuyItemsWithTheseTags.Length > 0 ? CanItemBeSoldToTheShop : null;
        bool CanItemBeSoldToTheShop(Item item) => !TagUtils.HasAnyTag(item.Tags, Template.shopCantBuyItemsWithTheseTags);
        
        // === Getters

        static ARDateTime WeatherTime => World.Any<GameRealTime>().WeatherTime;
        bool IsRestockDue => WeatherTime - _lastRestockTime > RestockInterval;
        bool CanRestock => _isRestockDistanceMet && IsRestockDue;
        public BoughtFromHeroStock BoughtFromHeroStock => Element<BoughtFromHeroStock>();

        // === IInventory
        public IInventory Inventory => this;
        public ICharacter Character => null;
        public IEquipTarget EquipTarget => null;
        [UnityEngine.Scripting.Preserve] public IEnumerable<Item> InventoryVisibleOnUI => Items.Where(item => !item.HiddenOnUI);
        public IEnumerable<Item> Items => Elements<Stock>().GetManagedEnumerator().SelectMany(s => s.Items);
        public Item Add(Item item, bool allowStacking = true) => BoughtFromHeroStock.AddItem(item, allowStacking);

        public void Remove(Item item, bool discard = true) => Elements<Stock>().First(s => s.Items.Contains(item)).RemoveItem(item, discard);
        
        public bool CanBeTheft => false;

        // === LifeCycle
        
        public void InitFromAttachment(ShopAttachment spec, bool isRestored) {
            Template = spec.ShopTemplate;
            if (!isRestored) {
                MerchantStats.Create(this);
            }
        }
        
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public Shop() {}

        public new static class Events {
            public static readonly Event<Shop, Shop> ShopOpened = new(nameof(ShopOpened));
            public static readonly Event<Shop, Shop> ShopClosed = new(nameof(ShopClosed));
        }

        protected override void OnInitialize() {
            AddElement(new UniqueStock());
            AddElement(new BoughtFromHeroStock());
            foreach (var stock in Template.restockableItems) {
                AddElement(new RestockableStock(stock));
            }
            _isRestockDistanceMet = true;
        }

        protected override void OnRestore() { }

        protected override void OnFullyInitialized() {
            ParentModel.ListenTo(ICullingSystemRegistreeModel.Events.DistanceBandChanged, RefreshDistanceBand, this);
            ParentModel.AfterFullyInitialized(() =>
                ParentModel.TryGetElement<IAlive>()?.ListenTo(IAlive.Events.BeforeDeath, OnDeath, this));
        }

        void OnDeath() {
            foreach (Item item in Items.ToList()) {
                item.Discard();
            }
            Discard();
        }

        // === Show UI
        
        public ShopUI OpenShop() {
            this.Trigger(Events.ShopOpened, this);
            Restock();
            ShopUI shopUI = new();
            AddElement(shopUI);
            shopUI.ListenTo(Model.Events.AfterDiscarded, OnShopClosed, this);
            return shopUI;
        }

        void OnShopClosed(Model _) {
            this.Trigger(Events.ShopClosed, this);
        }

        // == Operations

        public void Restock(bool force = false) {
            if (CanRestock || force) {
                TimeSpan time = WeatherTime - _lastRestockTime;
                int restockCount = (int) math.min(time.Ticks / RestockInterval.Ticks, MaxRestockCount);

                PooledList<Stock>.Get(out var pooledStocks);
                var stocks = pooledStocks.value;
                stocks.Clear();
                Elements<Stock>().FillList(stocks);

                while (restockCount > 0) {
                    for (var index = stocks.Count - 1; index >= 0; index--) {
                        Stock s = stocks[index];
                        s.Restock();
                        if (s.RestockOnce) stocks.RemoveAt(index);
                    }

                    RestockCurrency(MerchantStats.Wealth, Template.maxWealth, Template.restockWealthGain.RandomPick());
                    restockCount--;
                }

                pooledStocks.Release();

                _lastRestockTime = WeatherTime;
                _isRestockDistanceMet = false;
            }
        }

        void RestockCurrency(CurrencyStat currency, int max, int add) {
            currency.SetTo(float.MaxValue);
            // Shop currency limit disabled
            // if (currency < max) {
            //     currency.IncreaseBy(add);
            // }
        }

        public IPriceProvider SellPriceProviderFor(Item item) {
            return item.RelatedValue(Stock.Relations.StockedBy).Get().PriceProvider;
        }

        public void DomainMoved(Domain newDomain) {
            foreach (var item in RelatedList(IItemOwner.Relations.Owns)) {
                item.MoveToDomain(newDomain);
            }
        }

        void RefreshDistanceBand(int band) {
            if (LocationCullingGroup.InRestockBand(band)) {
                _isRestockDistanceMet = true;
            }
        }

        // == IWithStats

        public Stat Stat(StatType statType) => statType is MerchantStatType stats ? stats.RetrieveFrom(this) : null;
    }
}
