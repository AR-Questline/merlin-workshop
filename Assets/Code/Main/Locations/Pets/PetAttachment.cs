using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Pets {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Defines a pet - location which follows hero.")]
    public class PetAttachment : MonoBehaviour, IAttachmentSpec {
        public Element SpawnElement() {
            return new PetElement();
        }

        public bool IsMine(Element element) {
            return element is PetElement;
        }
    }
}