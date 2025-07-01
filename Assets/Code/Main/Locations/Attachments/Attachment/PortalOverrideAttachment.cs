using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.Main.Utility.Tags;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [RequireComponent(typeof(PortalAttachment))]
    [AttachesTo(typeof(Portal), AttachmentCategory.ExtraCustom, "Changes portal destination based on flag condition")]
    public class PortalOverrideAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField] FlagLogic flagLogic;
        [SerializeField] LocationReference alternativePortal;
        
        public FlagLogic FlagLogic => flagLogic;
        public LocationReference AlternativePortal => alternativePortal;

        public Element SpawnElement() => new PortalOverride();

        public bool IsMine(Element element) => element is PortalOverride;

        bool IAttachmentSpec.IsValidOwner(IModel owner) => owner is Portal;
    }
}