using Awaken.Utility;
using System.Linq;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes;

namespace Awaken.TG.Main.Locations.Actions {
    public partial class ItemRequirement : Element<Location>, IRefreshedByAttachment<ItemRequirementAttachment> {
        public override ushort TypeForSerialization => SavedModels.ItemRequirement;

        [Saved] bool _consumed;
        ItemTemplate _itemTemplate;

        ItemRequirementData Data { get; set; }
        public ItemTemplate ItemTemplate => _itemTemplate ??= Data.itemReference.Get<ItemTemplate>();
        
        public bool HasItem { get; private set; }
        public bool ShowInfo => !(Data.requireOnce && _consumed);
        public string Info { get; private set; }

        public void InitFromAttachment(ItemRequirementAttachment spec, bool isRestored) {
            Data = spec.itemRequirementData;
        }

        public void RefreshValues(Hero hero) {
            if (Data.requireOnce && _consumed) {
                return;
            }

            Item item = hero.Inventory.Items.FirstOrDefault(CheckItem);
            HasItem = item?.Quantity >= Data.quantity;
            string itemText;
            if (Data.quantity > 1) {
                itemText = $"{ItemTemplate.itemName} x{Data.quantity.ToString()}";
                if (item) {
                    itemText = $"{itemText} ({item?.Quantity.ToString()})";
                }
            } else {
                itemText = ItemTemplate.itemName.ToString();
            }

            Info = HasItem
                ? LocTerms.UseSomething.Translate(itemText)
                : LocTerms.ToolRequired.Translate(itemText);
        }

        public bool ShouldDisableInteraction(Hero hero) {
            RefreshValues(hero);
            return Data.hideInteractionUntilMet && !HasItem;
        }

        public bool ConsumeItem(Hero hero) {
            if (Data.requireOnce && _consumed) {
                return true;
            }

            Item item = hero.Inventory.Items.FirstOrDefault(CheckItem);
            if (item == null || item.Quantity < Data.quantity) {
                return false;
            }

            if (Data.consumeOnUse) {
                item.ChangeQuantity(-Data.quantity);
            }
            
            _consumed = true;

            return true;
        }

        bool CheckItem(Item item) {
            return item.Template.InheritsFrom(ItemTemplate);
        }
    }
}