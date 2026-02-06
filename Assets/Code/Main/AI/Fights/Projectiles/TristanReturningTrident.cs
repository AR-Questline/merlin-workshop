using Awaken.TG.Main.AI.Combat.Attachments.Customs;
using Awaken.TG.Main.Fights.NPCs;
using UnityEngine;

namespace Awaken.TG.Main.AI.Fights.Projectiles {
    public class TristanReturningTrident : HomingProjectile {

        protected override void ProcessFixedUpdate(float deltaTime) {
            base.ProcessFixedUpdate(deltaTime);
            if (TryGetTargetPosition(out var targetPosition) && Vector3.Distance(_rb.position, targetPosition) <= 0.5f) {
                DestroyProjectile(_rb.position);
            }
        }
        
        protected override void DestroyProjectile(Vector3 position) {
            PickUp();
            base.DestroyProjectile(position);
        }

        void PickUp() {
            if (Owner is NpcElement { EnemyBaseClass: Tristan tristan }) {
                tristan.OnTridentReturnProjectileDestroy(this);
            }
        }
    }
}