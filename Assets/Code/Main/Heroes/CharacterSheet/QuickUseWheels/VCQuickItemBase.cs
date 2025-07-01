using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Localization;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.QuickUseWheels {
    public abstract class VCQuickItemBase : VCQuickUseAction {
        [Space(10f)]
        [SerializeField] protected ItemSlotUI slot;
        
        protected Item _item;
        protected virtual bool UseOnClose => true;
        protected virtual string ItemName => LocTerms.UIItemsEquip.Translate();
        protected virtual bool ShowQuantity => false;
        protected static HeroItems HeroItems => Hero.Current.HeroItems;
        
        public override OptionDescription Description => new(true, ItemName);
        
        protected abstract Item RetrieveItem();
        public abstract void UseItemAction();

        protected override void OnAttach() {
            _item = RetrieveItem();

            var visibility = ItemSlotUI.VisibilityConfig.QuickWheel;
            visibility.quantity = ShowQuantity;
            
            slot.SetVisibilityConfig(visibility);
            Refresh();
            base.OnAttach();
        }

        protected void Refresh() {
            if (previewObject) {
                previewObject.SetActive(_item == null);
            }
            
            if (Target == null || _item == null || _item is { HasBeenDiscarded: true }) {
                return;
            }

            slot.Setup(_item, ParentView);
        }

        protected override void NotifyHover() {
            slot.NotifyHover();
        }

        protected override void OnShow() {
            VQuickUseWheel.QuickItemTooltipUIPrimary.ShowItem(_item);
        }

        protected override void OnHide() {
            VQuickUseWheel.HideItemTooltips();
        }
        
        public override void OnSelect(bool onClose) {
            if (onClose && !UseOnClose) {
                return;
            }
            
            if (_item is { HasBeenDiscarded: false }) {
                UseItemAction();
            } else {
                FMODManager.PlayOneShot(_selectNegativeSound);
            }
        }
    }
}