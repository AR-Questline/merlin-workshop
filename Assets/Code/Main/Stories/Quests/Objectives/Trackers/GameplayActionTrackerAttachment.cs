using Awaken.TG.Main.Stories.Quests.Objectives.Specs;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Trackers {
    [AttachesTo(typeof(ObjectiveSpecBase), AttachmentCategory.Trackers, "Used to track gameplay related events.")]
    public class GameplayActionTrackerAttachment : BaseSimpleTrackerAttachment {
        [SerializeField] GameplayActionTracker.Action trackedAction;

        public GameplayActionTracker.Action TrackedAction => trackedAction;
        
        public override Element SpawnElement() => new GameplayActionTracker();
        public override bool IsMine(Element element) => element is GameplayActionTracker;
    }
}