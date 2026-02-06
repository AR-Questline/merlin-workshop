using System;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Choose;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Tabs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Transmogrify {
    public partial class TransmogrifyChooseUI : ItemChooseUI<TransmogrifyUI>, IClosable {
        public override Type ItemsListElementView => typeof(VItemEqChooseElement);
        public override ItemsTabType SortingTab => ItemsTabType.TransmogChooseSortingTab;
        ItemTransmog _currentTransmog;
        Item _currentChosenItem;
        readonly Item _targetItem;

        public TransmogrifyChooseUI(Item targetItem) : base(ItemsTabType.All.Yield()) {
            _targetItem = targetItem;
        }

        protected override void OnFullyInitialized() {
            base.OnFullyInitialized();
            ParentModel.ListenTo(TransmogrifyUI.Events.TransmogrifyRemoved, OnTransmogRemoved, this);
        }

        protected override bool ItemFilter(Item item) {
            if (_targetItem.EquipmentType != item.EquipmentType || _targetItem.Template == item.Template) {
                return false;
            }
            
            if (_targetItem.EquipmentType == EquipmentType.OneHanded || _targetItem.EquipmentType == EquipmentType.TwoHanded || _targetItem.EquipmentType == EquipmentType.Bow) {
                // For weapons, we can only transmog to items of the same type
                return _targetItem.IsSameWeaponType(item);
            }

            return true;
        }

        protected override void HoveredItemsChanged(Item item) {
            _promptSelect.SetActive(item != null && (item != _currentTransmog?.TransmogPreview || item.Template != _currentTransmog?.Template));
        }

        protected override void Choose(Item item) {
            if (_currentChosenItem == item) { 
                return;
            }
            
            _currentChosenItem = item;
            _currentTransmog = _targetItem.HasElement<ItemTransmog>() 
                ? _targetItem.Element<ItemTransmog>() 
                : _targetItem.AddElement<ItemTransmog>();

            _currentTransmog.EquipPreview(_currentChosenItem);
            ParentModel.Trigger(TransmogrifyUI.Events.TransmogrifyPreviewChanged, _currentTransmog);
            HoveredItemsChanged(_currentChosenItem);
        }

        void OnTransmogRemoved() {
            _currentChosenItem = null;
            _currentTransmog = _targetItem.TryGetElement<ItemTransmog>();
        }

        public void Close() {
            ParentModel.ParentModel.ShowEmptyInfo(true);
            Discard();
        } 
    }
}