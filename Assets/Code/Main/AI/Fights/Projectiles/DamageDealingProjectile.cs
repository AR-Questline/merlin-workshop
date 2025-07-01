using System;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Utils;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.Enums;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Main.AI.Fights.Projectiles {
    [RequireComponent(typeof(Rigidbody))]
    public abstract class DamageDealingProjectile : Projectile {
        const float InitialThrowDistance = 3f;
        const float InitialThrowSafetyMargin = 0.25f;
        const float InitialThrowRadius = 0.3f;
        // === Fields
        [HideInEditorMode] public ICharacter owner;
        [SerializeField] RaycastCheck targetDetection;
        [SerializeField] FlyNoiseConfig flyNoiseConfig = new() {
            enabled = true, ellipseRadius = 1, width = 0.4f,
        };
        [SerializeField] float environmentHitNoiseRadius = 4;
        [SerializeField, RichEnumExtends(typeof(NoiseStrength))]
        RichEnumReference environmentHitNoiseStrength = NoiseStrength.Medium;
        [SerializeField] Transform visualParent;

        protected Transform _transform;
        protected Transform _firePoint;
        protected bool _destroyed;
        protected bool _deflected;
        protected bool _isPrimary = true;
        protected float _forceDamage;
        protected float _ragdollForce;
        protected float _poiseDamage;
        protected RuntimeDamageTypeData _damageTypeData;
        protected int _piercedCount;
        protected List<IAlive> _alivesHit = new();
        ProjectileOffsetData _positionOffset;
        float _offsetLerp = 1f;
        bool _offsetDisabledGravity;
        Vector3 _initialPosition;
        Vector3 _kinematicStoredVelocity;

        // === Projectile
        WeakModelRef<Item> _sourceWeapon;
        WeakModelRef<Item> _sourceProjectile;
        RawDamageData _originalRawDamageData;
        protected float LifeTime { get; private set; }
        protected bool Piercing { get; private set; }
        protected bool LimitedPiercing { get; private set; }
        protected int PiercingLimit { get; private set; }
        public bool IsPrimary => _isPrimary;
        float DefaultLifeTime => _logicData.lifetime;
        bool DefaultPiercing => _logicData.piercing;
        bool DefaultLimitedPiercing => _logicData.limitedPiercing;
        int DefaultPiercingLimit => _logicData.piercingLimit;
        bool UseBoxCast => VisualData.useBoxCast;
        Vector3 BoxCastSize => VisualData.boxCastSize;
        public ProjectileOffsetData PositionOffset => _positionOffset;
        public RuntimeDamageTypeData DamageTypeData => _damageTypeData;
        protected RawDamageData RawDamageData => new(_originalRawDamageData);
        public float FireStrength { get; private set; }
        public override Vector3 Velocity => _rb.isKinematic ? _kinematicStoredVelocity : _rb.linearVelocity;
        public override Transform VisualParent => visualParent;
        public override bool UsesGravity => _rb.useGravity || _offsetDisabledGravity;
        
        // === Properties
        protected virtual float RaycastRadius => 0.01f;
        protected LayerMask HitMask => targetDetection.accept;
        
        public override ICharacter Owner => OwnerExists ? owner : null;
        ICharacterView CharacterView => OwnerExists ? owner.CharacterView : null;

        public Item SourceProjectile => _sourceProjectile;
        public virtual ItemTemplate ItemTemplate => SourceProjectile.Template;
        public Item SourceWeapon => _sourceWeapon;
        protected virtual DamageType DefaultDamageType => DamageType.PhysicalHitSource;
        protected bool OwnerExists => owner is {HasBeenDiscarded: false};
        protected virtual bool AllowMultiHit => Piercing;
        protected bool CanStillPierce => Piercing && (!LimitedPiercing || _piercedCount < PiercingLimit);
        protected bool IsPiercing => Piercing;

        protected override void OnSetup(Transform firePoint) {
            _firePoint = firePoint;
            LifeTime = DefaultLifeTime;
            Piercing = DefaultPiercing;
            LimitedPiercing = DefaultLimitedPiercing;
            PiercingLimit = DefaultPiercingLimit;
            OnLifetimeStart();
        }

        protected virtual void OnLifetimeStart() {
            _initialPosition = _transform.position;
            if (_logicData.showLifetimeStartVFX && VisualData.lifetimeStartVFX is { IsSet: true }) {
                SpawnLifetimeStartVFX().Forget();
            }
        }
        
        protected override void OnFullyConfigured() {
            base.OnFullyConfigured();
            Owner?.Trigger(ICharacter.Events.OnFiredProjectile, this);
            ThrowInitialHeroRaycasts();
            AttachIdleAudio();
        }

        public void SetBaseDamageParams(Item weapon, Item projectile, float fireStrength, DamageTypeData damageTypeData = null, RawDamageData rawDamageData = null, float? damageAmount = null) {
            _sourceWeapon = weapon;
            _sourceProjectile = projectile;
            FireStrength = fireStrength;
            _damageTypeData = damageTypeData?.GetRuntimeData() ?? GetDamageTypeData(weapon, projectile);
            if (rawDamageData != null) {
                _originalRawDamageData = rawDamageData;
            } else if (damageAmount != null) {
                Log.Minor?.Warning($"Please notify Jakub Ka about case #2 still being in use for {LogUtils.GetDebugName(weapon)} {LogUtils.GetDebugName(projectile)} " +
                                    $"(owner: {LogUtils.GetDebugName(owner)} {LogUtils.GetDebugName(weapon?.Character)} {LogUtils.GetDebugName(weapon?.Character)})");
                _originalRawDamageData = new RawDamageData(damageAmount.Value);
            } else if (weapon?.IsThrowable ?? false) {
                Log.Minor?.Warning($"Please notify Jakub Ka about case #3 still being in use for {LogUtils.GetDebugName(weapon)} {LogUtils.GetDebugName(projectile)} " +
                $"(owner: {LogUtils.GetDebugName(owner)} {LogUtils.GetDebugName(weapon?.Character)} {LogUtils.GetDebugName(weapon?.Character)})");
                _originalRawDamageData = Damage.GetThrowableDamageDamageFrom(Owner, SourceWeapon, FireStrength);
            } else if (_damageTypeData.SourceType == DamageType.MagicalHitSource) {
                // Magic Projectiles etc. from hero 
                _originalRawDamageData = Damage.GetMagicProjectileDamageFrom(Owner, SourceWeapon, FireStrength);
            } else {
                _originalRawDamageData = Damage.GetBowDamageFrom(Owner, SourceWeapon, FireStrength, _sourceProjectile.Get());
            }
            
            _forceDamage = (weapon?.ItemStats?.ForceDamage.ModifiedValue ?? 0) + (projectile?.ItemStats?.ForceDamage.ModifiedValue ?? 0);
            _forceDamage *= fireStrength;
            _ragdollForce = (weapon?.ItemStats?.RagdollForce.ModifiedValue ?? 0) + (projectile?.ItemStats?.RagdollForce.ModifiedValue ?? 0);
            _ragdollForce *= fireStrength;
            _poiseDamage = (weapon?.ItemStats?.PoiseDamage.ModifiedValue ?? 0) + (projectile?.ItemStats?.PoiseDamage.ModifiedValue ?? 0);
            _poiseDamage *= fireStrength;
        }

        public override void SetVelocityAndForward(Vector3 velocity, ProjectileOffsetData? offsetData = null) {
            if (offsetData.HasValue) {
                var offset = offsetData.Value;
                offset.Init(velocity);
                _positionOffset = offset;
                _offsetLerp = 0f;
                if (offset.moveProjectileAtStart) {
                    this.transform.localPosition += offset.StartingOffset;
                    _initialPosition += offset.StartingOffset;
                }
                if (offset.disableGravityWhileOffseting && _rb.useGravity) {
                    _offsetDisabledGravity = true;
                    _rb.useGravity = false;
                }
                velocity += offset.InitialVelocity;
            } else {
                _offsetLerp = 1f;
            }
            _rb.linearVelocity = velocity;
            _transform.rotation = Quaternion.LookRotation(velocity, _transform.up);
        }

        public override void DeflectProjectile(DeflectedProjectileParameters parameters) {
            SetKinematicWithStoredVelocity(false);
            owner = parameters.newOwner;
            SetVelocityAndForward(parameters.newDirection);
            LifeTime = DefaultLifeTime;
            _deflected = true;
        }

        // Only Primary Projectiles trigger some effects (like duplicate arrows).
        public void SetAsSecondaryProjectile() {
            _isPrimary = false;
        }
        
        public void AddMultiplierToBaseDamage(float mult) {
            _originalRawDamageData.AddMultModifier(mult);
        }

        public void MultiplyBaseDamage(float mult) {
            _originalRawDamageData.MultiplyMultModifier(mult);
        }

        protected void OverrideBaseDamage(float damage) {
            _originalRawDamageData = new RawDamageData(damage);
        }

        // === Initialization
        void Awake() {
            _transform = transform;
        }

        protected override void Start() {
            if (!_destroyed) {
                base.Start();
            }
        }

        public void ChangeTargetLayers(LayerMask? acceptLayers = null, LayerMask? preventLayers = null) {
            if (acceptLayers.HasValue) {
                targetDetection.accept = acceptLayers.Value;
            }

            if (preventLayers.HasValue) {
                targetDetection.prevent = preventLayers.Value;
            }
        }

        protected void Pierce() {
            _piercedCount++;
        }

        public void IncreasePiercingLimit(int bonusLimit) {
            if (!Piercing) {
                Piercing = true;
                LimitedPiercing = true;
                PiercingLimit = bonusLimit;
                return;
            }
            if (Piercing && !LimitedPiercing) {
                return;
            }
            PiercingLimit += bonusLimit;
        }

        public void SetPiercing(bool activate, bool limitedPiercing = false, int piercingLimit = 1) {
            this.Piercing = activate;
            this.LimitedPiercing = limitedPiercing;
            this.PiercingLimit = piercingLimit;
        }

        RuntimeDamageTypeData GetDamageTypeData(Item weapon, Item projectile) {
            if (weapon != null) {
                if (projectile != null) {
                    return DamageTypeDataUtils.CombineWeaponAndAmmoType(weapon.ItemStats.DamageTypeData, projectile.ItemStats.DamageTypeData);
                } 
                return weapon.ItemStats.RuntimeDamageTypeData;
            }
            return new RuntimeDamageTypeData(DefaultDamageType);
        }

        async UniTaskVoid SpawnLifetimeStartVFX() {
            Vector3 position;
            Quaternion initRotation = _transform != null ? _transform.rotation : Quaternion.identity;
            var vfxTask = PrefabPool.InstantiateAndReturn(VisualData.lifetimeStartVFX, Vector3.zero, Quaternion.identity, VisualData.lifetimeStartVFXDuration, _firePoint, automaticallyActivate: false);
            var result = await vfxTask;
            if (result.Instance == null) {
                return;
            }

            if (!_initialized) {
                // position needs to be calculated after spawn because of projectile offset etc. moving the projectile at the start
                if (!await AsyncUtil.WaitUntil(this, () => _initialized)) {
                    return;
                }
            }
            
            if (_firePoint != null) {
                if (_logicData.lifeTimeStartVFXOnFirePointPosition) {
                    position = Vector3.zero;
                } else {
                    position = _firePoint.InverseTransformPoint(_initialPosition);
                }
            } else {
                position = _initialPosition;
                result.Instance.transform.localRotation = _transform != null ? _transform.rotation : initRotation;
            }
            result.Instance.transform.localPosition = position;
            result.Instance.SetActive(true);
        }

        void AttachIdleAudio() {
            var idleEventRef = ItemAudioType.ProjectileIdle.RetrieveFrom(SourceWeapon);
            if (idleEventRef.IsNull) {
                return;
            }

            // var idleAudioEmitter = gameObject.GetOrAddComponent<ARFmodEventEmitter>();
            // idleAudioEmitter.ChangeEvent(idleEventRef, false);
            // idleAudioEmitter.EventPlayTrigger = EmitterGameEvent.ObjectEnable;
            // idleAudioEmitter.EventStopTrigger = EmitterGameEvent.ObjectDisable;
            // if (gameObject.activeInHierarchy) {
            //     idleAudioEmitter.Play();
            // }
        }
        
        // === Processing
        protected override void ProcessUpdate(float deltaTime) {
            if (!_isSetup) {
                return;
            }
            
            if (!_destroyed) {
                ProcessRotation(deltaTime);
            }

            LifeTime -= deltaTime;
            if (LifeTime <= 0) {
                OnLifetimeEnd();
            }
        }

        protected virtual void ProcessRotation(float deltaTime) {
            if ((!_positionOffset.blockRotationWhileOffseting || _offsetLerp >= 1f) && _rb.linearVelocity != Vector3.zero) {
                _transform.rotation = Quaternion.LookRotation(_rb.linearVelocity, _transform.up);
            }
        }

        protected override void ProcessFixedUpdate(float deltaTime) {
            if (!_initialized || !_isSetup || _destroyed || _rb.isKinematic) {
                return;
            }

            MoveOffset(deltaTime);
            ThrowCast(deltaTime);
            TryMakeFlyNoise(deltaTime);
        }
        
        protected void MoveOffset(float deltaTime) {
            if (_offsetLerp >= 1f) {
                return;
            }
            
            float lastOffset = _offsetLerp;
            _offsetLerp += deltaTime * _positionOffset.DeltaTimeMultiplier;
            if (_offsetLerp > 1f) {
                _offsetLerp = 1f;
                if (_offsetDisabledGravity) {
                    _rb.useGravity = true;
                }
            }
            float negatedChange = EaseOffset(lastOffset) - EaseOffset(_offsetLerp);
            
            _rb.AddForce(_positionOffset.InitialVelocity * negatedChange, ForceMode.VelocityChange);
        }

        float EaseOffset(float value) {
            return DOVirtual.EasedValue(0, 1, value, _positionOffset.ease);
        }

        void ThrowInitialHeroRaycasts() {
            var previousAcceptMask = targetDetection.accept;
            targetDetection.accept = previousAcceptMask & ~RenderLayers.Mask.Player;
            
            if (_firePoint != null) {
                var deltaFromFirePoint = _transform.position - _firePoint.position;
                ThrowRaycast(_firePoint.position, deltaFromFirePoint, deltaFromFirePoint.magnitude, InitialThrowRadius);
            }
            
            if (!_destroyed) {
                var transformToShootFrom = _firePoint ? _firePoint : _transform;
                var shootForward = _transform.forward;
                var shootPoint = transformToShootFrom.position - shootForward * InitialThrowSafetyMargin;
                ThrowRaycast(shootPoint, shootForward, InitialThrowDistance, InitialThrowRadius);
            }
            
            targetDetection.accept = previousAcceptMask;
        }
        
        void ThrowCast(float deltaTime) {
            var velocity = _rb.linearVelocity;
            var velocityMagnitude = velocity.magnitude;
            Vector3 rbPosition = _rb.position;
            if (UseBoxCast) {
                ThrowBoxCast(rbPosition, velocity / velocityMagnitude, velocityMagnitude * deltaTime, BoxCastSize);
            } else {
                ThrowRaycast(rbPosition, velocity / velocityMagnitude, velocityMagnitude * deltaTime, RaycastRadius);
            }
            Debug.DrawRay(rbPosition, velocity * deltaTime, Color.red, 10f);
        }
        
        void ThrowBoxCast(Vector3 origin, Vector3 direction, float distance, Vector2 boxCastSize) {
            foreach (var hitResult in targetDetection.OverlapTargetsBox(origin, direction, distance, boxCastSize)) {
                CheckCastResult(hitResult);
                if (!AllowMultiHit || _destroyed) {
                    return;
                }
            }
        }

        void ThrowRaycast(Vector3 origin, Vector3 direction, float distance, float radius) {
            if (AllowMultiHit) {
                List<HitResult> hitResults = targetDetection.RaycastMultiHit(origin, direction, distance, radius);
                foreach (var hitResult in hitResults) {
                    CheckCastResult(hitResult);
                    if (!AllowMultiHit || _destroyed) {
                        return;
                    }
                }
            } else {
                HitResult hitResult = targetDetection.Raycast(origin, direction, distance, radius);
                CheckCastResult(hitResult);
            }
        }

        void CheckCastResult(HitResult hitResult) {
            if (hitResult.Prevented) {
                OnPrevent();
                return;
            }
            
            Collider hitCollider = hitResult.Collider;
            if (hitCollider == null) {
                return;
            }

            if (hitCollider.GetComponentInParent<ICharacterView>() != CharacterView) {
                OnRaycastHit(hitResult);
            }
        }
        
        void CheckCastResult(Collider collider) {
            if (collider == null) {
                return;
            }
            
            bool prevented = targetDetection.prevent.Contains(collider.gameObject.layer);
            if (prevented) {
                OnPrevent();
                return;
            }
            
            if (collider.GetComponentInParent<ICharacterView>() != CharacterView) {
                OnRaycastHit(new HitResult(collider, collider.ClosestPoint(_rb.position), Vector3.zero));
            }
        }

        void TryMakeFlyNoise(float deltaTime) {
            if (!flyNoiseConfig.enabled || _destroyed) {
                return;
            }
            if (!OwnerExists) {
                return;
            }

            foreach (var colliderHit in targetDetection.OverlapTargetsCapsule(_rb.position, _rb.linearVelocity, deltaTime, flyNoiseConfig.ellipseRadius, flyNoiseConfig.width)) {
                TryMakeNoiseForCollider(colliderHit).Forget();
            }
        }

        // === Collider Interactions & Dealing Damage
        protected virtual void OnRaycastHit(HitResult hitResult) {
            _transform.position = hitResult.Point;
            OnContact(hitResult);
            MakeNoiseOnContact(hitResult.Collider);
            VFXUtils.StopVfx(gameObject);
        }

        protected virtual void OnPrevent() {
            GameObject go = gameObject;
            SendVSEvent(VSCustomEvent.OnPrevent, FireStrength, owner);
        }
        protected abstract void OnContact(HitResult hitResult);
        protected abstract void OnLifetimeEnd();

        protected void SetKinematicWithStoredVelocity(bool state) {
            bool previousState = _rb.isKinematic;
            if (state && !previousState) {
                _kinematicStoredVelocity = _rb.linearVelocity;
                _rb.isKinematic = true;
            } else if (!state && previousState) {
                _rb.isKinematic = false;
                _rb.linearVelocity = _kinematicStoredVelocity;
                _kinematicStoredVelocity = Vector3.zero;
            }
            
        }
        
        protected Quaternion GetVfxRotation(Vector3 hitResultNormal) {
            if (hitResultNormal == Vector3.zero) {
                return Quaternion.Inverse(transform.rotation);
            }
            var up = Vector3.ProjectOnPlane(hitResultNormal, transform.forward);
            return Quaternion.LookRotation(hitResultNormal, up);
        }

        protected void TrySpawnHitVfx(Vector3 position, Vector3 hitResultNormal) {
            TrySpawnExplodingVfx(VisualData.hitVFX, position, hitResultNormal, VisualData.hitVFXDuration, VisualData.setVFXProjectileForwardPropertyOnHit).Forget();
        }
        
        protected void TrySpawnLifetimeEndVfx(Vector3 position, Vector3 hitResultNormal) {
            TrySpawnExplodingVfx(VisualData.lifetimeEndVFX, position, hitResultNormal, VisualData.lifetimeEndVFXDuration, VisualData.setVFXProjectileForwardPropertyOnLifetimeEnd).Forget();
        }
        
        protected void TrySpawnEnviroVfx(Vector3 position, Vector3 hitResultNormal) {
            TrySpawnExplodingVfx(VisualData.enviroVFX, position, hitResultNormal, VisualData.enviroVFXDuration, VisualData.setVFXProjectileForwardPropertyOnEnviro).Forget();
        }

        protected async UniTaskVoid TrySpawnExplodingVfx(ShareableARAssetReference explodingVfx, Vector3 position, Vector3 hitResultNormal, float duration, bool setProjectileForwardProperty) {
            if (explodingVfx is not { IsSet: true }) {
                return;
            }

            if (!setProjectileForwardProperty) {
                PrefabPool.InstantiateAndReturn(explodingVfx, position, GetVfxRotation(hitResultNormal), duration).Forget();
                return;
            }

            var forward = _transform.forward;
            var result = await PrefabPool.InstantiateAndReturn(explodingVfx, position, GetVfxRotation(hitResultNormal), duration);
            if (result.Instance != null) {
                result.Instance.GetComponentInChildren<VisualEffect>(true)?.SetVector3("ProjectileForward", forward);
            }
        }

        protected void MakeNoiseOnContact(Collider hitCollider) {
            ICharacter targetCharacter = VGUtils.TryGetModel<ICharacter>(hitCollider.gameObject);
            bool isEnvironmentHit = targetCharacter == null;
            if (isEnvironmentHit) {
                if (!OwnerExists) {
                    return;
                }

                var center = _rb.position;
                var strength = environmentHitNoiseStrength.EnumAs<NoiseStrength>().Value;
                var hearingNpcs = Services.Get<NpcGrid>().GetHearingNpcs(center, environmentHitNoiseRadius);
                foreach (var npc in hearingNpcs) {
                    if (npc.IsHostileTo(Owner)) {
                        AINoises.MakeNoise(environmentHitNoiseRadius, strength, true, center, npc.NpcAI);
                    }
                }
            } else {
                AINoises.MakeNoise(CharacterDealingDamage.SoundRange, NoiseStrength.Strong, true, _rb.position, targetCharacter);
            }
            _disableGizmos = true;
        }
        
        async UniTaskVoid TryMakeNoiseForCollider(Collider collider) {
            var npc = VGUtils.TryGetModel<NpcElement>(collider.gameObject);
            if (!npc) {
                return;
            }

            // Delay, so that NPC that is hit by the arrow doesn't receive alert from arrow's fly noise.
            if (!await AsyncUtil.DelayTime(npc, 0.5f)) {
                return;
            }
            
            if (!OwnerExists || !npc.IsHostileTo(Owner)) {
                return;
            }
            var npcAI = npc.NpcAI;
            npcAI.AlertStack.NewPoi(AlertStack.AlertStrength.Strong, Owner);
        }

        bool _disableGizmos = false;

        void OnDrawGizmosSelected() {
            if (!flyNoiseConfig.enabled || _disableGizmos) {
                return;
            }

            var rotation = transform.rotation;
            var forward = rotation * Vector3.forward;
            var up = rotation * Vector3.up;
            var right = rotation * Vector3.right;

            var startPoint = transform.position;
            var endPoint = startPoint + forward * flyNoiseConfig.width;

            Gizmos.DrawWireSphere(startPoint, flyNoiseConfig.ellipseRadius);
            Gizmos.DrawWireSphere(endPoint, flyNoiseConfig.ellipseRadius);
            Gizmos.DrawLine(startPoint, endPoint);

            var offset = up * flyNoiseConfig.ellipseRadius;
            var lineStart = startPoint + offset;
            var lineEnd = endPoint + offset;
            Gizmos.DrawLine(lineStart, lineEnd);

            offset = right * flyNoiseConfig.ellipseRadius;
            lineStart = startPoint + offset;
            lineEnd = endPoint + offset;
            Gizmos.DrawLine(lineStart, lineEnd);

            offset = -up * flyNoiseConfig.ellipseRadius;
            lineStart = startPoint + offset;
            lineEnd = endPoint + offset;
            Gizmos.DrawLine(lineStart, lineEnd);

            offset = -right * flyNoiseConfig.ellipseRadius;
            lineStart = startPoint + offset;
            lineEnd = endPoint + offset;
            Gizmos.DrawLine(lineStart, lineEnd);

            if (VisualData != null && UseBoxCast) {
                Matrix4x4 rotationMatrix = Matrix4x4.TRS(startPoint, rotation, Vector3.one);
                Gizmos.matrix = rotationMatrix;
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(BoxCastSize.x, BoxCastSize.y, 1));
            }
        }

        [Serializable]
        protected struct FlyNoiseConfig {
            public bool enabled;
            public float width;
            public float ellipseRadius;
        }
    }
}