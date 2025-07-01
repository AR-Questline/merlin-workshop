using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Spawners {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Spawns NPCs on interaction.")]
    public class SpawnerActionAttachment : MonoBehaviour, IAttachmentSpec {
        // === Operations
        public Element SpawnElement() => new SpawnerAction();

        public bool IsMine(Element element) {
            return element is SpawnerAction;
        }
    }
}
