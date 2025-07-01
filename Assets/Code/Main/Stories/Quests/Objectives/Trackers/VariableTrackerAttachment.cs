using Awaken.TG.MVC.Elements;
using System;
using Awaken.TG.Main.Stories.Quests.Objectives.Specs;
using Awaken.TG.Main.Templates.Attachments;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Trackers {
    [AttachesTo(typeof(ObjectiveSpecBase), AttachmentCategory.Trackers, "Used to track a variable (set from Story/VS/etc).")]
    public class VariableTrackerAttachment : BaseSimpleTrackerAttachment {
        public string key;
        public string[] contexts = Array.Empty<string>();
        
        public override Element SpawnElement() {
            return new VariableTracker();
        }

        public override bool IsMine(Element element) => element is VariableTracker vt && vt.Key == key;
    }
}