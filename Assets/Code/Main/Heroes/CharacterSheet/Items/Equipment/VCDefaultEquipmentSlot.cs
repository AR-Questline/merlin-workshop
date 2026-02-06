using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Utility.RichEnums;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Equipment {
    public class VCDefaultEquipmentSlot : VCEquipmentSlotBase {
        [Space(10f)]
        [SerializeField, RichEnumExtends(typeof(EquipmentSlotType))] RichEnumReference type;
        public override bool Hidden => false;
        public override bool Locked => !Target.HeroItems.AllowEquipping;
        public override EquipmentSlotType Type => type.EnumAs<EquipmentSlotType>();
    }
}