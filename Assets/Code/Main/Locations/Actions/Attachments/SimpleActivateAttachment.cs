using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.Main.Utility.VFX;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Technical, "Used to set parameters in simple activate element that are persistent between sessions.")]
    public class SimpleActivateAttachment : MonoBehaviour, IAttachmentSpec {
        public GameObject[] targetGameObjects = null;

        public Element SpawnElement() => new SimpleActivateElement();
        public bool IsMine(Element element) => element is SimpleActivateElement;
    }
}