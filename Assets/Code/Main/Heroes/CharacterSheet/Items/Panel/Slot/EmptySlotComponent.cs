using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot {
    public class EmptySlotComponent : ItemSlotComponent {
        protected override bool MiddleVisibilityOf((Item, View, ItemDescriptorType) data) => data.Item1 == null;

        void Start() {
            SetInternalVisibility(true);
        }
    }
}