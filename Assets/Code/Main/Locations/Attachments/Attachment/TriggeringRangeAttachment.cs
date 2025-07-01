using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Triggers story when hero enters or exits location range. Requires TriggerVolume in location hierarchy.")]
    public class TriggeringRangeAttachment : MonoBehaviour, IAttachmentSpec {
        [InfoBox("For this attachment to work, Location must have TriggerVolume somewhere inside")]
        [InfoBox("If story is not set it will trigger default action for this location"), TemplateType(typeof(StoryGraph))]
        public TemplateReference storyToRun;
        [InfoBox("False mean trigger on exit")]
        public bool triggerOnEnter = true;
        public bool triggerOnExit;
        public bool onlyOnce = true;

        // === Implementation
        public Element SpawnElement() {
            return new TriggeringRange();
        }

        public bool IsMine(Element element) {
            return element is TriggeringRange;
        }
    }
}
