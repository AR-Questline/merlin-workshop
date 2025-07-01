using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Common, "Reads a story when interacted with.")]
    public class ReadAttachment : MonoBehaviour, IAttachmentSpec {
        [TemplateType(typeof(StoryGraph)), SerializeField]
        TemplateReference readable;
        [SerializeField] bool hasImage;

        public TemplateReference StoryRef => readable;
        public StoryBookmark Readable => StoryBookmark.ToInitialChapter(readable);
        public bool HasImage => hasImage;
        
        public Element SpawnElement() {
            return new ReadAction();
        }
        
        public bool IsMine(Element element) {
            return element is ReadAction;
        }
    }
}