using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    [AttachesTo(typeof(ItemTemplate), AttachmentCategory.ExtraCustom, "On item pickup discards owner location.")]
    public class ItemDiscardDummyLocationAttachment : MonoBehaviour, IAttachmentSpec {
        public bool dropRemainingItems;
        
        public Element SpawnElement() {
            return new ItemDiscardDummyLocation();
        }

        public bool IsMine(Element element) {
            return element is ItemDiscardDummyLocation;
        }
    }
}