using Awaken.TG.Assets;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Used for NPCs that don't die when killed. Instead it will disappear and reappear after some time.")]
    public class TemporaryDeathAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField, AnimancerAnimationsAssetReference] ARAssetReference animations;
        [SerializeField] float deathStartDuration = 3f;
        [SerializeField] float deathEndDuration = 3f;
        [Space] 
        [SerializeField, MinValue(10), SuffixLabel("seconds")] float deathDurationHero = 10;
        [SerializeField, MinValue(10), SuffixLabel("seconds")] float deathDurationOther = 10;
        [SerializeField] bool forceChangeIntoGhost;
        [SerializeField, ShowIf(nameof(forceChangeIntoGhost))] bool ifForcedStayInGhost;
        [Space]
        [SerializeField] StoryBookmark storyToRunOnTemporaryDeath;
        [SerializeField, ARAssetReferenceSettings(new[] { typeof(GameObject) }, group: AddressableGroup.VFX)]
        ShareableARAssetReference deathVFX;
        
        public ARAssetReference Animations => animations;
        public float DeathStartDuration => deathStartDuration;
        public float DeathEndDuration => deathEndDuration;
        public float DeathDurationHero => deathDurationHero;
        public float DeathDurationOther => deathDurationOther;
        public bool ForceChangeIntoGhost => forceChangeIntoGhost;
        public bool IfForcedStayInGhost => ifForcedStayInGhost;
        public StoryBookmark StoryToRunOnTemporaryDeath => storyToRunOnTemporaryDeath;
        public ShareableARAssetReference DeathVFX => deathVFX;

        public Element SpawnElement() => new TemporaryDeathElement();

        public bool IsMine(Element element) => element is TemporaryDeathElement;
    }
}