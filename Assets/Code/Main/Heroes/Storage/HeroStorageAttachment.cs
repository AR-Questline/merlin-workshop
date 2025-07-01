using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Storage {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Location becomes Hero Storage chest.")]
    public class HeroStorageAttachment : MonoBehaviour, IAttachmentSpec {
        public Element SpawnElement() => new HeroStorageAction();
        public bool IsMine(Element element) => element is HeroStorageAction;
    }
}