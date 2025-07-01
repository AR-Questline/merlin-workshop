using System;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;

namespace Awaken.TG.Main.Utility.Animations {
    public enum WeaponRestriction : byte {
        None = 0,
        MainHand = 3,
        OffHand = 4,
        AdditionalMainHand = 5,
        AdditionalOffHand = 6,
        BothMainHands = 7,
        BothAdditionalHands = 8,
    }
    
    public static class AnimatorRestrictionExtension {
        public static bool Match(this WeaponRestriction restriction, CharacterHandBase hand) {
            var slotType = EquippedInSlotOfType(hand);
            return restriction switch {
                WeaponRestriction.None => true,
                WeaponRestriction.MainHand => slotType == EquipmentSlotType.MainHand,
                WeaponRestriction.OffHand => slotType == EquipmentSlotType.OffHand,
                WeaponRestriction.AdditionalMainHand => slotType == EquipmentSlotType.AdditionalMainHand,
                WeaponRestriction.AdditionalOffHand => slotType == EquipmentSlotType.AdditionalOffHand,
                WeaponRestriction.BothMainHands => slotType == EquipmentSlotType.MainHand || slotType == EquipmentSlotType.OffHand,
                WeaponRestriction.BothAdditionalHands => slotType == EquipmentSlotType.AdditionalMainHand || slotType == EquipmentSlotType.AdditionalOffHand,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        
        public static bool Match(this WeaponRestriction restriction, CastingHand hand) {
            return restriction switch {
                WeaponRestriction.None => true,
                WeaponRestriction.MainHand => hand is CastingHand.MainHand,
                WeaponRestriction.OffHand => hand is CastingHand.OffHand,
                WeaponRestriction.AdditionalMainHand => false,
                WeaponRestriction.AdditionalOffHand => false,
                WeaponRestriction.BothMainHands => true,
                WeaponRestriction.BothAdditionalHands => true,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        // === Helpers
        static EquipmentSlotType EquippedInSlotOfType(CharacterHandBase weapon) {
            if (weapon is CharacterFist { OffHand: true }) {
                return weapon.Item?.EquippedInSlotOfType == EquipmentSlotType.AdditionalMainHand
                    ? EquipmentSlotType.AdditionalOffHand
                    : EquipmentSlotType.OffHand;
            }
            return weapon.Item?.EquippedInSlotOfType;
        }
    }
}