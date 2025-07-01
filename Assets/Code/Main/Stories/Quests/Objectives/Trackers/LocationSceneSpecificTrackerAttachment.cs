using System;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Quests.Objectives.Specs;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Trackers {
    [AttachesTo(typeof(ObjectiveSpecBase), AttachmentCategory.Trackers, "Used to track locations on a specific scene")]
    public class LocationSceneSpecificTrackerAttachment : BaseSceneSpecificTrackerAttachment {
        public LocationReference[] locationsToTrack = Array.Empty<LocationReference>();
        [InfoBox("If true, locations that are dead won't be counted and death will decrease counter. " +
                 "\nIt will still work on discard for locations that can't die")]
        public bool countOnlyAlive = true;
        [InfoBox("Amount of locations to complete the tracker." +
                 "\nIt needs to be at least once above 0 to start working")]
        public int amountToComplete = 0;
        public CountType countType = CountType.LesserOrEqual;

        protected override string DisplayPatternDescription => base.DisplayPatternDescription +
                                                               "\n{cur} - current count of locations (f.e. 2 enemies are alive or 2 plants are planted = 2)" +
                                                               "\n{max} - amount to complete {f.e. 3 plants need to be planted = 3";

        public override Element SpawnElement() {
            return new LocationSceneSpecificTracker();
        }

        public override bool IsMine(Element element) => element is LocationSceneSpecificTracker;

        public enum CountType : byte {
            LesserOrEqual = 0,
            Equal = 1,
            GreaterOrEqual = 2,
        }
    }
}