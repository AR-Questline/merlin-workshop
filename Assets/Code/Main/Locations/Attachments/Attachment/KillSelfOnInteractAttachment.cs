using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Technical, "Kill alive on this location on interact")]
    public class KillSelfOnInteractAttachment : MonoBehaviour, IAttachmentSpec {
        public bool makeInactive = true;
        
        public Element SpawnElement() => new KillSelfOnInteract();
        public bool IsMine(Element element) => element is KillSelfOnInteract;
    }
}