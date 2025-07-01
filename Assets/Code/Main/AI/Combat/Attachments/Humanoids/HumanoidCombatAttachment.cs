using Awaken.TG.Main.General;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Attachments.Humanoids {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Technical, "Setup combat behaviours for human NPC.")]
    public class HumanoidCombatAttachment : MonoBehaviour, IAttachmentSpec {
        public FloatRange meleeRangedSwitchDistance = new(4f, 6.5f);
        public bool weaponsAlwaysEquipped;
        public bool usesCombatMovementAnimations = true;
        public bool usesAlertMovementAnimations = true;
        public bool canBePushed = true;
        public bool canLookAround = true;
        public bool canBeSlidInto = true;
        public ItemProjectileAttachment.ItemProjectileData customArrowData = new();

        public Element SpawnElement() {
            return new HumanoidCombat();
        }

        public bool IsMine(Element element) {
            return element is HumanoidCombat;
        }
    }
}
