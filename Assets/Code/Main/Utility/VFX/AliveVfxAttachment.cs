using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Utility.VFX {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Technical, "Used to override default VFX for IAlive.")]
    public class AliveVfxAttachment : MonoBehaviour, IAttachmentSpec {
        public ItemVfxContainerWrapper vfxWrapper;

        public Element SpawnElement() {
            return new AliveVfx();
        }

        public bool IsMine(Element element) {
            return element is AliveVfx;
        }
    }
}
