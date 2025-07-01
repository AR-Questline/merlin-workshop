using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Stories.Quests.Objectives.Specs;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Effectors {
    [AttachesTo(typeof(ObjectiveSpecBase), AttachmentCategory.Effectors, "Used to set an activity (PS5) when objective changes state.")]
    public class ActivityEffectorAttachment : MonoBehaviour, IAttachmentSpec {
        public string activityId;
        
        public Element SpawnElement() {
            return new ActivityEffector();
        }

        public bool IsMine(Element element) {
            return element is ActivityEffector effector && effector.ActivityId == activityId;
        }
    }
}