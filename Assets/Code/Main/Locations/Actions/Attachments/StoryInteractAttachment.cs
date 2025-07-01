using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Rare, "Starts story on location interaction.")]
    public class StoryInteractAttachment : MonoBehaviour, IAttachmentSpec {
        public bool showInteractionInfoIfBlocked = true;
        public StoryBookmark storyBookmark;

        public Element SpawnElement() {
            return new StoryInteractAction();
        }

        public bool IsMine(Element element) => element is StoryInteractAction;
    }
}