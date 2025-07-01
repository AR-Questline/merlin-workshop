using System;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.States.Combat;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.Combat;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Main.AI.Combat.Behaviours.CustomBehaviours {
    [Serializable]
    public partial class PreventDamageAndChargeExplosionBehaviour : PreventDamageBehaviour {
        const string VfxChargeProperty = "ChargeValue";
        
        [ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)]
        public ShareableARAssetReference chargeVFX;
        [SerializeField] ChargeThreshold[] chargeThresholds = Array.Empty<ChargeThreshold>();
        [SerializeField] NpcDamageData damageData = NpcDamageData.DefaultMagicAttackData;
        [SerializeField] float explosionForceDamage = 5;
        [SerializeField] float explosionDuration = 0.5f;
        [SerializeField] float explosionRange = 9;
      	[SerializeField] float ragdollForce = 5;
  		[SerializeField, InfoBox("If set to nothing will use NpcHitMask from Template")] LayerMask explosionHitMask;

        int _chargeHits;
        int _currentThreshold;
        IPooledInstance _chargeVFXInstance;
        VisualEffect _chargeVFX;
        VFXBodyMarker _vfxBodyMarker;
        
        protected override NpcStateType StateType => _isPreparing ? animatorPrepareStateType : GetStateType();
        protected override bool ListenToDamagePrevented => true;
        protected override bool HideLoopState => true;

        int GetThresholdId(int amountOfHits) {
            for (int i = chargeThresholds.Length - 1; i > 0; i--) {
                if (chargeThresholds[i].hitsToEnter <= amountOfHits) {
                    return i;
                }
            }
            return 0;
        }
        
        ChargeThreshold GetThreshold(int amountOfHits) {
            return chargeThresholds[GetThresholdId(amountOfHits)];
        }

        NpcStateType GetStateType() { 
            return GetThreshold(_chargeHits).animationState;
        }
        
        protected override void OnPreventionStarted() {
            base.OnPreventionStarted();
            _chargeHits = 0;
            _currentThreshold = 0;
            CreateChargeVFX(chargeThresholds[0].chargeVFXValue).Forget();
        }
        
        protected override void OnDamagePrevented(Damage damage) {
            base.OnDamagePrevented(damage);
            _chargeHits++;
            int newThreshold = GetThresholdId(_chargeHits);
            if (newThreshold != _currentThreshold) {
                _currentThreshold = newThreshold;
                var animatorState = chargeThresholds[_currentThreshold].animationState;
                if (CurrentAnimatorState is PreventDamageStateLoop loopState) {
                    loopState.Leave(animatorState);
                } else {
                    if (CurrentAnimatorState is AttackGeneric) {
                        ParentModel.GenericAttackData = GenericAttackData.Default;
                    }
                    ParentModel.SetAnimatorState(animatorState);
                }
                
                if (AttackGeneric.IsGenericAttack(animatorState)) {
                    ParentModel.GenericAttackData = new GenericAttackData() {
                        canBeExited = false,
                        canUseMovement = false,
                        isLooping = true
                    };
                }
                
                UpdateChargeVFXValue(chargeThresholds[_currentThreshold].chargeVFXValue);
                if (chargeThresholds[_currentThreshold].explodeOnEnter) {
                    Exit(animatorInterruptedStateType);
                }
            }
        }

        protected override void Exit(NpcStateType exitState) {
            Explode();
            DestroyChargeVFX();
            switch (CurrentAnimatorState) {
                case AttackGeneric attackGeneric:
                    ParentModel.GenericAttackData = GenericAttackData.Default;
                    ParentModel.SetAnimatorState(exitState);
                    break;
                case PreventDamageStateLoop loopState:
                    loopState.Leave(exitState);
                    break;
                default:
                    var targetState = GetThreshold(_chargeHits).animationState;
                    if (CurrentAnimatorState.Type == targetState) {
                        ParentModel.SetAnimatorState(exitState);
                    }
                    break;
            }
            _isExiting = true;
        }

        
        protected override void BehaviourExit() {
            _chargeHits = 0;
            _currentThreshold = 0;
            // If Behaviour is stopped or interrupted externally we can't remain in this unstoppable states
            switch (CurrentAnimatorState) {
                case AttackGeneric:
                    Explode();
                    ParentModel.GenericAttackData = GenericAttackData.Default;
                    break;
                case PreventDamageStateLoop loopState:
                    Explode();
                    loopState.Leave(NpcStateType.ChargeInterrupt);
                    break;
            }
            DestroyChargeVFX();
            base.BehaviourExit();
        }

        void Explode() {
            var threshold = GetThreshold(_chargeHits);
            if (!threshold.canExplode) {
                return;
            }

            // TODO: This should be standardized the same way SolarBeamData is standardized, just add ExplosionData to Behaviour and than create Explosion from there
			float forceDamage = explosionForceDamage * threshold.forceMultiplier;
            float radius = explosionRange * threshold.radiusMultiplier;
            Vector3 position = ParentModel.Coords;
            LayerMask mask = explosionHitMask == 0 ? ParentModel.NpcElement.HitLayerMask : explosionHitMask;
            var explosionVFX = threshold.explosionVFX;
            
            var parameters = DamageParameters.Default;
            parameters.ForceDamage = forceDamage;
            parameters.RagdollForce = ragdollForce;
            parameters.DamageTypeData = damageData.GetDamageTypeData(Npc).GetRuntimeData();
            parameters.Inevitable = true;
            
            SphereDamageParameters sphereDamageParameters = new() {
                rawDamageData = damageData.GetRawDamageData(Npc, threshold.damageMultiplier),
                duration = explosionDuration,
                endRadius = radius,
                hitMask = mask,
                item = ParentModel.StatsItem,
                baseDamageParameters = parameters
            };
            DealDamageInSphereOverTime sphere = new(sphereDamageParameters, position, Npc);
            ParentModel.ParentModel.AddElement(sphere);
            
            // --- VFX
            position = Ground.SnapToGround(position);
            if (explosionVFX.IsSet) {
                PrefabPool.InstantiateAndReturn(explosionVFX, position, Quaternion.identity).Forget();
            }
        }
        
        async UniTaskVoid CreateChargeVFX(float defaultValue) {
            if (chargeVFX.IsSet) {
                _vfxBodyMarker ??= Npc.ParentTransform.GetComponentInChildren<VFXBodyMarker>();
                _vfxBodyMarker.MarkBeingUsed();
                _chargeVFXInstance = await PrefabPool.Instantiate(chargeVFX, Vector3.zero, Quaternion.identity, _vfxBodyMarker.transform, Vector3.one);
                if (_chargeVFXInstance.Instance is {} instance) {
                    _chargeVFX = instance.GetComponentInChildren<VisualEffect>();
                    _chargeVFX.SetFloat(VfxChargeProperty, defaultValue);
                }
            }
        }

        void UpdateChargeVFXValue(float value) {
            if (_chargeVFX != null) {
                _chargeVFX.SetFloat(VfxChargeProperty, value);
            }
        }
        
        void DestroyChargeVFX() {
            if (_chargeVFXInstance != null) {
                _chargeVFXInstance?.Return();
                _chargeVFXInstance = null;
                _vfxBodyMarker.MarkBeingUnused();
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            DestroyChargeVFX();
            base.OnDiscard(fromDomainDrop);
        }
        
        [Serializable]
        internal struct ChargeThreshold {
            public int hitsToEnter;
            public bool canExplode;
            [ShowIf(nameof(canExplode))] public NpcStateType animationState;
            [ShowIf(nameof(canExplode))] public float damageMultiplier;
            [ShowIf(nameof(canExplode))] public float forceMultiplier;
            [ShowIf(nameof(canExplode))] public float radiusMultiplier;
            [ShowIf(nameof(canExplode))] public float chargeVFXValue;
            [ShowIf(nameof(canExplode))] public bool explodeOnEnter;
            [ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)]
            [ShowIf(nameof(canExplode))] public ShareableARAssetReference explosionVFX;
        }
    }
}