using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.SocialServices;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    [AttachesTo(typeof(ItemTemplate), AttachmentCategory.Technical, "Item won't be available without the DLC.")]
    public class ItemFromDlcSpec : MonoBehaviour, IAttachmentSpec {
        [SerializeField] DlcCategory requiredDlcCategory;
        
        public DlcCategory RequiredDlcCategory => requiredDlcCategory;
        
        public Element SpawnElement() {
            return new ItemFromDlc();
        }
        public bool IsMine(Element element) {
            return element is ItemFromDlc;
        }
    }
}