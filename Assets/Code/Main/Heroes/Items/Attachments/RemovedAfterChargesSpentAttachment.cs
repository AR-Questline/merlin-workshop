using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    [AttachesTo(typeof(ItemTemplate), AttachmentCategory.ExtraCustom, "For items that are removed after a certain number of charges are spent.")]
    public class RemovedAfterChargesSpentAttachment : MonoBehaviour, IAttachmentSpec {
        [field: SerializeReference] public int ChargesToSpend { get; private set; } = 1;
        public Element SpawnElement() => new RemovedAfterChargesSpent();

        public bool IsMine(Element element) => element is RemovedAfterChargesSpent;
    }
}