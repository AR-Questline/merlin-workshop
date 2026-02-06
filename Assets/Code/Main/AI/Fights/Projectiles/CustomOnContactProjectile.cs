using System;
using Awaken.CommonInterfaces;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.MVC;
using Awaken.Utility.Animations;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.AI.Fights.Projectiles {
    public class CustomOnContactProjectile : DamageDealingProjectile {
        public float destroyDelayAfterContact = 0.5f;
        
        Action _onContactAction;
        Action _onReleaseAction;
        
        // Explosion parameters
        bool _explodeOnContact;
        float _explosionRadius;
        float _explosionDuration;
        bool _explosionConfigured;

        protected override bool AllowMultiHit => false;

        protected override float RaycastRadius => VisualData.raycastSphereSize;

        protected override void Awake() {
            base.Awake();
            var renderers = GetComponentsInChildren<IWithUnityRepresentation>();
            foreach (var unityRepresentation in renderers) {
                unityRepresentation.SetUnityRepresentation(new IWithUnityRepresentation.Options {
                    linkedLifetime = true,
                    movable = true
                });
            }
        }

        public void AssignOnContactAction(Action action) {
            if (_onContactAction != null) {
                Log.Important?.Error("OnContact action is already assigned, cannot reassign.");
                return;
            }
            _onContactAction = action;
        }

        public void AssignReleaseAction(Action action) {
            if (_onReleaseAction != null) {
                Log.Important?.Error("OnRelease action is already assigned, cannot reassign.");
                return;
            }
            _onReleaseAction = action;
        }

        public void ConfigureExplosion(ExplosionConfig explosionConfig, bool explodeOnContact, ICharacter owner = null) {
            _explosionRadius = explosionConfig.radius;
            _explosionDuration = explosionConfig.duration;
            _explodeOnContact = explodeOnContact;
            _explosionConfigured = true;
            this.owner = owner;
            
            _forceDamage = explosionConfig.forceDamage;
            _poiseDamage = explosionConfig.poiseDamage;
        }

        public void DealExplosionDamage(IModel source, Vector3 explosionPosition) {
            if (!_explosionConfigured) return;

            var parameters = DamageParameters.Default;
            parameters.DamageTypeData = _damageTypeData;
            parameters.PoiseDamage = _poiseDamage;
            parameters.ForceDamage = _forceDamage;
            parameters.RagdollForce = _ragdollForce;
            parameters.Inevitable = false;
            if (_knockdownType != KnockdownType.None) {
                parameters.KnockdownType = _knockdownType;
                parameters.KnockdownStrength = _knockdownStrength;
            }
            
            var sphereDamageParameters = new SphereDamageParameters {
                rawDamageData = RawDamageData,
                duration = _explosionDuration,
                endRadius = _explosionRadius,
                hitMask = HitMask,
                defaultDelay = 0f,
                item = SourceWeapon,
                baseDamageParameters = parameters
            };

            if (source == null) {
                source = World.Add(new DealDamageInAreaPoint(explosionPosition));
                source.AddElement(new DealDamageInSphereOverTime(sphereDamageParameters, explosionPosition)).ListenTo(Model.Events.BeforeDiscarded, source.Discard);
            } else {
                source.AddElement(new DealDamageInSphereOverTime(sphereDamageParameters, explosionPosition));
            }
        }

        protected override void OnContact(HitResult hitResult) {
            TrySpawnHitVfx(Ground.SnapNpcToGround(hitResult.Point), hitResult.Normal);
            
            _onContactAction?.Invoke();
            _onContactAction = null;
            _destroyed = true;

            // Trigger explosion if configured
            if (_explodeOnContact && OwnerExists) {
                DealExplosionDamage(Owner, hitResult.Point);
            }

            DelayedDestroy(destroyDelayAfterContact).Forget();
        }

        protected override void OnLifetimeEnd() {
            _destroyed = true;
            DelayedDestroy(destroyDelayAfterContact).Forget();
        }
        
        async UniTaskVoid DelayedDestroy(float delay) {
            await UniTask.Delay(TimeSpan.FromSeconds(delay));
            if (this != null) {
                Destroy(gameObject);
            }
            _onReleaseAction?.Invoke();
            _onReleaseAction = null;
        }

        protected override IBackgroundTask OnDiscard() {
            _onReleaseAction?.Invoke();
            _onReleaseAction = null;
            return base.OnDiscard();
        }
    }
}