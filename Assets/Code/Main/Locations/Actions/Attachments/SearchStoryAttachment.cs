using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Stories;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    public class SearchStoryAttachment : MonoBehaviour, IAttachmentSpec {
        public SearchTriggersStory storyTrigger;
        public StoryBookmark story;
        [ShowIf("@storyTrigger == SearchTriggersStory.OnPickup")]
        public bool triggerOnce = true;
        
        public Element SpawnElement() {
            return new SearchActionStory();
        }

        public bool IsMine(Element element) {
            return element is SearchActionStory;
        }
        
        public enum SearchTriggersStory : byte {
            OnPickup,
            OnEmptied
        }
    }
}