using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Rare, "Makes the location interactable, might be used by VS.")]
    public class InteractAttachment : MonoBehaviour, IAttachmentSpec {
        [LocStringCategory(Category.Interaction)]
        public LocString customInteractLabel;
        public EventReference interactionSound;
        public bool blockInCombat;
        public bool waitForManualFinishAction;

        public Element SpawnElement() {
            return new InteractAction();
        }
        public bool IsMine(Element element) => element is InteractAction;
    }
}