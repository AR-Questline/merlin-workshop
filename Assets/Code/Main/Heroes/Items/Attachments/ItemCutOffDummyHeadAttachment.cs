using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    [AttachesTo(typeof(ItemTemplate), AttachmentCategory.ExtraCustom, "Marks item as cut-off head.")]
    public class ItemCutOffDummyHeadAttachment : MonoBehaviour, IAttachmentSpec {
        public Element SpawnElement() {
            return new ItemCutOffDummyHead();
        }

        public bool IsMine(Element element) {
            return element is ItemCutOffDummyHead;
        }
    }
}