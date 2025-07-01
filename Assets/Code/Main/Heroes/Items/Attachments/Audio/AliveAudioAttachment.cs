using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments.Audio {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Common, "Used to setup audio for NPCs.")]
    public class AliveAudioAttachment : MonoBehaviour, IAttachmentSpec {
        [InlineProperty, LabelWidth(90)]
        public AliveAudioContainerWrapper aliveAudioContainerWrapper;

        public bool usesExplicitWyrdConvertedAudio;
        [InlineProperty, LabelWidth(90), ShowIf(nameof(usesExplicitWyrdConvertedAudio))]
        public AliveAudioContainerWrapper wyrdConvertedAudioContainerWrapper;
        public Element SpawnElement() {
            return new AliveAudio();
        }
        
        public bool IsMine(Element element) {
            return element is AliveAudio;
        }
    }
}