using System.Threading;
using Awaken.TG.Assets;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Executions;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Locations.Views;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.Main.Utility.VFX;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.AI.Fights.Projectiles {
    [RequireComponent(typeof(Rigidbody))]
    public class Arrow : DamageDealingProjectile {
        [SerializeField] bool discardOnHit;

        protected virtual ItemTemplate BrokenItemTemplate => Services.Get<CommonReferences>().BrokenArrowItemTemplate;
        protected ItemTemplate _itemTemplate;
        ShareableARAssetReference EnvironmentArrowVisualPrefab => Services.Get<CommonReferences>().EnvironmentArrowVisualPrefab;
        ShareableARAssetReference EnvironmentArrowVisualPrefabWithCollisions => Services.Get<CommonReferences>().EnvironmentArrowVisualPrefabWithCollisions;
        LocationTemplate EnvironmentArrowLocationTemplate => Services.Get<CommonReferences>().EnvironmentArrowLocationTemplate;

        Transform _originalParent;
        LocationAttachedProjectiles _locationAttachedProjectiles;
        
        public override ItemTemplate ItemTemplate => _itemTemplate;

        public void SetItemTemplate(ItemTemplate arrowTemplate) {
            _itemTemplate = arrowTemplate;
        }
        
        protected override void OnRaycastHit(HitResult hitInfo) {
            base.OnRaycastHit(hitInfo);
            VGUtils.SendCustomEvent(hitInfo.Collider.gameObject, gameObject, VSCustomEvent.HitPosition, hitInfo.Point);
        }

        protected override void OnContact(HitResult hitResult) {
            Vector3 forward = Vector3.Slerp(_transform.forward, -hitResult.Normal, 0.2f);
            _transform.forward = forward;
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
            bool environmentHit = false;
            
            DamageParameters parameters = DamageParameters.Default;
            parameters.PoiseDamage = _poiseDamage;
            parameters.ForceDamage = _forceDamage;
            parameters.RagdollForce = _ragdollForce;
            parameters.ForceDirection = _transform.forward;
            parameters.Position = _transform.position;
            parameters.Direction = parameters.ForceDirection;
            parameters.DamageTypeData = _damageTypeData;
            parameters.IsFromProjectile = true;
            parameters.BowDrawStrength = FireStrength;

            bool dealtDamage = DamageUtils.TryDoDamage(aliveHit, hitCollider, RawDamageData, Owner, ref parameters, item: SourceWeapon, this);

            if (!dealtDamage) {
                OnEnvironmentHit(new EnvironmentHitData {
                    Location = VGUtils.TryGetModel<Location>(hitCollider.gameObject)?.LocationView,
                    Item = SourceWeapon,
                    Rigidbody = _rb,
                    Position = hitResult.Point,
                    Direction = forward,
                    RagdollForce = _ragdollForce
                }, parameters.BowDrawStrength);
                environmentHit = true;
                
            } else if (aliveHit is Hero h) {
                if (((HeroHealthElement)h.HealthElement).HasPostponedDamage) {
                    SetKinematicWithStoredVelocity(true);
                    h.ListenToLimited(HeroHealthElement.Events.HeroParryPostponeWindowEnded, _ => OnContactEnding(hitResult, hitCollider, aliveHit, environmentHit), this);
                    return;
                }
            }
                
            OnContactEnding(hitResult, hitCollider, aliveHit, environmentHit);
        }

        void OnContactEnding(HitResult hitResult, Collider hitCollider, IAlive aliveHit, bool environmentHit) {
            if (_deflected) {
                _alivesHit.Clear();
                return;
            }
            SendVSEvent(VSCustomEvent.OnContact, FireStrength, owner, hitCollider, _rb.position, aliveHit);

            if (aliveHit is NpcElement { IsAlwaysPiercedByArrows: true }) {
                PenetrateTarget(aliveHit);
                return;
            }
            
            if (aliveHit != null && CanStillPierce) {
                Pierce();
                PenetrateTarget(aliveHit);
                return;
            }
            
            _destroyed = true;
            OnTargetHit(hitResult, environmentHit, aliveHit);
        }

        void PenetrateTarget(IAlive aliveHit) {
            _alivesHit.Add(aliveHit);
            OnTargetPenetrated(aliveHit);
        }

        protected virtual void OnTargetHit(HitResult hitResult, bool environmentHit, IAlive aliveHit) {
            var colliderHit = hitResult.Collider;
            VHeroController heroController = colliderHit.GetComponentInParent<VHeroController>();
            bool releaseSelfOnHit = discardOnHit || heroController != null;
            var cancellationTokenSource = new CancellationTokenSource();
            CustomTrailHolderBasedDestroy(hitResult.Point, cancellationTokenSource, releaseSelfOnHit).Forget();
            if (releaseSelfOnHit) {
                _rb.isKinematic = true;
                ReleaseSelf();
            } else {
                Target.RemoveElementsOfType<Skill>();
                _originalParent = _transform.parent;
                if (aliveHit != null && !aliveHit.HasBeenDiscarded) {
                    ref readonly var hitbox = ref aliveHit.HealthElement.GetHitbox(colliderHit, out _);
                    if (hitbox.reflectArrows) {
                        _transform.SetParent(null);
                        ReflectArrowFromHitbox(hitResult);
                        ReleaseSelf(LifeTime).Forget();
                        Services.Get<MitigatedExecution>().Register(() => ChangeToPickable(null, cancellationTokenSource, true), null,
                            MitigatedExecution.Cost.Heavy, MitigatedExecution.Priority.Low, 0.5f);
                        return;
                    }
                }
                _rb.isKinematic = true;
                _transform.SetParent(environmentHit ? null : colliderHit.transform);
                ReleaseSelf(LifeTime).Forget();
                Location location = VGUtils.TryGetModel<Location>(colliderHit.gameObject);
                Services.Get<MitigatedExecution>().Register(() => ChangeToPickable(location, cancellationTokenSource, false), location,
                    MitigatedExecution.Cost.Heavy, MitigatedExecution.Priority.Low, 0.5f);
            }
        }

        void ReflectArrowFromHitbox(HitResult hitResult) {
            const float RandomAngleOffset = 30;
            const float ReflectForceMultiplier = 0.33f;
            const float ReflectForceCap = 8f;
            const float AfterReflectLinearDamping = 0.25f;
            
            const float AngularVelocityMin = 15f;
            const float AngularVelocityMax = 60f;
            const float AfterReflectAngularDamping = 0.4f;

            var linearVelocity = _rb.linearVelocity;
            var reflectForce = linearVelocity.magnitude * ReflectForceMultiplier;
            if (reflectForce > ReflectForceCap) reflectForce = ReflectForceCap;
            var reflectDirection = Vector3.Reflect(linearVelocity.normalized, hitResult.Normal);
            float angleOffsetX = RandomUtil.UniformFloat(-RandomAngleOffset, RandomAngleOffset);
            float angleOffsetY = RandomUtil.UniformFloat(-RandomAngleOffset, RandomAngleOffset);
            float angleOffsetZ = RandomUtil.UniformFloat(-RandomAngleOffset, RandomAngleOffset);
            reflectDirection = Quaternion.Euler(angleOffsetX, angleOffsetY, angleOffsetZ) * reflectDirection;
            if (IsDownwards(linearVelocity) && IsDownwards(reflectDirection)) {
                // If Both directions are downwards, reflect it to minimalize the change of forcing the arrow under ground;
                reflectDirection.y *= -1;
            }
            _rb.linearVelocity = reflectDirection.normalized * reflectForce;
            
            float angularVelocityX = RandomUtil.UniformFloat(AngularVelocityMin, AngularVelocityMax);
            float angularVelocityY = RandomUtil.UniformFloat(AngularVelocityMin, AngularVelocityMax);
            float angularVelocityZ = RandomUtil.UniformFloat(AngularVelocityMin, AngularVelocityMax);
            _rb.angularVelocity = new Vector3(angularVelocityX, angularVelocityY, angularVelocityZ);
            
            _rb.linearDamping = AfterReflectLinearDamping;
            _rb.angularDamping = AfterReflectAngularDamping;
            
            _rb.useGravity = true;
            DisableReflectedProjectile().Forget();

            static bool IsDownwards(Vector3 direction) {
                return direction.y < 0;
            }
        }

        async UniTaskVoid DisableReflectedProjectile() {
            if (!await AsyncUtil.WaitUntil(this, _rb.IsSleeping)) {
                return;
            }

            _rb.isKinematic = true;
            _rb.useGravity = false;
            foreach (var coll in _transform.GetComponentsInChildren<Collider>()) {
                if (coll.gameObject.layer == RenderLayers.Objects) {
                    coll.enabled = false;
                }
            }
        }
        
        protected virtual void OnTargetPenetrated(IAlive alive) {}

        protected override void OnLifetimeEnd() {
            ReleaseSelf(false);
        }

        async UniTaskVoid ReleaseSelf(float time) {
            if (await AsyncUtil.DelayTime(this, time)) {
                ReleaseSelf(false);
            }
        }
        
        public void ReleaseSelf(bool removeItemInInventory = true) {
            _locationAttachedProjectiles?.Release(this, removeItemInInventory);
            _locationAttachedProjectiles = null;

            if (this == null) {
                return;
            }

            if (!_originalParent) {
                _originalParent = _transform.parent;
            }
            if (_originalParent && _originalParent.TryGetComponent<VDynamicLocation>(out var dynamicLocation)) {
                dynamicLocation.ClearReferences();
            } else {
                BeforeGameObjectDestroy();
                Destroy(gameObject);
            }
        }

        void AfterInteractedWithPickable(CancellationTokenSource trailCancellationTokenSource) {
            trailCancellationTokenSource?.Cancel();
            ReleaseSelf();
        }

        void ChangeToPickable(Location location, CancellationTokenSource trailCancellationTokenSource, bool withCollisions) {
            LogMissingItemTemplateError();
            // --- Configure PickItemAction
            bool notBroken = _itemTemplate != null && RandomUtil.WithProbability(World.Only<HeroStats>().ArrowRetrievalChance);
            ItemSpawningDataRuntime itemSpawningDataRuntime = new(notBroken ? _itemTemplate : BrokenItemTemplate);
            
            // --- Spawn Location
            var pickItemLocation = EnvironmentArrowLocationTemplate.SpawnLocation(prefabReferenceOverride: withCollisions ? EnvironmentArrowVisualPrefabWithCollisions.Get() : EnvironmentArrowVisualPrefab.Get());
            pickItemLocation.MarkedNotSaved = true;

            if (location != null) {
                _locationAttachedProjectiles ??= LocationAttachedProjectiles.GetOrCreate(location);
                _locationAttachedProjectiles.AddProjectileLocation(this, pickItemLocation, itemSpawningDataRuntime);
            }

            PickItemAction pickItemAction = new(itemSpawningDataRuntime, false);
            pickItemLocation.ListenTo(Location.Events.AfterInteracted, _ => AfterInteractedWithPickable(trailCancellationTokenSource), this);
            pickItemLocation.OnVisualLoaded(t => {
                if (_transform == null) {
                    return;
                }
                LocationParent locationParent = t.GetComponentInParent<LocationParent>();
                locationParent.transform.SetParent(_transform, false);
                pickItemLocation.AddElement(pickItemAction);
            });
        }
        
        protected virtual void LogMissingItemTemplateError() {
            if (_itemTemplate == null) {
                Log.Important?.Error($"Arrow fired without ItemTemplate assigned! Fallback to broken arrow! {gameObject}");
            }
        }
        
        protected virtual void OnEnvironmentHit(EnvironmentHitData hitData, float bowDrawStrength) {
            // --- VFX
            Item item = hitData.Item;
            NpcDummy npcDummy = hitData.Location != null ? hitData.Location.Target?.TryGetElement<NpcDummy>() : null;
            SurfaceType surfaceType = npcDummy != null ? npcDummy.Template.SurfaceType : SurfaceType.HitStone;
            VFXManager.SpawnCombatVFX(SurfaceType.DamageArrow, surfaceType, hitData.Position, hitData.Direction, null, null);
            // --- Audio
            EventReference eventReference = ItemAudioType.MeleeHit.RetrieveFrom(item);
            SurfaceType audioSurfaceType = npcDummy != null ? npcDummy.Template.SurfaceType : SurfaceType.HitGround;
            
            FMODParameter[] parameters = { 
                audioSurfaceType, 
                new("ShootingForce", bowDrawStrength)
            };
            
            FMODManager.PlayOneShot(eventReference, hitData.Position, this, parameters);
        }
    }
}
