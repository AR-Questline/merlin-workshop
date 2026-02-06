using System;
using Awaken.TG.Main.Animations.FSM.Npc.States.General;
using Awaken.TG.Main.Animations.IK;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.Utility.Collections;
using Awaken.Utility.GameObjects;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements.DeathBehaviours {
    public abstract class PostponedRagdollBehaviourBase : MonoBehaviour, IDeathBehaviour {
        const float CacheRagdollBufferTime = 0.5f;
        const int CacheRagdollBufferFrames = 3;
        [SerializeField] RagdollEnableData ragdollData = RagdollEnableData.Default;

        Location _location;
        GameObject _alivePrefab;
        protected DeathRagdollNpcBehaviour _ragdollDeathBehaviour;
        
        public bool IsVisualInitialized { get; private set; }
        public abstract bool UseDeathAnimation { get; }
        public abstract NpcDeath.DeathAnimType UseCustomDeathAnimation { get; }

        protected virtual RagdollEnableData RagdollData => ragdollData;
        protected bool EnableRagdollAfterAnimation => RagdollData.enableRagdollAfterAnimation;
        protected float DelayToEnterRagdoll => RagdollData.DelayToEnterRagdoll;
        protected AnimToRagdollForceBufferType AnimToRagdollForceBuffer => RagdollData.animToRagdollForceBufferType;

        public virtual void OnVisualLoaded(DeathElement death, Transform transform) {
            IsVisualInitialized = true;
            var aliveTransform = transform.gameObject.FindChildRecursively("AlivePrefab", true);
            if (aliveTransform != null) {
                _alivePrefab = aliveTransform.gameObject;
            }
            _location = death?.ParentModel.ParentModel;
            _ragdollDeathBehaviour = death?.GetBehaviour<DeathRagdollNpcBehaviour>();
        }

        public virtual void OnDeath(DamageOutcome damageOutcome, Location dyingLocation) {
            if (_alivePrefab != null) {
                _alivePrefab.SetActive(false);
            }

            if (EnableRagdollAfterAnimation) {
                if (_ragdollDeathBehaviour == null) {
                    return;
                }
                if (_ragdollDeathBehaviour.IsRagdollInProgress) {
                    _ragdollDeathBehaviour.DisableRagdoll();
                }
                EnterRagdollAfterAnimationStarted().Forget();
            }
        }

        public virtual void AfterOnDeath(DamageOutcome damageOutcome, bool isUsingCustomDeathAnimation, NpcDeath.DeathAnimType deathAnimType) { }

        protected virtual void OnRagdollEnabled() { }

        async UniTaskVoid EnterRagdollAfterAnimationStarted() {
            if (!await AsyncUtil.DelayFrame(_location, 1)) {
                return;
            }
            var animancer = transform.GetComponentInChildren<ARNpcAnimancer>(true);
            if (animancer != null) {
                while (animancer.Layers[(int) ARNpcAnimancer.NpcLayers.Overrides].CurrentState is { NormalizedTime: 0f }) {
                    if (!await AsyncUtil.DelayFrame(_location, 1)) {
                        return;
                    }
                }

                var animator = transform.GetComponentInChildren<Animator>();
                if (animator != null && animator.cullingMode is {} cullingCache && cullingCache != AnimatorCullingMode.AlwaysAnimate) {
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    animancer.Evaluate();
                    animator.cullingMode = cullingCache;
                } else {
                    animancer.Evaluate();
                }
            }

            EnterRagdollAfterDelay().Forget();
        }
        
        async UniTaskVoid EnterRagdollAfterDelay() {
            if (AnimToRagdollForceBuffer == AnimToRagdollForceBufferType.None) {
                await AsyncUtil.DelayTimeWithModelTimeScale(_location, DelayToEnterRagdoll);
                _ragdollDeathBehaviour.EnableDeathRagdoll();
                OnRagdollEnabled();
                return;
            }

            var positions = default(UnsafeArray<float3>);
            var rotations = default(UnsafeArray<quaternion>);
            var cacheTime = default(float);
            var cacheTimeTimeScale = default(float);
            if (AnimToRagdollForceBuffer == AnimToRagdollForceBufferType.OneFrame) {
                (positions, rotations, cacheTime, cacheTimeTimeScale) = await CacheAfterFrames(DelayToEnterRagdoll, 1);
            } else if (AnimToRagdollForceBuffer == AnimToRagdollForceBufferType.ConstAmountOfFrames) {
                (positions, rotations, cacheTime, cacheTimeTimeScale) = await CacheAfterFrames(DelayToEnterRagdoll, CacheRagdollBufferFrames);
            } else if (AnimToRagdollForceBuffer == AnimToRagdollForceBufferType.ConstTimeBuffer) {
                if (DelayToEnterRagdoll <= CacheRagdollBufferTime) {
                    (positions, rotations, cacheTime, cacheTimeTimeScale) = await CacheAfterTime(0, DelayToEnterRagdoll);
                } else {
                    (positions, rotations, cacheTime, cacheTimeTimeScale) = await CacheAfterTime(DelayToEnterRagdoll - CacheRagdollBufferTime, CacheRagdollBufferTime);
                }
            }

            if (positions.IsCreated == false) {
                return;
            }

            float avgTimeScale = (cacheTimeTimeScale + _location.GetTimeScale()) * 0.5f;

            var positionsVelocity = new UnsafeArray<float3>(positions.Length, ARAlloc.Temp);
            var rotationsVelocity = new UnsafeArray<float3>(rotations.Length, ARAlloc.Temp);

            var elapsedTime = (Time.unscaledTime - cacheTime) / avgTimeScale;
            _ragdollDeathBehaviour.RagdollController.CacheRigidbodyTransforms(ARAlloc.Temp, out var currentPositions, out var currentRotations);

            new CalculateVelocitiesJob {
                previousPositions = positions,
                currentPositions = currentPositions,
                previousRotations = rotations,
                currentRotations = currentRotations,
                elapsedTime = elapsedTime,

                outPositionsVelocity = positionsVelocity,
                outRotationsVelocity = rotationsVelocity
            }.Run();

            positions.Dispose();
            currentPositions.Dispose();
            rotations.Dispose();
            currentRotations.Dispose();

            _ragdollDeathBehaviour.EnableDeathRagdoll(positionsVelocity, rotationsVelocity);

            positionsVelocity.Dispose();
            rotationsVelocity.Dispose();

            OnRagdollEnabled();
        }

        async UniTask<(UnsafeArray<float3>, UnsafeArray<quaternion>, float, float)> CacheAfterFrames(float normalDelay, int frameDelay) {
            if (!await AsyncUtil.DelayTimeWithModelTimeScale(_location, normalDelay)) {
                return default;
            }
            _ragdollDeathBehaviour.RagdollController.CacheRigidbodyTransforms(ARAlloc.Persistent, out var positions, out var rotations);
            var cacheTime = Time.unscaledTime;
            var cacheTimeTimeScale = _location.GetTimeScale();
            if (!await AsyncUtil.DelayFrame(_location, frameDelay)) {
                positions.Dispose();
                rotations.Dispose();
                return default;
            }

            return (positions, rotations, cacheTime, cacheTimeTimeScale);
        }

        async UniTask<(UnsafeArray<float3>, UnsafeArray<quaternion>, float, float)> CacheAfterTime(float firstDelay, float secondDelay) {
            if (firstDelay > 0) {
                if (!await AsyncUtil.DelayTimeWithModelTimeScale(_location, firstDelay)) {
                    return default;
                }
            }
            _ragdollDeathBehaviour.RagdollController.CacheRigidbodyTransforms(ARAlloc.Persistent, out var positions, out var rotations);
            var cacheTime = Time.unscaledTime;
            var cacheTimeTimeScale = _location.GetTimeScale();
            if (!await AsyncUtil.DelayTimeWithModelTimeScale(_location, secondDelay)) {
                positions.Dispose();
                rotations.Dispose();
                return default;
            }
            return (positions, rotations, cacheTime, cacheTimeTimeScale);
        }

        [Serializable]
        public struct RagdollEnableData {
            [ShowIf(nameof(enableRagdollAfterAnimation))] public FloatRange delayToEnterRagdoll;
            [ShowIf(nameof(enableRagdollAfterAnimation))] public AnimToRagdollForceBufferType animToRagdollForceBufferType;
            public bool enableRagdollAfterAnimation;

            public float DelayToEnterRagdoll => delayToEnterRagdoll.RogueRandomPick();

            public RagdollEnableData(bool enableRagdollAfterAnimation, float delayToEnterRagdoll, AnimToRagdollForceBufferType animToRagdollForceBufferType)
                : this(enableRagdollAfterAnimation, new FloatRange(delayToEnterRagdoll, delayToEnterRagdoll), animToRagdollForceBufferType) { }

            public RagdollEnableData(bool enableRagdollAfterAnimation, FloatRange delayToEnterRagdoll, AnimToRagdollForceBufferType animToRagdollForceBufferType) {
                this.enableRagdollAfterAnimation = enableRagdollAfterAnimation;
                this.delayToEnterRagdoll = delayToEnterRagdoll;
                this.animToRagdollForceBufferType = animToRagdollForceBufferType;
            }
            
            public static RagdollEnableData Default => new RagdollEnableData(true, 3.5f, AnimToRagdollForceBufferType.ConstAmountOfFrames);
        }

        public enum AnimToRagdollForceBufferType : byte {
            None,
            OneFrame,
            ConstAmountOfFrames,
            ConstTimeBuffer
        }

        [BurstCompile]
        struct CalculateVelocitiesJob : IJob {
            public UnsafeArray<float3>.Span previousPositions;
            public UnsafeArray<float3>.Span currentPositions;
            public UnsafeArray<quaternion>.Span previousRotations;
            public UnsafeArray<quaternion>.Span currentRotations;
            public float elapsedTime;

            public UnsafeArray<float3>.Span outPositionsVelocity;
            public UnsafeArray<float3>.Span outRotationsVelocity;

            public void Execute() {
                float timeMultiplier = math.rcp(elapsedTime);
                for (var i = 0u; i < previousPositions.Length; i++) {
                    outPositionsVelocity[i] = (currentPositions[i] - previousPositions[i]) * timeMultiplier;

                    var deltaRotation = math.mul(currentRotations[i], math.inverse(previousRotations[i]));
                    mathExt.toAxisAngleRad(deltaRotation, out var angleRad, out var axis);
                    outRotationsVelocity[i] = axis * (angleRad * timeMultiplier);
                }
            }
        }
    }
}