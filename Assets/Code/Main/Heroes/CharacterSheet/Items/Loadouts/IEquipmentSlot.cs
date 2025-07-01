using Awaken.TG.Main.Heroes.Items;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Loadouts {
    public interface IEquipmentSlot {
        bool Locked { get; }
        bool AllowUnequip { get; }
        Item ItemInSlot { get; }
        EquipmentSlotType Type { get; }
        void Equip(Item item);
        void Unequip();
    }
}