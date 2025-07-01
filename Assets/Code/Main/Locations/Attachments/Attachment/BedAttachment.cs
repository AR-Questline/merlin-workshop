using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Rare, "Allow resting and apply statuses on rest.")]
    public class BedAttachment : MonoBehaviour, IAttachmentSpec {
        [LocStringCategory(Category.Interaction)]
        public LocString label;

        [TemplateType(typeof(StatusTemplate))] public TemplateReference statusToAdd;
        public float statusToAddDuration = 300;
        [TemplateType(typeof(StatusTemplate))] public TemplateReference statusToAddOnRejuvenation;
        public float statusToAddOnRejuvenationDuration = 600;
        [TemplateType(typeof(StatusTemplate))] public TemplateReference statusToRemove;
        
        
        public Element SpawnElement() => new BedElement();
        public bool IsMine(Element element) => element is BedElement;
    }
}