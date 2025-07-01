using System.Collections.Generic;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Initial skills for the location.")]
    public class InitialSkillsAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField]
        List<SkillReference> skills = new();
        
        public IEnumerable<SkillReference> Skills => skills;

        // === Implementation
        public Element SpawnElement() {
            return new InitialSkills();
        }

        public bool IsMine(Element element) {
            return element is InitialSkills;
        }
    }
}
