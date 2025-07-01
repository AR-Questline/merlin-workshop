using System;
using Awaken.TG.Main.Character;
using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Main.Fights.DamageInfo {
    public readonly struct DamageOutcome {
        public Damage Damage { get; }
        public Vector3 Position { get; }
        public Vector3 RagdollForce => (Damage.ForceDirection ?? Vector3.zero) * Damage.RagdollForce;
        
        [Obsolete("Use '" + nameof(FinalAmount) + "' instead"), UsedImplicitly, UnityEngine.Scripting.Preserve] // Used by VS
        public float Amount => Damage.Amount;
        public float FinalAmount { get; }
        public float Radius => Damage.Radius;
        public ICharacter Attacker => Damage.DamageDealer;
        public Collider HitCollider => Damage.HitCollider;
        public IAlive Target => Damage.Target;
        public DamageModifiersInfo DamageModifiersInfo { get; }

        public DamageOutcome(Damage damage, Vector3 position, DamageModifiersInfo damageModifiers, float finalDamageValue) {
            Damage = damage;
            Position = position;
            DamageModifiersInfo = damageModifiers;
            FinalAmount = finalDamageValue;
        }

        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public static DamageModifiersInfo GetDamageModifiersInfo(DamageOutcome Input) => Input.DamageModifiersInfo;
    }
}