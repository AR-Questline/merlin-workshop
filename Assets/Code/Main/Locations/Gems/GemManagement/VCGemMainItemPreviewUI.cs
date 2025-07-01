using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Gems;
using Awaken.TG.Main.Heroes.Items.Tooltips.Components;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Gems.GemManagement {
    public class VCGemMainItemPreviewUI : ViewComponent<GemManagementUI> {
        [SerializeField] ItemSlotUI clickedItemPreview;
        [SerializeField] ItemTooltipHeaderComponent header;
        [SerializeField] VGemSlotUI slotPrefab;
        [SerializeField] Transform relicSlotsParent;
        
        Item _clickedItem;
        
        protected override void OnAttach() {
            Target.ListenTo(IGemBase.Events.ClickedItemChanged, OnGearItemClicked, this);
            clickedItemPreview.SetVisibilityConfig(ItemSlotUI.VisibilityConfig.GearUpgrade);
        }
        
        void OnGearItemClicked(Item item) {
            var mainView = Target.View<VGemManagementUI>();

            if (item == null) {
                _clickedItem = null;
                Target.RemoveElementsOfType<GemSlotUI>();
                mainView.HideRightSide();
                return;
            }

            if (_clickedItem == item) {
                return;
            }

            Target.RemoveElementsOfType<GemSlotUI>();

            _clickedItem = item;
            clickedItemPreview.Setup(item, mainView);
            header.Refresh(ItemDescriptorType.ExistingItem.GetItemDescriptor(item), null);

            int spawnedSlots = 0;
            var attachedGems = _clickedItem.Elements<GemAttached>();
            foreach (var attached in attachedGems) {
                Target.AddElement(new GemSlotUI(true, attached, relicSlotsParent));
                spawnedSlots++;
            }
            
            int unlockedSlots = _clickedItem.TryGetElement<ItemGems>()?.FreeSlots ?? 0;
            for (int i = 0; i < unlockedSlots; i++) {
                Target.AddElement(new GemSlotUI(true, null, relicSlotsParent));
                spawnedSlots++;
            }
            
            int lockedSlots = _clickedItem.MaxGemSlots - spawnedSlots;
            for (int i = 0; i < lockedSlots; i++) {
                Target.AddElement(new GemSlotUI(false, null, relicSlotsParent));
            }
            
            mainView.HideRightSide();
            
            var gemSlotFocusTarget = Target.Elements<GemSlotUI>().FirstOrDefault()?.View<VGemSlotUI>().FocusTarget;
            if (Target.ItemsUI.TryGetElement<ItemsListUI>(out var itemsList)) {
                foreach (var itemElement in itemsList.Elements<ItemsListElementUI>()) {
                    itemElement.NextFocusTarget = () => gemSlotFocusTarget;
                }
            }
        }
    }
}