using Awaken.Utility.Enums;

namespace Awaken.TG.Editor.BalanceTool.Data {
    public class StatEntryEnum : RichEnum {
        public readonly StatEntry statEntry;

        protected StatEntryEnum(string enumName, StatEntry entry) : base(enumName: enumName) {
            statEntry = entry;
        }
    }
    
    public class AdditionalStatEntryEnum : StatEntryEnum {
        AdditionalStatEntryEnum(string enumName, StatEntry entry) : base(enumName, entry) { }

        public static readonly AdditionalStatEntryEnum
            HP = new(nameof(HP), new StatEntry("HP", 50.0f)),
            Stamina = new(nameof(Stamina), new StatEntry("Stamina", 50.0f)),
            CarryLimit = new(nameof(CarryLimit), new StatEntry("Carry Limit", 50f)),
            CriticalChance = new(nameof(CriticalChance), new StatEntry("Critical Chance %", 5.0f));
    }

    public class ModifiersStatEntryEnum : StatEntryEnum {
        ModifiersStatEntryEnum(string enumName, StatEntry entry) : base(enumName: enumName, entry: entry) { }

        public static readonly ModifiersStatEntryEnum
            AdditionalDamage = new(nameof(AdditionalDamage), new StatEntry("+/- Damage", 0.0f)),
            AdditionalArmor = new(nameof(AdditionalArmor), new StatEntry("+/- Armor", 0.0f));
    }
}

