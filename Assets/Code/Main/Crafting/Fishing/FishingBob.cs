using Awaken.TG.Main.AI.Fights.Projectiles;
using Awaken.TG.Main.Character;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.Fishing {
    public class FishingBob : Projectile {
        public override ICharacter Owner => null;
        public override Vector3 Velocity => _rb.linearVelocity;
        public override Transform VisualParent => null;

        public override void SetVelocityAndForward(Vector3 velocity, ProjectileOffsetData? offsetData = null) {
            _rb.linearVelocity = velocity;
        }

        public override void DeflectProjectile(DeflectedProjectileParameters parameters) {
            // --- Fishing bob cannot be deflected
        }
    }
}