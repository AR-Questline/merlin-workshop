using Awaken.TG.Main.AI.Fights.Projectiles;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    public interface IApplicableToProjectile {
        void ApplyToProjectile(GameObject gameObject, Projectile projectile);
    }
}