using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.Utility;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Locations.Shops.Stocks {
    /// <summary>
    /// Shop stock which doesn't change on restock
    /// Used to store important items that can be bought by player only once (like quest item)
    /// </summary>
    public partial class UniqueStock : Stock {
        public override ushort TypeForSerialization => SavedModels.UniqueStock;

        protected override void OnInitialize() {
            foreach (var itemTemplateReference in ParentModel.Template.uniqueItems) {
                var template = itemTemplateReference.ItemTemplate(this);
                if (template == null) {
                    Log.Minor?.Info($"Item template not found for {itemTemplateReference} in {LogUtils.GetDebugName(this)}");
                    return;
                }
                AddItem(new Item(template, itemTemplateReference.quantity, itemTemplateReference.ItemLvl));
            }
        }

        protected override void OnRestore() {
            // Do nothing
        }
    }
}