using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    [AttachesTo(typeof(ItemTemplate), AttachmentCategory.Rare, "For items that display as one item and once picked turn into another.")]
    public class FakeItemAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField, TemplateType(typeof(ItemTemplate))] TemplateReference realItem;

        public ItemTemplate RealItem => realItem.Get<ItemTemplate>();
        
        public Element SpawnElement() => new FakeItem();
        public bool IsMine(Element element) => element is FakeItem;
    }
}