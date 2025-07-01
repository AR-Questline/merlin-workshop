using System;

namespace Awaken.TG.Main.Fights.DamageInfo {
    [Serializable]
    public struct DamageParametersData {
        public bool ignoreArmor;
        public bool inevitable;
        public bool canBeCritical;
        public DamageType sourceType;
        public DamageSubType subtype;
        public bool isPrimary;
        public bool isDamageOverTime;
        public float armorPenetration;
        public float poiseDamage;
        public float forceDamage;
        public float ragdollForce;

        public static DamageParametersData DefaultSolarBeam => new () {
            canBeCritical = false,
            sourceType = DamageType.MagicalHitSource,
            isPrimary = true,
            isDamageOverTime = false,
        };

        public DamageParameters Get() => new() {
            IgnoreArmor = ignoreArmor,
            Inevitable = inevitable,
            CanBeCritical = canBeCritical,
            DamageTypeData = new RuntimeDamageTypeData(sourceType, subtype),
            IsPrimary = isPrimary,
            IsDamageOverTime = isDamageOverTime,
            ArmorPenetration = armorPenetration,
            PoiseDamage = poiseDamage,
            ForceDamage = forceDamage,
            RagdollForce = ragdollForce,
        };
    }
}