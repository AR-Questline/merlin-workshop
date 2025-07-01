using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Tooltips;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using Awaken.Utility;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Gems {
    public class VCIngredientUpgradeSlotUI : ViewComponent {
        [SerializeField] ItemSlotUI itemSlotUi;
        [SerializeField] TextMeshProUGUI quantityText;
        [SerializeField] ButtonConfig buttonConfig;
        [SerializeField] TooltipPosition leftTooltipPosition;
        [SerializeField] TooltipPosition rightTooltipPosition;
        
        ExistingItemDescriptor _itemDescriptor;
        Item _item;
        ItemTooltipUI _itemTooltipUI;
        
        public Component FocusTarget => buttonConfig.button;
        public ItemSlotUI ItemSlotUI => itemSlotUi;

        void Start() {
            buttonConfig.InitializeButton();
            buttonConfig.button.OnHover += OnHover;
            buttonConfig.button.OnEvent += Handle;
        }
        
        public void AssignItem(Item item, ItemTooltipUI itemTooltipUI) {
            _item = item;
            _itemDescriptor = new ExistingItemDescriptor(item);
            _itemTooltipUI = itemTooltipUI;
            buttonConfig.button.Interactable = GetCurrentClickedListItem() != null;
        }
        
        public void RefreshRequiredQuantity(int inventoryQuantity, int requiredQuantity) {
            quantityText.SetText($"{inventoryQuantity.ToString().ColoredText(inventoryQuantity > 0 ? ARColor.MainGrey : ARColor.MainRed)}/{requiredQuantity}");
        }

        public void ResetTooltip() {
            _itemTooltipUI.ResetDescriptor(_itemDescriptor);
        }

        public void ResetSlotIngredient() {
            buttonConfig.button.Interactable = false;
            _item.Discard();
            _item = null;
            ResetTooltip();
            
            if (World.Only<Focus>().Focused != GetCurrentClickedListItem()) {
                World.Only<Focus>().Select(null);
            }
        }
        
        void OnHover(bool hover) {
            if (hover) {
                OnHoverEntered();
            } else {
                OnHoverExit();
            }
        }
        
        void OnHoverEntered() {
            _itemTooltipUI.SetPosition(leftTooltipPosition, rightTooltipPosition);
            _itemTooltipUI.SetDescriptor(_itemDescriptor);
        }
        
        void OnHoverExit() {
            ResetTooltip();
        }
        
        UIResult Handle(UIEvent evt) {
            bool properAction = evt is UIAction action && action.Name == KeyBindings.UI.Generic.Cancel ||
                                evt is UINaviAction naviAction && naviAction.direction == NaviDirection.Left;
            if (RewiredHelper.IsGamepad && properAction) {
                World.Only<Focus>().Select(GetCurrentClickedListItem());
                return UIResult.Accept;
            }
            return UIResult.Ignore;
        }

        static VItemsListElement GetCurrentClickedListItem() {
            Item clicked = World.Only<IGemBase>().ClickedItem;
            return World.Only<ItemsListUI>().GetItemsListElementWithItem(clicked)?.View<VItemsListElement>();
        }
    }
}