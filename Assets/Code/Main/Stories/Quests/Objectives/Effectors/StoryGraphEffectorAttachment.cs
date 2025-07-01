using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Stories.Quests.Objectives.Specs;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Effectors {
    [AttachesTo(typeof(ObjectiveSpecBase), AttachmentCategory.Effectors, "Used to run a StoryGraph when objective changes state.")]
    public class StoryGraphEffectorAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField] StoryBookmark storyBookmark;
        [SerializeField] ObjectiveState runOnState;

        public StoryBookmark StoryBookmark => storyBookmark;
        public ObjectiveState RunOnState => runOnState;
        
        public Element SpawnElement() {
            return new StoryGraphEffector();
        }

        public bool IsMine(Element element) {
            return element is StoryGraphEffector effector && effector.RunOnState == runOnState; 
        }
    }
}