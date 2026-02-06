using System;
using Awaken.TG.Main.Character;
using UnityEngine;

namespace Awaken.TG.Main.AI.Fights.Projectiles {
    [Serializable]
    public struct ProjectileContactParams {
        public DamageDealingProjectile Projectile { get; }
        public Collider HitCollider { get; }
        public IAlive HitAlive { get; }
        
        public ProjectileContactParams(DamageDealingProjectile projectile, Collider hitCollider, IAlive hitAlive) {
            Projectile = projectile;
            HitCollider = hitCollider;
            HitAlive = hitAlive;
        }
    }
}