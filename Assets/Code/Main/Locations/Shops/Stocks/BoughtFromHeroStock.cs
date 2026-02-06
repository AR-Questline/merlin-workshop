using System.Collections.Generic;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Locations.Shops.Prices;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Relations;
using Awaken.Utility;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Locations.Shops.Stocks {
    /// <summary>
    /// Shop stock which removes all of its items on restock
    /// Used to store items bought from hero that will disappear from the shop after some time
    /// </summary>
    public partial class BoughtFromHeroStock : Stock {
        public override ushort TypeForSerialization => SavedModels.BoughtFromHeroStock;

        bool _isCompressingOrDecompressing;
        
        CachedPriceProvider CachedPriceProvider => Element<CachedPriceProvider>();
        public override IPriceProvider PriceProvider => CachedPriceProvider;
        protected override List<ItemSpawningDataRuntime> CompressedItems => null;

        protected override void OnInitialize() {
            AddElement(new CachedPriceProvider());
            this.ListenTo(Relations.Stocks.Events.AfterAttached, AfterAttached, this);
            this.ListenTo(Relations.Stocks.Events.AfterDetached, data => OnRemoveItem((Item) data.to), this);
        }

        protected override void OnRestore() {
            this.ListenTo(Relations.Stocks.Events.AfterAttached, AfterAttached, this);
            this.ListenTo(Relations.Stocks.Events.AfterDetached, data => OnRemoveItem(data.to), this);
        }

        protected override void OnFullyInitialized() {
            DelayedCleanup().Forget();
        }
        
        async UniTaskVoid DelayedCleanup() {
            if (!await AsyncUtil.DelayFrame(this)) {
                return;
            }
            CachedPriceProvider.CleanupOldItems();
        }
        
        protected override void RemoveCompressedItemsOfTemplate(ItemTemplate template) {
            CachedPriceProvider.RemoveCompressedItemsOfTemplate(template);
        }

        void AfterAttached(RelationEventData data) {
            OnAddItem((Item) data.to);
        }

        void OnAddItem(Item item) {
            if (_isCompressingOrDecompressing) {
                return;
            }
            CachedPriceProvider.Add(ParentModel, item);
        }
        
        void OnRemoveItem(IModel item) {
            if (_isCompressingOrDecompressing) {
                return;
            }
            CachedPriceProvider.Remove(item);
        }

        protected override Item StacksWith(Item item) {
            return CachedPriceProvider.GetItemToStackWith(ParentModel, item);
        }
        
        protected override void CompressAllItems() {
            _isCompressed = false;
            _isCompressingOrDecompressing = true;
            CachedPriceProvider.CompressAllItems();
            for (int i = RelatedItems.Count - 1; i >= 0; i--) {
                RelatedItems[i].Discard();
            }
            StatTweak.CleanupObsoleteStatTweaks();
            _isCompressingOrDecompressing = false;
            _isCompressed = true;

        }

        protected override void CreateAllItems() {
            _isCompressed = false;
            _isCompressingOrDecompressing = true;
            var newItems = new List<Item>();
            CachedPriceProvider.CreateAllItems(newItems);
            foreach (var item in newItems) {
                RegisterCreatedItem(item);
            }
            _isCompressingOrDecompressing = false;
        }
    }
}