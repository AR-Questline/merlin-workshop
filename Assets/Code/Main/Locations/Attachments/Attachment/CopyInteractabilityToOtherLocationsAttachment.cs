using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Copies interactability of this location to other locations.")]
    public class CopyInteractabilityToOtherLocationsAttachment : MonoBehaviour, IAttachmentSpec {
        [field: SerializeField] public LocationSpec[] Locations { get; private set; }
        
        public Element SpawnElement() {
            return new CopyInteractabilityToOtherLocations();
        }

        public bool IsMine(Element element) {
            return element is CopyInteractabilityToOtherLocations;
        }
    }
}