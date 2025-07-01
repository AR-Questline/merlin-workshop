using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.Main.Utility.Tags;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [RequireComponent(typeof(PortalAttachment))]
    [AttachesTo(typeof(Portal), AttachmentCategory.ExtraCustom, "Starts story instead of use portal based on flag condition")]
    public class PortalOverrideWithStoryAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField] bool requiresFlag = true;
        [ShowIf(nameof(requiresFlag))]
        [SerializeField] FlagLogic flagLogic;
        [SerializeField] StoryBookmark alternativeStory;
        [SerializeField] bool triggerBasePortalOnStoryEnd;
        
        public bool RequiresFlag => requiresFlag;
        public FlagLogic FlagLogic => flagLogic;
        public StoryBookmark AlternativeStory => alternativeStory;
        public bool TriggerBasePortalOnStoryEnd => triggerBasePortalOnStoryEnd;

        public Element SpawnElement() => new PortalOverrideWithStory();

        public bool IsMine(Element element) => element is PortalOverrideWithStory;

        bool IAttachmentSpec.IsValidOwner(IModel owner) => owner is Portal;
    }
}