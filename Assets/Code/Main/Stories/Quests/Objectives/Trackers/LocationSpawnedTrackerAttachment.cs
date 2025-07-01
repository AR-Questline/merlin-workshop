using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Quests.Objectives.Specs;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Trackers {
    [AttachesTo(typeof(ObjectiveSpecBase), AttachmentCategory.Trackers, "Used to track if location is spawned.")]
    public class LocationSpawnedTrackerAttachment : BaseSimpleTrackerAttachment {
        [field: SerializeField] public LocationReference Location { get; private set; }

        public override Element SpawnElement() {
            return new LocationSpawnedTracker();
        }

        public override bool IsMine(Element element) => element is LocationSpawnedTracker;
    }
}