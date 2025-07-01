using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Attachments.Bosses {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Technical, "Maks location as a boss.")]
    public class BossCombatAttachment : MonoBehaviour, IAttachmentSpec {
        public bool weaponsAlwaysEquipped = true;
        public bool canLoseTargetBasedOnVisibility = false;
        [field: SerializeReference] public BaseBossCombat BossBaseClass { get; private set; }
        
        public Element SpawnElement() {
            return BossBaseClass.Copy();
        }

        public bool IsMine(Element element) {
            return element.GetType() == BossBaseClass.GetType();
        }
    }
}