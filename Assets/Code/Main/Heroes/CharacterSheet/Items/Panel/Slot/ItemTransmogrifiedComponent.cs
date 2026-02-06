using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot {
    public class ItemTransmogrifiedComponent : ItemSlotComponent {
        protected override void Refresh(Item item, View view, ItemDescriptorType itemDescriptorType) {
            SetInternalVisibility(item.IsTransmogrified);
        }
    }
}