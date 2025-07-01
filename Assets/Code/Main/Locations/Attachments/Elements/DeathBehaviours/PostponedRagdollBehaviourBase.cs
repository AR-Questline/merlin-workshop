using System;
using Awaken.TG.Main.Animations.FSM.Npc.States.General;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements.DeathBehaviours {
    public abstract class PostponedRagdollBehaviourBase : MonoBehaviour, IDeathBehaviour {
        const float CacheRagdollBufferTime = 0.5f;
        const int CacheRagdollBufferFrames = 3;
        [SerializeField] RagdollEnableData ragdollData = RagdollEnableData.Default;

        Location _location;
        GameObject _alivePrefab;
        protected DeathRagdollBehaviour _ragdollDeathBehaviour;
        
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
            _ragdollDeathBehaviour = death?.GetBehaviour<DeathRagdollBehaviour>();
        }

        public virtual void OnDeath(DamageOutcome damageOutcome, Location dyingLocation) {
            if (_alivePrefab != null) {
                _alivePrefab.SetActive(false);
            }

            if (EnableRagdollAfterAnimation) {
                if (_ragdollDeathBehaviour == null) {
                    return;
                }
                if (_ragdollDeathBehaviour.IsRagdollEnabled) {
                    _ragdollDeathBehaviour.DisableRagdoll();
                }
                EnterRagdollAfterAnimationStarted().Forget();
            }
        }

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
            (DeathRagdollBehaviour.TransformCache[] transforms, float time, float timeScale) cacheData;
            switch (AnimToRagdollForceBuffer) {
                case AnimToRagdollForceBufferType.None:
                    await AsyncUtil.DelayTimeWithModelTimeScale(_location, DelayToEnterRagdoll);
                    _ragdollDeathBehaviour.EnableDeathRagdoll();
                    OnRagdollEnabled();
                    return;
                case AnimToRagdollForceBufferType.OneFrame:
                    cacheData = await CacheAfterFrames(DelayToEnterRagdoll, 1);
                    break;
                case AnimToRagdollForceBufferType.ConstAmountOfFrames:
                    cacheData = await CacheAfterFrames(DelayToEnterRagdoll, CacheRagdollBufferFrames);
                    break;
                case AnimToRagdollForceBufferType.ConstTimeBuffer:
                    if (DelayToEnterRagdoll <= CacheRagdollBufferTime) {
                        cacheData = await CacheAfterTime(0, DelayToEnterRagdoll);
                    } else {
                        cacheData = await CacheAfterTime(DelayToEnterRagdoll - CacheRagdollBufferTime, CacheRagdollBufferTime);
                    }
                    break;
                default:
                    return;
            }
            if (cacheData.transforms == null) {
                return;
            }
            float avgTimeScale = (cacheData.timeScale + _location.GetTimeScale()) * 0.5f;
            GetBonePositionsOffset(cacheData.transforms, (Time.unscaledTime - cacheData.time) / avgTimeScale, out var bonePositionVelocity, out var boneRotationVelocity);
            _ragdollDeathBehaviour.EnableDeathRagdoll(bonePositionVelocity, boneRotationVelocity);
            OnRagdollEnabled();
        }

        async UniTask<(DeathRagdollBehaviour.TransformCache[] transforms, float time, float timeScale)> CacheAfterFrames(float normalDelay, int frameDelay) {
            if (!await AsyncUtil.DelayTimeWithModelTimeScale(_location, normalDelay)) {
                return (null, 0, 0);
            }
            var transformsCache = _ragdollDeathBehaviour.GetBoneTransformCache();
            var cacheTime = Time.unscaledTime;
            var cacheTimeTimeScale = _location.GetTimeScale();
            if (!await AsyncUtil.DelayFrame(_location, frameDelay)) {
                return (null, 0, 0);
            }
            return (transformsCache, cacheTime, cacheTimeTimeScale);
        }

        async UniTask<(DeathRagdollBehaviour.TransformCache[] transforms, float time, float timeScale)> CacheAfterTime(float firstDelay, float secondDelay) {
            if (firstDelay > 0) {
                if (!await AsyncUtil.DelayTimeWithModelTimeScale(_location, firstDelay)) {
                    return (null, 0, 0);
                }
            }
            var transformsCache = _ragdollDeathBehaviour.GetBoneTransformCache();
            var cacheTime = Time.unscaledTime;
            var cacheTimeTimeScale = _location.GetTimeScale();
            if (!await AsyncUtil.DelayTimeWithModelTimeScale(_location, secondDelay)) {
                return (null, 0, 0);
            }
            return (transformsCache, cacheTime, cacheTimeTimeScale);
        }

        void GetBonePositionsOffset(DeathRagdollBehaviour.TransformCache[] cachedTransform, float cacheTime, out Vector3[] positionVelocity, out Vector3[] rotationVelocity) {
            if (_ragdollDeathBehaviour == null) {
                positionVelocity = null;
                rotationVelocity = null;
                return;
            }
            var currentState =  _ragdollDeathBehaviour.GetBoneTransformCache();
            float timeMultiplier = 1 / cacheTime;
            positionVelocity = new Vector3[cachedTransform.Length];
            rotationVelocity = new Vector3[cachedTransform.Length];
            for (int i = 0; i < cachedTransform.Length; i++) {
                positionVelocity[i] = (currentState[i].position - cachedTransform[i].position) * timeMultiplier;
                
                var deltaRotation = currentState[i].rotation * Quaternion.Inverse(cachedTransform[i].rotation);
                deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
                rotationVelocity[i] = axis * (angle * Mathf.Deg2Rad * timeMultiplier);
            }
        }

        [Serializable]
        public struct RagdollEnableData {
            public bool enableRagdollAfterAnimation;
            [ShowIf(nameof(enableRagdollAfterAnimation))] public FloatRange delayToEnterRagdoll;
            [ShowIf(nameof(enableRagdollAfterAnimation))] public AnimToRagdollForceBufferType animToRagdollForceBufferType;
            
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
    }
}