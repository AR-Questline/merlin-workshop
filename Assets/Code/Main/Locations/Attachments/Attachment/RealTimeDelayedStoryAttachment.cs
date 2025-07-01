using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Times;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Technical, "Starts story on restore after some real time.")]
    public class RealTimeDelayedStoryAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField] StoryBookmark story;
        [SerializeField, FoldoutGroup("Time Cycle")] ARTimeSpan actionDelay;
        [SerializeField] bool resetTimerOnFailedActivation;
        
        public StoryBookmark Story => story;
        public ARTimeSpan ActionDelay => actionDelay;
        public bool ResetTimerOnFailedActivation => resetTimerOnFailedActivation;

        // === Operations
        public Element SpawnElement() => new RealTimeDelayedStory();

        public bool IsMine(Element element) {
            return element is RealTimeDelayedStory;
        }
    }
}