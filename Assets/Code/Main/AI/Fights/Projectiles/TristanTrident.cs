using Awaken.TG.Main.AI.Combat.Attachments.Customs;
using Awaken.TG.Main.Fights.NPCs;
using UnityEngine;

namespace Awaken.TG.Main.AI.Fights.Projectiles {
    public class TristanTrident : MagicProjectile {
        protected override void DestroyProjectile(Vector3 position) {
            SpawnLocation(position);
            base.DestroyProjectile(position);
        }

        void SpawnLocation(Vector3 position) {
            if (Owner is NpcElement { EnemyBaseClass: Tristan tristan }) {
                tristan.OnTridentProjectileDestroy(this, position);
            }
        }
    }
}