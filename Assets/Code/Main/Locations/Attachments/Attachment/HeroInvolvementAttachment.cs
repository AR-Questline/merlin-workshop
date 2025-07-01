using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Involves hero during location interaction (fireplace).")]
    public class HeroInvolvementAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField] float finishDelay = 0f;
        [SerializeField] bool hideWeapons = true;
        [SerializeField] bool hideHands = true;

        public float FinishDelay => finishDelay;
        public bool HideWeapons => hideWeapons;
        public bool HideHands => hideHands;
        
        public Element SpawnElement() => new HeroLocationInvolvement();

        public bool IsMine(Element element) {
            return element is HeroLocationInvolvement;
        }
    }
}