using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot {
    public class ItemBackgroundComponent : ItemSlotComponent {
        protected override bool MiddleVisibilityOf((Item, View, ItemDescriptorType) data) => true;
        
        void Start() {
            SetInternalVisibility(true);
        }
    }
}