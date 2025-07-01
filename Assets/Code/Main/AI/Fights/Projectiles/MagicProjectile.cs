using Awaken.TG.Assets;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.AI.Fights.Projectiles {
    [RequireComponent(typeof(Rigidbody))]
    public class MagicProjectile : DamageDealingProjectile {
        bool _exploded;
        float RaycastSphereSize => VisualData.raycastSphereSize;
        bool DealDirectDamageOnContact => _logicData.dealDirectDamageOnContact;
        bool ExplodeOnContact => _logicData.explodeOnContact;
        float SphereDamageDuration => VisualData.sphereDamageDuration;
        float SphereEndRadius => VisualData.sphereEndRadius;
        bool ExplodeOnEnviroHit => _logicData.explodeOnEnviroHit;
        bool ExplodeOnLifetimeEnd => _logicData.explodeOnLifetimeEnd;
        float DestructionSphereDamageDuration => VisualData.destructionSphereDamageDuration;
        float DestructionSphereEndRadius => VisualData.destructionSphereEndRadius;
        
        protected override float RaycastRadius => RaycastSphereSize;
        protected override DamageType DefaultDamageType => DamageType.MagicalHitSource;
        public override ItemTemplate ItemTemplate => SourceWeapon.Template;

        public void Detonate() {
            if (!ExplodeOnContact) {
                Log.Minor?.Error("Tried to detonate projectile that doesn't explode. " +
                                    "If you want to detonate this projectile set explodeOnContact to true");
                return;
            }
            
            Vector3 position = _rb.position;
            TrySpawnHitVfx(position, Vector3.zero);
            DealExplosionDamage(position, SphereDamageDuration, SphereEndRadius);
            DestroyProjectile(position);
            _exploded = true;
        }

        protected override void OnRaycastHit(HitResult hitResult) {
            if (hitResult.Prevented) {
                OnPrevent();
                return;
            }
            OnContact(hitResult);
        }

        protected override void OnContact(HitResult hitResult) {
            var hitCollider = hitResult.Collider;
            IAlive aliveHit = null;
            if (hitCollider != null) {
                aliveHit = VGUtils.TryGetModel<IAlive>(hitCollider.gameObject);
                
                if (_alivesHit.Contains(aliveHit)) {
                    return;
                }
                
                if (aliveHit == null && IsPiercing && VGUtils.TryGetModel<NpcDummy>(hitCollider.gameObject) != null) {
                    return;
                }
            }
            
            _deflected = false;
            
            Vector3 position;
            bool shouldDestroy;
            if (aliveHit == null) {
                position = hitResult.Point;
                shouldDestroy = true;
                OnEnvironmentHit(position, hitResult.Normal);
            } else {
                bool hasHitbox = aliveHit.Element<HealthElement>().HasHitbox(hitCollider);
                position = hasHitbox ? hitCollider.ClosestPoint(hitResult.Point) : hitResult.Point;
                shouldDestroy = OnTargetHit(hitCollider, position, aliveHit, hitResult.Normal);
                if (aliveHit is Hero h) {
                    if (((HeroHealthElement)h.HealthElement).HasPostponedDamage) {
                        SetKinematicWithStoredVelocity(true);
                        h.ListenToLimited(HeroHealthElement.Events.HeroParryPostponeWindowEnded, _ => OnContactEnding(hitCollider, position, aliveHit, shouldDestroy), this);
                        return;
                    }
                }
                
            }

            OnContactEnding(hitCollider, position, aliveHit, shouldDestroy);
        }

        void OnContactEnding(Collider hitCollider, Vector3 position, IAlive aliveHit, bool shouldDestroy) {
            if (_deflected) {
                return;
            }
            
            SendVSEvent(VSCustomEvent.OnContact, FireStrength, owner, hitCollider, position, aliveHit, SphereEndRadius);
            
            MakeNoiseOnContact(hitCollider);

            if (shouldDestroy && !_exploded) {
                DestroyProjectile(position);
            }
        }

        bool OnTargetHit(Collider collider, Vector3 position, IAlive aliveHit, Vector3 hitResultNormal) {
            // --- Damage Dealing
            if (ExplodeOnContact) {
                DealExplosionDamage(position, SphereDamageDuration, SphereEndRadius);
            } else if (DealDirectDamageOnContact && aliveHit != null) {
                DealDirectDamage(collider, position);
            }

            if (_deflected) {
                _alivesHit.Clear();
                return false;
            }
            
            // --- VFX
            bool spawnHitVFX = collider != null || aliveHit != null || ExplodeOnContact;
            if (spawnHitVFX) {
                TrySpawnHitVfx(position, hitResultNormal);
            } else {
                TrySpawnLifetimeEndVfx(position, hitResultNormal);
            }
            
            if (CanStillPierce && aliveHit != null) {
                Pierce();
                _alivesHit.Add(aliveHit);
                OnTargetPenetrated(aliveHit);
                return false;
            }

            return true;
        }
        
        void OnEnvironmentHit(Vector3 position, Vector3 hitResultNormal) {
            if (ExplodeOnEnviroHit) {
                SpawnDeathExplosion(position, hitResultNormal);
            } else {
                TrySpawnEnviroVfx(position, hitResultNormal);
            }
        }

        void SpawnDeathExplosion(Vector3 position, Vector3 hitResultNormal) {
            TrySpawnEnviroVfx(position, hitResultNormal);
            DealExplosionDamage(position, DestructionSphereDamageDuration, DestructionSphereEndRadius);
        }

        protected void DestroyProjectile(Vector3 position) {
            CustomTrailHolderBasedDestroy(position).Forget();
            
            _destroyed = true;
            _rb.isKinematic = true;
            
            BeforeGameObjectDestroy();
            Destroy(gameObject);
        }
        
        void DealDirectDamage(Collider collider, Vector3 position) {
            DamageParameters parameters = DamageParameters.Default;
            parameters.PoiseDamage = _poiseDamage;
            parameters.ForceDamage = _forceDamage;
            parameters.RagdollForce = _ragdollForce;
            parameters.ForceDirection = _transform.forward;
            parameters.Position = position;
            parameters.Direction = parameters.ForceDirection;
            parameters.DamageTypeData = _damageTypeData;
            parameters.IsFromProjectile = true;
            
            DamageUtils.TryDoDamage(null, collider, RawDamageData, Owner, ref parameters, item: SourceWeapon, this);
        }
        
        void DealExplosionDamage(Vector3 explosionPosition, float duration, float endRadius) {
            var parameters = DamageParameters.Default;
            parameters.DamageTypeData = _damageTypeData;
            parameters.PoiseDamage = _poiseDamage;
            parameters.ForceDamage = _forceDamage;
            parameters.RagdollForce = _ragdollForce;
            parameters.Inevitable = false;
            
            var sphereDamageParameters = new SphereDamageParameters {
                rawDamageData = RawDamageData,
                duration = duration,
                endRadius = endRadius,
                hitMask = HitMask,
                defaultDelay = 0f,
                item = SourceWeapon,
                baseDamageParameters = parameters
            };
            Owner?.AddElement(new DealDamageInSphereOverTime(sphereDamageParameters, explosionPosition));
        }
        
        protected override void OnLifetimeEnd() {
            GameObject go = gameObject;
            SendVSEvent(VSCustomEvent.OnDestruction, FireStrength, owner, SphereEndRadius);
            Vector3 position = _rb.position;
            if (ExplodeOnEnviroHit && ExplodeOnLifetimeEnd) {
                SpawnDeathExplosion(position, Vector3.zero);
            } else {
                TrySpawnLifetimeEndVfx(position, Vector3.zero);
            }
            DestroyProjectile(position);
        }
        
        protected virtual void OnTargetPenetrated(IAlive alive) {}
    }
}
