using System;
using Awaken.TG.Main.Heroes.Items;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Fights {
    public class ArmorType : RichEnum {
        public EquipmentType[] EquipmentTypes { get; }
        public float ArmorMultiplier { [UnityEngine.Scripting.Preserve] get; }

        protected ArmorType(string enumName, EquipmentType[] equipmentTypes, float armorMultiplier) : base(enumName) {
            EquipmentTypes = equipmentTypes;
            ArmorMultiplier = armorMultiplier;
        }

        [UnityEngine.Scripting.Preserve]
        public static readonly ArmorType
            None = new(nameof(None), Array.Empty<EquipmentType>(), 1f),
            Head = new(nameof(Head), new[] {EquipmentType.Helmet}, 5f),
            Body = new(nameof(Body), new[] {EquipmentType.Cuirass, EquipmentType.Gauntlets}, 2f),
            Legs = new(nameof(Legs), new[] {EquipmentType.Greaves, EquipmentType.Boots}, 3.33f);
    }
}