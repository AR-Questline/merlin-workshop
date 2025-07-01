using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Shops {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Common, "Adds shop to the location, can be opened by Story.")]
    public class ShopAttachment : MonoBehaviour, IAttachmentSpec {
        [TemplateType(typeof(ShopTemplate))] 
        public TemplateReference shopDefinition;

        public ShopTemplate ShopTemplate => shopDefinition.Get<ShopTemplate>();

        public Element SpawnElement() {
            return new Shop();
        }

        public bool IsMine(Element element) => element is Shop;
    }
}