using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Technical, "Adds dialogue to the pet location.")]
    public class PetTalkAttachment : DialogueAttachment {
        public override Element SpawnElement() {
            return new PetTalkAction();
        }

        public override bool IsMine(Element element) {
            return element is PetTalkAction;
        }
    }
}