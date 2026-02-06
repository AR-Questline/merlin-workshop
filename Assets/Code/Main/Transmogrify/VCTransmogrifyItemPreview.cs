using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Tooltips.Components;
using Awaken.TG.Main.Locations.Gems;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Transmogrify {
    public class VCTransmogrifyItemPreview : ViewComponent<TransmogrifyUI> {
        [SerializeField] ItemSlotUI clickedItemPreview;
        [SerializeField] ItemTooltipHeaderComponent header;

        VTransmogrifyUI MainView => _mainView = _mainView ? _mainView : Target.View<VTransmogrifyUI>();
        VTransmogrifyUI _mainView;
        Item _clickedItem;

        protected override void OnAttach() {
            Target.ListenTo(IGemBase.Events.ClickedItemChanged, OnGearItemClicked, this);
            clickedItemPreview.SetVisibilityConfig(ItemSlotUI.VisibilityConfig.GearUpgrade);
        }
        
        void OnGearItemClicked(Item item) {
            if (item == null) {
                _clickedItem = null;
                MainView.HideRightSide();
                return;
            }

            if (_clickedItem == item) {
                return;
            }

            _clickedItem = item;
            clickedItemPreview.Setup(item, MainView);
            header.Refresh(ItemDescriptorType.ExistingItem.GetItemDescriptor(item), null);
            MainView.HideRightSide();
        }
    }
}