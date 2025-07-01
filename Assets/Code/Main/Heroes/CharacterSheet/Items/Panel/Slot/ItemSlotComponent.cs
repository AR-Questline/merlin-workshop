using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot {
    public abstract class ItemSlotComponent : ComponentWithPartialVisibility<(Item item, View view, ItemDescriptorType type)> {
        protected override bool MiddleVisibilityOf((Item item, View view, ItemDescriptorType type) data) => data.item != null;
        
        protected sealed override void Refresh((Item item, View view, ItemDescriptorType type) data) => Refresh(data.item, data.view, data.type);
        protected virtual void Refresh(Item item, View view, ItemDescriptorType itemDescriptorType) { }
    }
}