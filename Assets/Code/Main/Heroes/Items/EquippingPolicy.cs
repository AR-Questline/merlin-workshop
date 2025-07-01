using System;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items.Loadouts;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Heroes.Items {
    /// <summary>
    /// It contains rules of matching equipment (applied when equipping item). <br/>
    /// eg: <br/>
    /// when equipping magic in main hand in off hand can be only another magic or shield. <br/>
    /// when equipping bow in quiver put best arrow hero has
    /// </summary>
    public abstract class EquippingPolicy {
        protected Item Item { get; private set; }
        protected ICharacterInventory Inventory { get; private set; }
        protected ILoadout Loadout { get; private set; }
        protected EquipmentSlotType Slot { get; private set; }

        public void Apply(Item item, ICharacterInventory inventory, ILoadout loadout, EquipmentSlotType slot) {
            Item = item;
            Inventory = inventory;
            Loadout = loadout;
            Slot = slot;
            Run();
            Slot = null;
            Inventory = null;
            Loadout = null;
            Item = null;
        }

        protected void DefaultEquip() {
            Inventory.Unequip(Slot);
            Inventory.EquipSlotInternal(Slot, Item);
        }

        protected void ForceSlotHasGivenType(EquipmentSlotType slot, params EquipmentType[] matching) {
            var current = Loadout[slot];
            if (current == null || !matching.Contains(current.EquipmentType)) {
                var better = Inventory.Items
                    .Where(item => !item.Locked && slot.Accept(item) && matching.Contains(item.EquipmentType))
                    .MaxBy(item => item.Quality.Priority, true);
                Loadout.EquipItem(slot, better);
            }
        }

        protected void EnsureSlotHasGivenType(EquipmentSlotType slot, params EquipmentType[] matching) {
            Item current = Loadout[slot];
            if (current != null && !matching.Contains(current.EquipmentType)) {
                Loadout.EquipItem(slot, null);
            }
        }
        
        protected void EquipInEach(EquipmentSlotType[] slots) {
            foreach (var slot in slots) {
                Loadout.InternalAssignItem(slot, Item);
            }
        }

        protected void Equip() {
            Loadout.InternalAssignItem(Slot, Item);
        }

        protected void Unequip(EquipmentSlotType slot) {
            Loadout.EquipItem(slot, null);
        }
        
        protected abstract void Run();
    }

    public class DefaultEquipping : EquippingPolicy {
        protected override void Run() {
            DefaultEquip();
        }
    }
    
    public class OneHandedEquipping : EquippingPolicy {
        protected override void Run() {
            Unequip(EquipmentSlotType.Quiver);
            if (Slot == EquipmentSlotType.OffHand) {
                EnsureSlotHasGivenType(EquipmentSlotType.MainHand, EquipmentType.OneHandedTypes);
            } else if (Slot == EquipmentSlotType.MainHand) {
                EnsureSlotHasGivenType(EquipmentSlotType.OffHand, EquipmentType.OneHandedTypes);
            } else if (Slot == EquipmentSlotType.AdditionalOffHand) {
                EnsureSlotHasGivenType(EquipmentSlotType.AdditionalMainHand, EquipmentType.OneHandedTypes);
            } else if (Slot == EquipmentSlotType.AdditionalMainHand) {
                EnsureSlotHasGivenType(EquipmentSlotType.AdditionalOffHand, EquipmentType.OneHandedTypes);
            }
            Equip();
        }
    }

    public class TwoHandedEquipping : EquippingPolicy {
        protected override void Run() {
            Unequip(EquipmentSlotType.Quiver);
            if (Slot == EquipmentSlotType.AdditionalMainHand || Slot == EquipmentSlotType.AdditionalOffHand) {
                EquipInEach(EquipmentSlotType.AdditionalHands);
            } else {
                EquipInEach(EquipmentSlotType.Hands);
            }
        }
    }
    
    public class BowEquipping : EquippingPolicy {
        protected override void Run() {
            if (Slot == EquipmentSlotType.AdditionalMainHand || Slot == EquipmentSlotType.AdditionalOffHand) {
                EquipInEach(EquipmentSlotType.AdditionalHands);
            } else {
                EquipInEach(EquipmentSlotType.Hands);
            }
            ForceSlotHasGivenType(EquipmentSlotType.Quiver, EquipmentType.Arrow);
        }
    }

    public class ArrowEquipping : EquippingPolicy {
        protected override void Run() {
            Equip();
        }
    }
}