using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.Utility.Animations;
using Awaken.Utility.GameObjects;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.Slots {
    [UsesPrefab("Crafting/Handcrafting/" + nameof(VInventorySlot))]
    public class VInventorySlot : RetargetableView<InventorySlot>, ISelectableCraftingSlot, IUIAware {
        [field: SerializeField] public Transform GhostItemParent { get; private set; }
        [field: SerializeField] public Transform ItemParent { get; private set; }
        [field: SerializeField] public ARButton SlotButton { get; private set; }
        
        [SerializeField] GameObject itemInfoContent;
        [SerializeField] ItemSlotUI itemSlotUI;
        [SerializeField] GameObject itemQuantityTextRoot;
        [SerializeField] TMP_Text itemQuantityText;
        
        ExistingItemDescriptor _tempItemDescriptor;
        Item _wantedItem;
        
        public override Transform DetermineHost() => Target.DeterminedHost;
        
        public void Submit() => Target.Submit();
        
        protected override void OnNewTarget() {
            SlotButton.OnClick += Target.Submit;
            Target.AddElement(Target.interactableItem);

            Target.ListenTo(Model.Events.AfterChanged, Refresh, this);
            Target.ListenTo(Model.Events.AfterElementsCollectionModified, Refresh, this);
            Target.interactableItem.ListenTo(Model.Events.AfterChanged, Refresh, this);
            Refresh();
        }
        
        protected override void OnOldTargetRemove() {
            SlotButton.OnClick -= Target.Submit;
            Target.RemoveElementsOfType<InteractableItem>();
        }
        
        void Refresh() {
            SlotButton.interactable = !Target.IsRecipeCrafting;
            
            itemInfoContent.SetActive(Target.Item != null || Target.WantedItemTemplate != null);
            if (Target.Item == null) {
                if (Target.WantedItemTemplate != null) {
                    _wantedItem?.Discard();
                    _wantedItem = new Item(Target.WantedItemTemplate);
                    World.Add(_wantedItem);
                    RefreshItemDescriptor(_wantedItem);
                }
                return;
            }
            
            RefreshItemDescriptor(Target.Item);
        }

        void RefreshItemDescriptor(Item item) {
            _tempItemDescriptor = new ExistingItemDescriptor(item);
            itemSlotUI.SetVisibilityConfig(ItemSlotUI.VisibilityConfig.QuickWheel);
            itemSlotUI.Setup(_tempItemDescriptor.ExistingItem, this);

            bool hasQuantity = Target.interactableItem.requiredQuantity > 1;
            itemQuantityTextRoot.SetActiveOptimized(hasQuantity);
            itemQuantityText.SetActiveAndText(hasQuantity, Target.interactableItem.requiredQuantity.ToString());
        }
        
        public UIResult Handle(UIEvent evt) {
            if (evt is UISubmitAction or UIEMouseDown { IsLeft: true }) {
                Target.Submit();
                return UIResult.Accept;
            }
            
            if (evt is UIEPointTo) {
                itemSlotUI.NotifyHover();
                return UIResult.Accept;
            }
            
            return UIResult.Ignore;
        }

        protected override IBackgroundTask OnDiscard() {
            _wantedItem?.Discard();
            _wantedItem = null;
            _tempItemDescriptor = null;
            return base.OnDiscard();
        }
    }
}