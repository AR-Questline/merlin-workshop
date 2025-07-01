using Awaken.TG.Assets;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Technical, "Used to set parameters of playable director and control timeline playback.")]
    public class TimelineAttachment : MonoBehaviour, IAttachmentSpec {
        public bool playOnVisualLoaded;
        [ARAssetReferenceSettings(new[] { typeof(PlayableAsset) }, true, AddressableGroup.Animations)]
        [ShowIf(nameof(playOnVisualLoaded))]
        public ShareableARAssetReference initialTimeline;
        
        public Element SpawnElement() => new TimelineElement();

        public bool IsMine(Element element) => element is TimelineElement;
    }
}