using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot {
    public class ItemInLoadoutComponent : ItemSlotComponent {
        protected override void Refresh(Item item, View view, ItemDescriptorType itemDescriptorType) {
            SetInternalVisibility(item.IsEquipped || item.IsUsedInLoadout());
        }
    }
}