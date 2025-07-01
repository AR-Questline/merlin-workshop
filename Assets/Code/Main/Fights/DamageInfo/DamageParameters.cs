using Awaken.Utility;
using System.ComponentModel;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Utility.Attributes;
using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Main.Fights.DamageInfo {
    public partial struct DamageParameters {
        public ushort TypeForSerialization => SavedTypes.DamageParameters;

        [Saved] public bool Critical { get; set; }
        [Saved] public bool IgnoreArmor { get; set; }
        [Saved] public bool Inevitable { get; set; }
        [Saved] public bool CanBeCritical { get; set; }
        [Saved] public KnockdownType KnockdownType { get; set; }
        [Saved] public float KnockdownStrength { get; set; }
        [Saved] public RuntimeDamageTypeData DamageTypeData { get; set; }
        [Saved] public StatusDamageType StatusDamageType { get; set; }
        /// <summary>
        /// True, if damage come from original skill, otherwise false
        /// </summary>
        [Saved] public bool IsPrimary { get; set; }
        [Saved] public bool IsDamageOverTime { get; set; }
        [Saved] public float ArmorPenetration { get; set; }
        [Saved] public bool IsHeavyAttack { get; set; }
        [Saved] public bool IsDashAttack { get; set; }
        [Saved] public bool IsPush { get; set; }
        [Saved] public bool IsBackStab { get; set; }
        [Saved] public bool IsFromProjectile { get; set; }
        [Saved] public float BowDrawStrength { [UnityEngine.Scripting.Preserve] get; set; }
        
        [Description("Amount of Poise that will be applied to target.")]
        [Saved] public float PoiseDamage { get; set; }
        [Description("Amount of Force damage that will be applied to target. If target ForceStumbleThreshold reaches max value it will enter Stumble.")]
        [Saved] public float ForceDamage { get; set; }
        // --- Physical Parameters
        [Description("Force magnitude that will be applied if target will enter ragdoll.")]
        [Saved] public float RagdollForce { get; set; }
        [Description("The radius of the explosive damage. If 0 then a non-explosive force will be used.")]
        [Saved] public float Radius { get; set; }
        [Description("The position of the damage.")]
        [Saved] public Vector3? Position { get; set; }
        [Description("The position from the damage came.")]
        [Saved] public Vector3? DealerPosition { get; set; }
        [Description("The direction that the object took damage from.")]
        [Saved] public Vector3? Direction { get; set; }
        [Saved] public Vector3? ForceDirection { get; set; }
        
        public DamageType Type => DamageTypeData.SourceType;

        /// <summary>
        /// Use this to get correct default values
        /// </summary>
        public static readonly DamageParameters Default = new() {
            IsPrimary = true,
            DamageTypeData = new RuntimeDamageTypeData(DamageType.PhysicalHitSource),
            CanBeCritical = true,
            Critical = false,
            IgnoreArmor = false,
            Inevitable = false,
            PoiseDamage = 1,
            ForceDamage = 1,
            Radius = 0,
            IsBackStab = false,
            IsFromProjectile = false,
            IsDamageOverTime = false,
        };

        public static readonly DamageParameters PassiveDamageOverTime = new() {
            IsPrimary = false,
            DamageTypeData = new RuntimeDamageTypeData(DamageType.Status),
            CanBeCritical = false,
            Critical = false,
            IgnoreArmor = true,
            Inevitable = true,
            PoiseDamage = 0,
            ForceDamage = 0,
            Radius = 0,
            IsBackStab = false,
            IsFromProjectile = false,
            IsDamageOverTime = true,
        };

        [UnityEngine.Scripting.Preserve]
        public static readonly DamageParameters WyrdnessTravelling = new() {
            IsPrimary = false,
            DamageTypeData = new RuntimeDamageTypeData(DamageType.Environment, DamageSubType.Wyrdness),
            CanBeCritical = false,
            Critical = false,
            IgnoreArmor = true,
            Inevitable = true,
            PoiseDamage = 0,
            ForceDamage = 0,
            Radius = 0,
            IsBackStab = false,
            IsFromProjectile = false,
            IsDamageOverTime = true,
        };

        public static readonly DamageParameters ManaShieldRetaliation = new() {
            IsPrimary = false,
            DamageTypeData = new RuntimeDamageTypeData(DamageType.MagicalHitSource),
            CanBeCritical = false,
            Critical = false,
            IgnoreArmor = true,
            Inevitable = false,
            ForceDamage = 0,
            Radius = 0,
            IsBackStab = false,
            IsFromProjectile = false,
            IsDamageOverTime = false,
        };
        
        public static readonly DamageParameters MeleeRetaliation = new() {
            IsPrimary = false,
            DamageTypeData = new RuntimeDamageTypeData(DamageType.PhysicalHitSource),
            CanBeCritical = false,
            Critical = false,
            IgnoreArmor = false,
            Inevitable = false,
            ForceDamage = 0,
            Radius = 0,
            IsBackStab = false,
            IsFromProjectile = false,
            IsDamageOverTime = false,
        };
        
        // === Fluent API
        [Pure] [UnityEngine.Scripting.Preserve]
        public DamageParameters NotPrimary() {
            IsPrimary = false;
            return this;
        }

        [Pure] [UnityEngine.Scripting.Preserve]
        public DamageParameters NotCritical() {
            CanBeCritical = false;
            return this;
        }
 
        [Pure] [UnityEngine.Scripting.Preserve]
        public DamageParameters Piercing(bool enabled = true) {
            IgnoreArmor = enabled;
            return this;
        }

        [Pure] [UnityEngine.Scripting.Preserve]
        public DamageParameters ForcedCritical() {
            Critical = true;
            return this;
        }
    }
}