using Awaken.TG.Main.Heroes.Items;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Equipment {
    public class VCHorseArmorEquipmentSlot : VCEquipmentSlotBase {
        [SerializeField] GameObject lockIcon;
        
        public override bool Hidden => Hero.Current.Element<HeroHorseArmorHandler>().SlotHidden;
        public override bool Locked => Hero.Current.Element<HeroHorseArmorHandler>().SlotLocked;
        public override EquipmentSlotType Type => EquipmentSlotType.HorseArmor;

        protected override void OnAttach() {
            base.OnAttach();
            if (!Hidden) {
                lockIcon.SetActive(Locked);
            }
        }
    }
}