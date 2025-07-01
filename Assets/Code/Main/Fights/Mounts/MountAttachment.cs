using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Mounts {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Rare, "Used by horses.")]
    public class MountAttachment : MonoBehaviour, IAttachmentSpec {
        [LocStringCategory(Category.Mount)]
        public LocString mountName;
        [Tooltip("Wild horses can be taken without stealing")]
        public bool wildHorse;
        [SerializeField, TemplateType(typeof(MountData))]
        TemplateReference mountData;

        public MountData MountData => mountData.Get<MountData>();
        
        public Element SpawnElement() {
            return new MountElement();
        }

        public bool IsMine(Element element) {
            return element is MountElement;
        }
    }
}
