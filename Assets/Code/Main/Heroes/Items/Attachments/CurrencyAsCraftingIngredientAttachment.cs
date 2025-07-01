using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Shops;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    [AttachesTo(typeof(ItemTemplate), AttachmentCategory.ExtraCustom, "For currencies that can be used in crafting.")]
    public class CurrencyAsCraftingIngredientAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField] CurrencyType type;
        
        public CurrencyType Type => type;

        public Element SpawnElement() {
            return new CurrencyAsCraftingIngredient();
        }
        
        public bool IsMine(Element element) {
            return element is CurrencyAsCraftingIngredient;
        }
    }
}