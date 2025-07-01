using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Adds a health bar to the location. Requires Alive Location Attachment.")]
    public class AliveLocationHealthBarAttachment : MonoBehaviour, IAttachmentSpec {
        public Element SpawnElement() {
            return new AliveLocationHealthBar();
        }

        public bool IsMine(Element element) {
            return element is AliveLocationHealthBar;
        }
    }
}