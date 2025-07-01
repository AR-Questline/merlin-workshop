using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Common, "Adds a signpost to the location. It cannot be interacted, but displays text when pointed to.")]
    public class SignPostAttachment :  MonoBehaviour, IAttachmentSpec {
        [LocStringCategory(Category.Interaction)]
        public LocString _text;
        
        public Element SpawnElement() {
            return new SignPostAction();
        }

        public bool IsMine(Element element) {
            return element is SignPostAction;
        }
    }
}