using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Utility;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Management {
    public class StorageItemsBatchHandler : ItemsBatchHandler {
        protected override string PromptName => LocTerms.UIStorageMoveAllIngredients.Translate();
        protected override KeyBindings KeyBinding => KeyBindings.UI.Items.DropItem;
        protected override bool ValidateItem(Item item) => item.IsCrafting && item.Quantity > 0;
    }
}