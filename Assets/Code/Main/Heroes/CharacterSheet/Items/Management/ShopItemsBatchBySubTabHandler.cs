using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Utility;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Management {
    public class ShopItemsBatchBySubTabHandler : ItemsBatchBySubTabHandler {
        protected override string PromptName => LocTerms.Sell.Translate();
        protected override string PromptNameAll => LocTerms.SellAll.Translate();
        protected override KeyBindings KeyBinding => KeyBindings.UI.Generic.MarkAllAsSeen;

        protected override bool ValidateItem(Item item) => item.Quantity > 0 && !item.Template.IsTool && !item.IsLockpick && !item.IsUnidentified;
    }
}