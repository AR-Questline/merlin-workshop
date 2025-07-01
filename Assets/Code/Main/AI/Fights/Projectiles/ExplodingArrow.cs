using Awaken.TG.Assets;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes.Combat;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.AI.Fights.Projectiles {
    public class ExplodingArrow : Arrow {
        bool DealDamageInSphereOnContact => _logicData.explodeOnContact;
        float SphereDamageDuration => VisualData.sphereDamageDuration;
        float SphereEndRadius => VisualData.sphereEndRadius;
        
        protected override float RaycastRadius => VisualData.raycastSphereSize;
        bool CanDealDamageInSphere => DealDamageInSphereOnContact && OwnerExists;
        
        protected override void OnContact(HitResult hitResult) {
            if (CanDealDamageInSphere) {
                OnTargetHit(hitResult, false, null);
            } else {
                base.OnContact(hitResult);
            }
        }

        protected override void OnTargetHit(HitResult hitResult, bool environmentHit, IAlive aliveHit) {
            _rb.isKinematic = true;
            Vector3 position = _rb.position;
            TrySpawnHitVfx(position, hitResult.Normal);

            if (CanDealDamageInSphere) {
                var parameters = DamageParameters.Default;
                parameters.DamageTypeData = _damageTypeData;
                parameters.ForceDamage = _forceDamage;
                parameters.RagdollForce = _ragdollForce;
                parameters.Inevitable = false;
                
                var sphereDamageParameters = new SphereDamageParameters {
                    rawDamageData = RawDamageData,
                    duration = SphereDamageDuration,
                    endRadius = SphereEndRadius,
                    hitMask = HitMask,
                    defaultDelay = 0f,
                    item = SourceWeapon,
                    baseDamageParameters = parameters
                };
                Owner.AddElement(new DealDamageInSphereOverTime(sphereDamageParameters, position));
            }

            CustomTrailHolderBasedDestroy(position).Forget();
            BeforeGameObjectDestroy();
            Destroy(gameObject);
        }

        protected override void OnLifetimeEnd() {
            OnTargetHit(default, false, null);
        }
    }
}