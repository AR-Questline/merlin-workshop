using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Times;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Destroys the location after a certain time.")]
    public class DestroyAfterTimeAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField] ARTimeSpan destroyAfter;
        [SerializeField] bool destroyOnRestore;
        
        public ARTimeSpan DestroyAfter => destroyAfter;
        public bool DestroyOnRestore => destroyOnRestore;

        public Element SpawnElement() {
            return new DestroyAfterTime();
        }

        public bool IsMine(Element element) {
            return element is DestroyAfterTime;
        }
    }
}
