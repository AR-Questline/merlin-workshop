using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    [AttachesTo(typeof(ItemTemplate), AttachmentCategory.Rare, "For items that can unlock crafting recipes.")]
    public class RecipeItemAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField, TemplateType(typeof(BaseRecipe))] TemplateReference recipeRef;
        [SerializeField] bool destroyOnPickup;

        public BaseRecipe Recipe => recipeRef.Get<BaseRecipe>();
        public bool DestroyOnPickup => destroyOnPickup;
        
        public Element SpawnElement() => new RecipeItem();
        public bool IsMine(Element element) => element is RecipeItem;
    }
}