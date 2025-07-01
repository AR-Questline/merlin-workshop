using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes {
    public class VCHeroItemQuantity : ViewComponent {
        [SerializeField] TextMeshProUGUI itemQuantityText;
        [SerializeField, TemplateType(typeof(ItemTemplate))] TemplateReference itemTemplateRef;
        
        ItemTemplate ItemTemplate => itemTemplateRef.Get<ItemTemplate>();
        IEventListener _itemQuantityListener;
        
        protected override void OnAttach() {
            ICharacterInventory heroInventory = Hero.Current.Inventory;
            
            Item heroItem = heroInventory.Items.FirstOrDefault(item => item.Template == ItemTemplate);
            itemQuantityText.SetText(heroItem?.Quantity.ToString() ?? "0");
            _itemQuantityListener = heroItem?.ListenTo(Item.Events.QuantityChanged, OnItemQuantityChanged, this);
            heroInventory.ListenTo(ICharacterInventory.Events.PickedUpItem, OnItemPicked, this);
        }

        void OnItemPicked(Item pickedItem) {
            if (pickedItem.Template != ItemTemplate) {
                return;
            }
            
            if (_itemQuantityListener != null) {
                //already listening to an  item in the inventory
                return;
            }
            
            itemQuantityText.SetText(pickedItem.Quantity.ToString());
            _itemQuantityListener = pickedItem.ListenTo(Item.Events.QuantityChanged, OnItemQuantityChanged, this);
        }

        void OnItemQuantityChanged(QuantityChangedData itemChange) {
            itemQuantityText.SetText(itemChange.CurrentQuantity.ToString());
            if (itemChange.CurrentQuantity <= 0) {
                World.EventSystem.RemoveListener(_itemQuantityListener);
                _itemQuantityListener = null;
            }
        }
    }
}