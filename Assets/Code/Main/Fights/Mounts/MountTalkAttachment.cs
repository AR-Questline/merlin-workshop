using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Fights.Mounts {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Allows horses to talk with hero.")]
    public class MountTalkAttachment : DialogueAttachment {
        public override Element SpawnElement() => new MountTalkAction();
        public override bool IsMine(Element element) => element is MountTalkAction;
    }
}