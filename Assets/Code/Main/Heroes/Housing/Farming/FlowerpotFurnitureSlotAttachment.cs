using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes.Housing.Farming {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Housing - Flowerpot.")]
    public class FlowerpotFurnitureSlotAttachment : FurnitureSlotAttachmentBase {
        public override Element SpawnElement() {
            return new FlowerpotSlot();
        }

        public override bool IsMine(Element element) {
            return element is FlowerpotSlot;
        }
    }
}