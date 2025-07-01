using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.Items;
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

        CachedPriceProvider CachedPriceProvider => Element<CachedPriceProvider>();
        public override IPriceProvider PriceProvider => CachedPriceProvider;

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

        void AfterAttached(RelationEventData data) {
            OnAddItem((Item) data.to);
        }

        void OnAddItem(Item item) {
            CachedPriceProvider.Add(ParentModel, item);
        }
        void OnRemoveItem(IModel item) {
            CachedPriceProvider.Remove(item);
        }

        protected override Item StacksWith(Item item) {
            return CachedPriceProvider.GetItemToStackWith(ParentModel, item);
        }
    }
}