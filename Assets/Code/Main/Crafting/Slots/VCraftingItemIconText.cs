using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Crafting.Slots {
    [UsesPrefab("Crafting/VCraftingItemIconText")]
    public class VCraftingItemIconText : View<InteractableItem> {
        public Image icon;
        public Image quality;
        public TextMeshProUGUI counter;
        Item Item => Target.Item;
        
        // === Initialization
        public override Transform DetermineHost() => Target.ParentModel.ItemSlot;
        
        protected override void OnInitialize() {
            Target.Item.Icon.RegisterAndSetup(this, icon);
            Refresh();
            
            Target.ListenTo(Model.Events.AfterChanged, Refresh, this);
        }

        void Refresh() {
            quality.color = Item.Quality.BgColor;
            
            bool showCounter = Target.requiredQuantity > 1 && Target.ParentModel is not EditableWorkbenchSlot;
            counter.transform.parent.gameObject.SetActive(showCounter);
            counter.text = Target.requiredQuantity.ToString();
        }
    }
}