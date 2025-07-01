using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Shops;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    public partial class CurrencyAsCraftingIngredient : Element<Item>, IRefreshedByAttachment<CurrencyAsCraftingIngredientAttachment> {
        public override ushort TypeForSerialization => SavedModels.CurrencyAsCraftingIngredient;

        CurrencyType _type;
        Stat _stat;
        
        public void InitFromAttachment(CurrencyAsCraftingIngredientAttachment spec, bool isRestored) {
            _type = spec.Type;
        }

        protected override void OnFullyInitialized() {
            var statType = _type == CurrencyType.Cobweb ? CurrencyStatType.Cobweb : CurrencyStatType.Wealth;
            _stat = Hero.Current.Stat(statType);
            Hero.Current.ListenTo(Stat.Events.StatChanged(statType), OnStatChanged, this);
            OnStatChanged(_stat);
            ParentModel.ListenTo(Item.Events.QuantityChanged, OnQuantityChanged, this);
        }

        void OnStatChanged(Stat stat) {
            int statValue = stat.ModifiedInt;
            if (statValue != ParentModel.Quantity) {
                ParentModel.SetQuantity(stat.ModifiedInt);
            }
        }
        
        void OnQuantityChanged(QuantityChangedData data) {
            int quantity = data.CurrentQuantity;
            if (quantity != _stat) {
                _stat.SetTo(quantity);
            }
        }
    }
}