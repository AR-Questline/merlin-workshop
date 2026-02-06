using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Relations;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    public partial class ItemToCurrency : Element<Item>, IRefreshedByAttachment<ItemToCurrencySpec> {
        public override ushort TypeForSerialization => SavedModels.ItemToCurrency;

        [Saved] public CurrencyStatType stat;
        bool _showMultipliedByCurrencyStatMultiplier;
        float _multiplier;

        public bool ShowMultipliedByCurrencyStatMultiplier => _showMultipliedByCurrencyStatMultiplier;
        public float CurrencyMultiplier => stat == CurrencyStatType.Wealth ? Hero.Current.Stat(HeroMultStatType.WealthMultiplier).ModifiedValue : 1f;
        Item Item => ParentModel;

        public void InitFromAttachment(ItemToCurrencySpec spec, bool isRestored) {
            stat = spec.Currency;
            _multiplier = spec.multiplier;
            _showMultipliedByCurrencyStatMultiplier = spec.showMultipliedByCurrencyStatMultiplier;
        }

        protected override void OnInitialize() {
            ParentModel.ListenTo(IItemOwner.Relations.OwnedBy.Events.BeforeAttached, BeforeAttached, this);
        }

        void BeforeAttached(HookResult<IModel, RelationEventData> result) {
            if (result.Value.to is Hero hero) {
                hero.Stat(stat).IncreaseBy(Item.Quantity * _multiplier);
                Item.Discard();
                result.Prevent();
            }
        }
    }
}