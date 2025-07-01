using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Stories.Quests.Objectives.Specs;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes.Tags;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Effectors {
    [AttachesTo(typeof(ObjectiveSpecBase), AttachmentCategory.Effectors, "Used to set a flag when objective changes state.")]
    public class FlagEffectorAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField, Tags(TagsCategory.Flag)] string flag;
        [SerializeField] ObjectiveState runOnState;

        public string Flag => flag;
        public ObjectiveState RunOnState => runOnState;
        
        public Element SpawnElement() {
            return new FlagEffector();
        }

        public bool IsMine(Element element) {
            return element is FlagEffector effector && effector.RunOnState == runOnState; 
        }
    }
}