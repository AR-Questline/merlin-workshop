using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Quests.Objectives.Specs;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Trackers {
    [AttachesTo(typeof(ObjectiveSpecBase), AttachmentCategory.Trackers, "Used to track if NPC is alive.")]
    public class AliveNPCTrackerAttachment : BaseSimpleTrackerAttachment {
        public LocationReference npcLocation;
        
        public override Element SpawnElement() {
            return new AliveNPCTracker();
        }

        public override bool IsMine(Element element) => element is AliveNPCTracker;
    }
}