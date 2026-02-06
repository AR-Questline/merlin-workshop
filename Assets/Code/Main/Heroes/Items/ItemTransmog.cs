using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Transmogrify;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;

namespace Awaken.TG.Main.Heroes.Items {
    public partial class ItemTransmog : Element<Item> {
        public override ushort TypeForSerialization => SavedModels.ItemTransmog;

        [Saved] public ItemTemplate Template { get; private set; }
        public bool IsTransmogrified => Template != null;
        public bool HasPreview => TransmogPreview != null;
        public Item TransmogPreview { get; private set; }

        ItemEquip PreviewItemEquip => TransmogPreview?.Element<ItemEquip>();
        
        public ItemTransmog() {
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<TransmogrifyUI>(), this, Clear);
        }
        
        public ItemTransmog(ItemTemplate template) {
            Template = template;
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<TransmogrifyUI>(), this, Clear);
        }

        public void Equip() {
            if (TransmogPreview == null) {
                ParentModel.VisualHeroEquip();
            } else {
                PreviewItemEquip.VisualHeroEquip();
            }
        }
        
        public void Unequip() {
            if (TransmogPreview == null) {
                ParentModel.VisualHeroUnequip();
            } else {
                PreviewItemEquip.VisualHeroUnequip();
            }
        }
        
        public void EquipPreview(Item item) {
            Unequip();
            TransmogPreview = item.Template != Template ? item : null;
            Equip();
        }
        
        public void ConfirmTransmog() {
            Template = TransmogPreview.Template;
            TransmogPreview = null;
        }

        public void RemoveTransmog() {
            Unequip();
            Template = null;
            Equip();
        }
        
        public void RemovePreview() {
            Unequip();
            TransmogPreview = null;
            Equip();
        }

        void Clear() {
            if (IsTransmogrified == false) {
                Discard();
            } else {
                TransmogPreview = null;
            }
        }
    }
}
