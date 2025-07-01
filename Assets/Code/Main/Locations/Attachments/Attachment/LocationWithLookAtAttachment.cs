using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Rare, "Attaches look at target to location (used in story).")]
    public class LocationWithLookAtAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField] Vector3 lookAtTargetOffset;

        public Vector3 LookAtTargetOffset => lookAtTargetOffset;

        public Element SpawnElement() {
            return new LocationWithLookAt();
        }

        public bool IsMine(Element element) => element is LocationWithLookAt;
    }
}