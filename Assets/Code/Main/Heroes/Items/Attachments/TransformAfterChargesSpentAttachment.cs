using System.Linq;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    [DisallowMultipleComponent]
    [AttachesTo(typeof(ItemTemplate), AttachmentCategory.ExtraCustom, "For items that transform into something else after a certain number of charges are spent.")]
    public class TransformAfterChargesSpentAttachment : MonoBehaviour, IAttachmentSpec {
        [field: SerializeField] public int ChargesToSpend { get; private set; } = 1;
        [SerializeField] ItemSpawningData transformsInto;

        
        public ItemSpawningDataRuntime TransformsInto => transformsInto.PopLoot(this).items.FirstOrDefault();

        public Element SpawnElement() => new TransformAfterChargesSpent();

        public bool IsMine(Element element) => element is TransformAfterChargesSpent;
        
        public virtual void AfterTransform(Item item) { }
    }
}
