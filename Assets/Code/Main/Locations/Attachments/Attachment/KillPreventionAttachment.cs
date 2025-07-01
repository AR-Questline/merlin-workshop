using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Rare, "When defeated, NPC will fall and recover after a while, unless Hero executes him.")]
    public class KillPreventionAttachment : MonoBehaviour, IAttachmentSpec {
        public Element SpawnElement() => new KillPreventionElement();

        public bool IsMine(Element element) => element is KillPreventionElement;
    }
}