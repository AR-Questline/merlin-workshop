using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Copies interactability from another location.")]
    public class CopyInteractabilityFromOtherLocationAttachment : MonoBehaviour, IAttachmentSpec {
        [field: SerializeField] public LocationSpec Location { get; private set; }
        
        public Element SpawnElement() {
            return new CopyInteractabilityFromOtherLocation();
        }

        public bool IsMine(Element element) {
            return element is CopyInteractabilityFromOtherLocation;
        }
    }
}