using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Housing.HousingTable {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Housing - table for enabling decor mode.")]
    public class HousingDecorModeAttachment : MonoBehaviour, IAttachmentSpec {
        public Element SpawnElement() {
            return new HousingDecorModeAction();
        }

        public bool IsMine(Element element) {
            return element is HousingDecorModeAction;
        }
    }
}