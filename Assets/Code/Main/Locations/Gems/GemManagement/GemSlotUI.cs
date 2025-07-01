using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Gems;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Gems.GemManagement {
    public partial class GemSlotUI : Element<GemManagementUI> {
        readonly Transform _slotsParent;
        VGemSlotUI _view;
        GemAttached _gemAttached;
        
        public ItemTemplate ItemInSlot => _gemAttached?.Template != null ? _gemAttached?.Template : PreviewGemItem?.Template;
        public Item PreviewGemItem { get; private set; }
        public bool IsUnlocked { get; private set; }
        public bool HasGemAttached => _gemAttached != null;
        public bool IsBeingPreviewed => PreviewGemItem != null;
        
        public new static class Events {
            public static readonly Event<GemSlotUI, bool> GemSlotRefreshed = new(nameof(GemSlotRefreshed));
            public static readonly Event<GemSlotUI, GemSlotUI> GemSlotClicked = new(nameof(GemSlotClicked));
        }

        public GemSlotUI(bool isUnlocked, GemAttached gemAttached, Transform slotsParent) {
            IsUnlocked = isUnlocked;
            _gemAttached = gemAttached;
            _slotsParent = slotsParent;
        }

        protected override void OnInitialize() {
            _view = World.SpawnView<VGemSlotUI>(this, true, true, _slotsParent);
            if (HasGemAttached) {
                _view.SetGemSprite(_gemAttached.Template.IconReference.Get());
            }
            this.Trigger(Events.GemSlotRefreshed, true);;
        }
        
        public void SetPreviewGemItem(Item previewGemItem) {
            PreviewGemItem = previewGemItem;
            _view.SetGemSprite(PreviewGemItem.Icon.Get());
            this.Trigger(Events.GemSlotRefreshed, true);
        }
        
        public void UnlockGemSlot() {
            IsUnlocked = true;
            this.Trigger(Events.GemSlotRefreshed, true);
        }
        
        public void AttachGem() {
            Item gearItem = ParentModel.ClickedItem;
            _gemAttached = gearItem.Element<ItemGems>().AttachGem(PreviewGemItem);
            ParentModel.Trigger(GemManagementUI.Events.GemAttached, new GemManagementUI.GemAttachmentChange(gearItem, PreviewGemItem, true));
            PreviewGemItem = null;
            this.Trigger(Events.GemSlotRefreshed, true);
        }

        public Item RetrieveGem() {
            var itemGem = _gemAttached.RetrieveGem();
            ParentModel.Trigger(GemManagementUI.Events.GemDetached, new GemManagementUI.GemAttachmentChange(ParentModel.ClickedItem, itemGem, false));
            _gemAttached = null;
            this.Trigger(Events.GemSlotRefreshed, true);
            return itemGem;
        }

        public void OnSlotButtonClicked() {
            this.Trigger(Events.GemSlotClicked, this);
        }
    }
}