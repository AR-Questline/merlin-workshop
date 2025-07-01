using Awaken.TG.Main.Localization;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    /// <summary>
    /// Actions that can be taken on items.
    /// </summary>
    public class ItemActionType : RichEnum {
        public string DisplayName { [UnityEngine.Scripting.Preserve] get; }
        public int Priority { get; }

        protected ItemActionType(string enumName, string displayName, int priority) : base(enumName) {
            DisplayName = displayName;
            Priority = priority;
        }
        
        [UnityEngine.Scripting.Preserve]
        public static readonly ItemActionType
            Passive = new(nameof(Passive), "", -1),
            Use = new(nameof(Use), LocTerms.Use, 8),
            Eat = new(nameof(Eat), LocTerms.Eat, 9),
            CastSpell = new(nameof(CastSpell), "Spell", 10),
            Read = new(nameof(Read), "Read", 4),
            Equip = new(nameof(Equip), "Equip", 0),
            Unequip = new(nameof(Unequip), "Unequip", 64);
        
        
        public static bool IsEquipAction(ItemActionType type) => type == Equip;
        public static bool IsConsumableAction(ItemActionType type) => type == Use || type == Eat;
        public static bool IsEdible(ItemActionType type) => type == Eat;
        public static bool IsUsable(ItemActionType type) => type == Use;
    }
}