using System.Collections.Generic;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Gems {
    [AttachesTo(typeof(ItemTemplate), AttachmentCategory.Rare, "For items that are relics and can be inserted into equippable item.")]
    public class GemAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField]
        List<SkillReference> skills = new();

        [SerializeField]
        GemType type;

        public IEnumerable<SkillReference> Skills => skills;
        public GemType Type => type;

        public Element SpawnElement() {
            return new GemUnattached();
        }

        public bool IsMine(Element element) {
            return element is GemUnattached;
        }
    }
}