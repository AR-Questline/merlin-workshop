using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.AudioSystem {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Plays SFX attached to Location when NPC is in Idle.")]
    public class PlaySfxWhenNpcInIdleAttachment: MonoBehaviour, IAttachmentSpec {
        public EventReference sfxToPlay;
        public Element SpawnElement() {
            return new PlaySfxWhenNpcInIdle();
        }
        public bool IsMine(Element element) {
            return element is PlaySfxWhenNpcInIdle;
        }
    }
}