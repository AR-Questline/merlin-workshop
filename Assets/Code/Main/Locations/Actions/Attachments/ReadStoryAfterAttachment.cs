using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Technical, "Triggers a story when read action is finished.")]
    public class ReadStoryAfterAttachment : MonoBehaviour, IAttachmentSpec {
        public StoryBookmark bookmark;
        
        public Element SpawnElement() {
            return new ReadActionStoryAfter();
        }
        
        public bool IsMine(Element element) {
            return element is ReadActionStoryAfter;
        }
    }
}