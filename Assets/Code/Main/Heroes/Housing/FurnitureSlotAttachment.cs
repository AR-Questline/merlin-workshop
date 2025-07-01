using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes.Housing {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Housing - Furniture Slot.")]
    public class FurnitureSlotAttachment : FurnitureSlotAttachmentBase {
        public override Element SpawnElement() {
            return new FurnitureSlot();
        }

        public override bool IsMine(Element element) {
            return element is FurnitureSlot;
        }
    }
}