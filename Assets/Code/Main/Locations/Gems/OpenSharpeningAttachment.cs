using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Gems {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Rare, "Adds interaction that opens Upgrade UI.")]
    public class OpenSharpeningAttachment : MonoBehaviour, IAttachmentSpec {
        public Element SpawnElement() => new OpenSharpeningAction();

        public bool IsMine(Element element) => element is OpenSharpeningAction;
    }
}