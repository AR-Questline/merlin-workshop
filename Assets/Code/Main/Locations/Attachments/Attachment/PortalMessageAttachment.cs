using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [RequireComponent(typeof(PortalAttachment))]
    [AttachesTo(typeof(Portal), AttachmentCategory.ExtraCustom, "Adds custom message to portal.")]
    public class PortalMessageAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField, LocStringCategory(Category.UI)] LocString title;
        [SerializeField, LocStringCategory(Category.UI)] LocString message;
        [SerializeField] bool hasChoice;

        [ShowIf(nameof(hasChoice)), SerializeField]
        bool usesAlternativePortal;

        [ShowIf(nameof(ShowAlternativePortal)), SerializeField]
        LocationReference alternativePortal;

        bool ShowAlternativePortal => hasChoice && usesAlternativePortal;

        public LocString Title => title;
        public LocString Message => message;
        public bool HasChoice => hasChoice;
        public LocationReference AlternativePortal => alternativePortal;

        public Element SpawnElement() => new PortalMessage();

        public bool IsMine(Element element) => element is PortalMessage;

        bool IAttachmentSpec.IsValidOwner(IModel owner) => owner is Portal;
    }
}