using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.Main.Utility.VFX;
using Awaken.TG.MVC.Elements;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Technical, "Used to set parameters in activate fast travel element that are persistent between sessions")]
    public class ActivateFastTravelAttachment : MonoBehaviour, IAttachmentSpec {
        public GameObject go;
        public Light lightRef;
        public VisualEffect vfxRef;

        public Element SpawnElement() => new ActivateFastTravelElement();
        public bool IsMine(Element element) => element is ActivateFastTravelElement;
    }
}