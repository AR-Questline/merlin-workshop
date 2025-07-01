using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    [AttachesTo(typeof(ItemTemplate), AttachmentCategory.Rare, "For items that can be used to pick locks.")]
    public class LockpickAttachment : MonoBehaviour, IAttachmentSpec {
        public Element SpawnElement() {
            return new Lockpick();
        }

        public bool IsMine(Element element) => element is Lockpick;
    }
}