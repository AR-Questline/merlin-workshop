using Awaken.TG.Main.Stories.Quests.Objectives.Specs;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Trackers {
    [AttachesTo(typeof(ObjectiveSpecBase), AttachmentCategory.Trackers, "Used to track if player has rested.")]
    public class RestTrackerAttachment : BaseSimpleTrackerAttachment {
        [field: SerializeField] public bool TrackOnlyWhenActive { get; private set; } = true;

        public override Element SpawnElement() {
            return new RestTracker();
        }

        public override bool IsMine(Element element) => element is RestTracker;
    }
}