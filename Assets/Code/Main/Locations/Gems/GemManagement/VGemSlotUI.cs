using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Tooltips;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Locations.Gems.GemManagement {
    [UsesPrefab("Gems/" + nameof(VGemSlotUI))]
    public class VGemSlotUI : View<GemSlotUI> {
        SpriteReference _gemSpriteReference;
        
        [SerializeField] ButtonConfig slotButton;
        [SerializeField] Image gemItemIcon;
        [SerializeField] GameObject addGemObject;
        [SerializeField] Image attachedGemIndicator;
        [SerializeField] TooltipPosition leftTooltipPosition;
        [SerializeField] TooltipPosition rightTooltipPosition;
        
        IItemDescriptor _itemDescriptor;
        
        public Component FocusTarget => slotButton.button;

        ItemTooltipUI IngredientTooltip => Target.ParentModel.IngredientTooltipUI;

        protected override void OnInitialize() {
            slotButton.InitializeButton(OnSlotButtonClicked);
            slotButton.button.OnHover += OnHover;
            slotButton.button.OnEvent += Handle;
            Target.ListenTo(GemSlotUI.Events.GemSlotRefreshed, Refresh, this);
        }
        
        void OnHover(bool hover) {
            if (Target.ItemInSlot == null) {
                return;
            }
            
            if (hover) {
                OnHoverEntered();
            } else {
                OnHoverExit();
            }
        }
        
        void OnHoverEntered() {
            _itemDescriptor = new TempItemDescriptor(Target.ItemInSlot, Target);
            IngredientTooltip.SetPosition(leftTooltipPosition, rightTooltipPosition);
            IngredientTooltip.SetDescriptor(_itemDescriptor);
        }
        
        void OnHoverExit() {
            ResetTooltip();
        }
        
        void ResetTooltip() {
            IngredientTooltip.ResetDescriptor(_itemDescriptor);
            _itemDescriptor = null;
        }

        void OnSlotButtonClicked() {
            Target.OnSlotButtonClicked();
        }

        void Refresh() {
            attachedGemIndicator.gameObject.SetActive(Target.HasGemAttached);
            gemItemIcon.gameObject.SetActive(Target.HasGemAttached || Target.IsBeingPreviewed);
            addGemObject.SetActive(!Target.IsUnlocked);

            if (_itemDescriptor != null && Target.ItemInSlot == null) {
                ResetTooltip();
            }
        }

        public void SetGemSprite(SpriteReference gemItemIconReference) {
            _gemSpriteReference?.Release();
            gemItemIconReference.SetSprite(gemItemIcon);
        }

        UIResult Handle(UIEvent evt) {
            bool isCancelAction = evt is UIAction action && action.Name == KeyBindings.UI.Generic.Cancel;
            bool isNaviLeftAction = evt is UINaviAction naviAction && naviAction.direction == NaviDirection.Left;
            bool properAction = isCancelAction || isNaviLeftAction;
            
            if (RewiredHelper.IsGamepad && properAction) {
                if (transform.GetSiblingIndex() != 0 && isNaviLeftAction) {
                    return UIResult.Ignore;
                }

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