using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Attachments.Customs {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Technical, "Setup combat behaviours for NPC.")]
    public class CustomCombatAttachment : MonoBehaviour, IAttachmentSpec {
        public bool weaponsAlwaysEquipped = true;
        [field: SerializeReference] public CustomCombatBaseClass CustomCombatBaseClass { get; private set; } = new();

        public Element SpawnElement() {
            return CustomCombatBaseClass.Copy();
        }

        public bool IsMine(Element element) {
            return element.GetType() == CustomCombatBaseClass.GetType();
        }
    }
}
