using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Plays hero custom animation on interaction.")]
    public class HeroAnimationOnActionAttachment : MonoBehaviour, IAttachmentSpec {
        public Element SpawnElement() {
            return new HeroAnimationOnAction();
        }

        public bool IsMine(Element element) {
            return element is HeroAnimationOnAction;
        }
    }
}