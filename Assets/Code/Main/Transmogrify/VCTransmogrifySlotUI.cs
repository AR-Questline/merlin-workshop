using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Locations.Gems;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using UnityEngine;

namespace Awaken.TG.Main.Transmogrify {
    public class VCTransmogrifySlotUI : ViewComponent<TransmogrifyUI> {
        [SerializeField] ButtonConfig slotButton;
        [SerializeField] ItemSlotUI transmogItemPreview;
        
        VTransmogrifyUI MainView => _mainView = _mainView ? _mainView : Target.View<VTransmogrifyUI>();
        VTransmogrifyUI _mainView;
        
        Item _tempTransmogItem;
        Item _clickedItem;
        
        protected override void OnAttach() {
            Target.ListenTo(IGemBase.Events.ClickedItemChanged, Refresh, this);
            Target.ListenTo(TransmogrifyUI.Events.TransmogrifyConfirmed, Refresh, this);
            Target.ListenTo(TransmogrifyUI.Events.TransmogrifyPreviewChanged, Refresh, this);
            Target.ListenTo(TransmogrifyUI.Events.TransmogrifyRemoved, OnTransmogRemoved, this);

            transmogItemPreview.SetVisibilityConfig(ItemSlotUI.VisibilityConfig.GearUpgrade);
            slotButton.InitializeButton(Target.OpenChooseUI);
            slotButton.button.OnEvent += Handle;
            Target.RegisterTransmogSlot(slotButton);
        }
        
        void Refresh(Item item) {
            if (_clickedItem == item) {
                return;
            }
            _clickedItem = item;
            
            if (item.TryGetElement<ItemTransmog>(out var transmog)) {
                Refresh(transmog);
            } else {
                Clear();
            }
        }

        void Refresh(ItemTransmog transmog) {
            if (transmog == null) {
                Clear();
                return;
            }
            
            if (transmog.HasPreview) {
                transmogItemPreview.Setup(transmog.TransmogPreview, MainView);
            } else if (transmog.IsTransmogrified) {
                _tempTransmogItem?.Discard();
                _tempTransmogItem = World.Add(new Item(transmog.Template));
                transmogItemPreview.Setup(_tempTransmogItem, MainView);
            }
        }

        void Clear() {
            transmogItemPreview.Setup(null, MainView);
        }
        
        void OnTransmogRemoved() {
            Clear();
            
            if (_clickedItem.TryGetElement<ItemTransmog>(out var transmog)) {
                Refresh(transmog);
            }
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
        
        protected override void OnDiscard() {
            _tempTransmogItem?.Discard();
        }
    }
}