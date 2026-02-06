using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    [RequireComponent(typeof(AliveLocationAttachment))]
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Rare, "Used by locations that are cut by sarras sickle.")]
    public class SarrasSickleInteractAttachment : LootInteractAttachment {
        public override Element SpawnElement() {
            return new SarrasSickleInteractAction();
        }

        public override bool IsMine(Element element) => element is SarrasSickleInteractAction;
    }
}