using Awaken.TG.Main.AI.Combat.Attachments.Customs;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.Utility.Maths;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.AI.Fights.Projectiles {
    public class TristanStalagmite : MagicProjectile {
        protected override void DestroyProjectile(Vector3 position) {
            DelayDestroy(position).Forget();
        }

        async UniTaskVoid DelayDestroy(Vector3 position) {
            _destroyed = true;
            _rb.isKinematic = true;
            
            if (!await SpawnLocation(position)) {
                return;
            }
            if (this != null) {
                base.DestroyProjectile(position);
            }
        } 

        async UniTask<bool> SpawnLocation(Vector3 position) {
            if (Owner is NpcElement { EnemyBaseClass: Tristan tristan }) {
                return await tristan.StalagmiteFell(this, position);
            }
            return true;
        }

        protected override DamageParameters GetDirectHitParameters(Collider collider, Vector3 position) {
            var parameters = base.GetDirectHitParameters(collider, position);
            parameters.ForceDirection = (position.X0Z() - _rb.position.X0Z() + 0.33f * Vector3.up).normalized;
            return parameters;
        }
    }
}