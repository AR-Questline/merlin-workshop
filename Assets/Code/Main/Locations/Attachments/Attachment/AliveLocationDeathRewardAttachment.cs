using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Technical, "Grants items on Alive Location death.")]
    public class AliveLocationDeathRewardAttachment : MonoBehaviour, IAttachmentSpec {
        public LootTableWrapper reward;
        public bool hasToBeKilledByHero = true;
            
        public Element SpawnElement() => new AliveLocationDeathReward();
        public bool IsMine(Element element) => element is AliveLocationDeathReward;
    }
}
