using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes.Tags;
using System;
using Awaken.TG.Main.Stories.Quests.Objectives.Specs;
using Awaken.TG.Main.Templates.Attachments;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Trackers {
    [AttachesTo(typeof(ObjectiveSpecBase), AttachmentCategory.Trackers, "Used to track if flag is set.")]
    public class FlagTrackerAttachment : BaseSimpleTrackerAttachment {
        [Tags(TagsCategory.Flag)]
        public string[] flags = Array.Empty<string>();
        
        public override Element SpawnElement() {
            return new FlagTracker();
        }

        public override bool IsMine(Element element) => element is FlagTracker;
    }
}