using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Destroys the location after a certain distance from Hero.")]
    public class DestroyOnDistanceAttachment : MonoBehaviour, IAttachmentSpec {
        [Range(10, 250)] public int destroyDistance = 100;

        public Element SpawnElement() {
            return new DestroyOnDistance();
        }

        public bool IsMine(Element element) {
            return element is DestroyOnDistance;
        }
    }
}
