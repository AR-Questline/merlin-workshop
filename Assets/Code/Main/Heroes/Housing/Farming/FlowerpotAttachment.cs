using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Housing.Farming {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Allows planting seeds in flowerpot location.")]
    public class FlowerpotAttachment : MonoBehaviour, IAttachmentSpec {
        public Element SpawnElement() {
            return new Flowerpot();
        }

        public bool IsMine(Element element) {
            return element is Flowerpot;
        }
    }
}