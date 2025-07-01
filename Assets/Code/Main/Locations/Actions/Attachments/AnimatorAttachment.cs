using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Technical, "Used to set parameters in animator that are persistent between sessions.")]
    public class AnimatorAttachment : MonoBehaviour, IAttachmentSpec {
        public Element SpawnElement() => new AnimatorElement();

        public bool IsMine(Element element) => element is AnimatorElement;
    }
}