using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.QuickUseWheels {
    public class VCQuickSlot : VCQuickItemBase {
        [Space(10f)]
        [SerializeField, RichEnumExtends(typeof(EquipmentSlotType))] RichEnumReference equipment;
        [SerializeField] GameObject selectedIndicator;
        
        EquipmentSlotType EquipmentSlotType => equipment.EnumAs<EquipmentSlotType>();

        protected override bool UseOnClose => false;
        protected override string ItemName => LocTerms.Select.Translate();
        protected override bool ShowQuantity => true;

        protected override void OnAttach() {
            Target.Hero.ListenTo(HeroItems.Events.QuickSlotSelected, OnQuickSlotSelected, this);
            selectedIndicator.SetActive(EquipmentSlotType == Target.HeroItems.SelectedQuickSlotType);
            base.OnAttach();
        }

        public override void OnSelect(bool onClose) {
            if (onClose && !UseOnClose) {
                return;
            }

            if (Target.HeroItems.IsEquipped(EquipmentSlotType)) {
                SelectQuickSlot();
            } else {
                FMODManager.PlayOneShot(_selectNegativeSound);
            }
        }

        public void SelectQuickSlot() {
            Target.HeroItems.SelectQuickSlot(EquipmentSlotType);
        }
        
        protected override Item RetrieveItem() {
            return Target.HeroItems.EquippedItem(EquipmentSlotType);
        }

        public override void UseItemAction() {
            if (_item is not { HasBeenDiscarded: false }) {
                FMODManager.PlayOneShot(_selectNegativeSound);
                return;
            }
            
            _item.Use();
            Refresh();
        }

        void OnQuickSlotSelected(EquipmentSlotType equipmentSlotType) {
            selectedIndicator.SetActive(equipmentSlotType == EquipmentSlotType);
        }
    }
}