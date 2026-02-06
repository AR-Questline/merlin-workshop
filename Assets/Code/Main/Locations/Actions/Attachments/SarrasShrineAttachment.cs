using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    public class SarrasShrineAttachment : MonoBehaviour, IAttachmentSpec {
        [TemplateType(typeof(StatusTemplate))]
        public TemplateReference statusReference;
        public StoryBookmark bookmark;
        
        public Element SpawnElement() {
            return new SarrasShrineAction();
        }
        
        public bool IsMine(Element element) {
            return element is SarrasShrineAction;
        }
    }
}