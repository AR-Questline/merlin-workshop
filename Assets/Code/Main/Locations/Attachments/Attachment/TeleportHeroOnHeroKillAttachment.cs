using Awaken.TG.Assets;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Teleports hero to another scene when hero gets killed.")]
    public class TeleportHeroOnHeroKillAttachment : MonoBehaviour, IAttachmentSpec {
        public SceneReference targetScene;
        public string indexTag;
        
        public Element SpawnElement() {
            return new TeleportHeroOnHeroKill();
        }

        public bool IsMine(Element element) {
            return element is TeleportHeroOnHeroKill;
        }
    }
}