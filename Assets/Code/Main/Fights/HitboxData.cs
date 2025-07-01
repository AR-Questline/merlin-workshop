using System;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.Utility.Extensions;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.Fights {
    [Serializable]
    public struct HitboxData {
        public static readonly HitboxData Default = new() {
            damageMultiplier = 1,
            armorType = ArmorType.None,
            canBeHitByDamageOfType = WeaponDependentDamageType.All,
            multiplyDamageOfType = WeaponDependentDamageType.All,
            preventDamageOfType = WeaponDependentDamageType.None,
            reflectArrows = false,
        };
        
        [RichEnumExtends(typeof(ArmorType))]
        public RichEnumReference armorType;
        
        public float damageMultiplier;
        
        [OnValueChanged(nameof(ValidateAndCorrectCanBeHit))] public WeaponDependentDamageType canBeHitByDamageOfType;
        [OnValueChanged(nameof(ValidateAndCorrectCanBeHit))] public WeaponDependentDamageType multiplyDamageOfType;
        [OnValueChanged(nameof(ValidateAndCorrectCanBeHit))] public WeaponDependentDamageType preventDamageOfType;
        
        [ShowIf(nameof(BlocksArrows))] public bool reflectArrows;
        
        public ArmorType ArmorType => armorType.EnumAs<ArmorType>() ?? ArmorType.None;
        bool BlocksArrows => preventDamageOfType.HasFlagFast(WeaponDependentDamageType.Ranged);
        
        public readonly float DamageMultiplier(Item itemDealingDamage) {
            return CanAccept(itemDealingDamage, multiplyDamageOfType) ? damageMultiplier : 1;
        }
        
        public readonly bool CanBeHit(Item itemDealingDamage) {
            return CanAccept(itemDealingDamage, canBeHitByDamageOfType);
        }
        
        public readonly bool CanPreventDamage(Item itemDealingDamage) {
            return CanAccept(itemDealingDamage, preventDamageOfType);
        }

        readonly bool CanAccept(Item itemDealingDamage, WeaponDependentDamageType acceptedDamageType) {
            if (acceptedDamageType == WeaponDependentDamageType.None) return false;

            if (itemDealingDamage == null) return true;

            if ((itemDealingDamage.IsRanged || itemDealingDamage.IsThrowable) && acceptedDamageType.HasFlagFast(WeaponDependentDamageType.Ranged)) {
                return true;
            }

            if (itemDealingDamage.IsMagic && acceptedDamageType.HasFlagFast(WeaponDependentDamageType.Magic)) {
                return true;
            }

            if (itemDealingDamage.IsMelee && acceptedDamageType.HasFlagFast(WeaponDependentDamageType.Melee)) {
                return true;
            }

            return false;
        }
        
        public void ValidateAndCorrectCanBeHit() {
            canBeHitByDamageOfType |= multiplyDamageOfType | preventDamageOfType;
        }
    }
}