using System;
using System.Threading;
using Awaken.Kandra.AnimationPostProcess;
using Awaken.TG.Main.Animations.FSM.Npc.States.General;
using Awaken.TG.Main.Animations.IK;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.MVC;
using Awaken.Utility.Animations;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel.Collections;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Locations.Attachments.Elements.DeathBehaviours {
    public class DeathRagdollNpcBehaviour : DeathRagdollBehaviour, IDeathBehaviour {
        Animator _animator;
        GameObject _alivePrefab;
        DeathElement _death;
        CancellationTokenSource _cancellationToken;
        CancellationTokenSource _fixedRagdollToken;
        Transform _hipsBone;
        float _npcWeight = 1f;

        readonly bool _shouldRagdollOnDeath;
        readonly bool _canRagdollWhenAlive;

        public bool IsVisualInitialized { get; private set; }
        public bool UseDeathAnimation => false;
        public NpcDeath.DeathAnimType UseCustomDeathAnimation => NpcDeath.DeathAnimType.Default;
        public bool IsRagdollInProgress { get; private set; }

        NpcElement Npc => _death.ParentModel;
        // It's called NpcBehaviour but Npc is required only at initialization, after initialized is LocationBehaviour :)
        Location Location { get; set; }

        protected override IModel TimeOwnerModel => Location;

        public DeathRagdollNpcBehaviour(bool canRagdollWhenAlive, bool shouldRagdollOnDeath) {
            _shouldRagdollOnDeath = shouldRagdollOnDeath;
            _canRagdollWhenAlive = canRagdollWhenAlive;
        }

        public DeathRagdollNpcBehaviour(DeathRagdollNpcBehaviour other, bool canRagdollWhenAlive, bool shouldRagdollOnDeath) : this(canRagdollWhenAlive, shouldRagdollOnDeath) {
            IsVisualInitialized = other.IsVisualInitialized;
            _death = other._death;
            Location = other.Location;
            RagdollController = other.RagdollController;
            _hipsBone = other._hipsBone;
            _animator = other._animator;
            _alivePrefab = other._alivePrefab;
        }

        public void OnVisualLoaded(DeathElement death, Transform transform) {
            IsVisualInitialized = true;
            _death = death;
            Location = Npc.ParentModel;
            RagdollController = transform.GetComponentInChildren<RagdollController>();
            _hipsBone = death.ParentModel.Hips;

            if (RagdollController == null) {
                Log.Important?.Error($"RagdollController is null for {Npc.Controller}", Npc.Controller);
#if UNITY_EDITOR || DEBUG || AR_DEBUG
                var rbs = Npc.Controller.GetComponentsInChildren<Rigidbody>();
                foreach (var rigidbody in rbs) {
                    if (rigidbody.TryGetComponent<Joint>(out var joint)) {
                        Object.Destroy(joint);
                    } else if (rigidbody.TryGetComponent<Collider>(out var collider)) {
                        Object.Destroy(collider);
                    }
                    Object.Destroy(rigidbody);
                }
#endif
            }

            _animator = RagdollController.rootBone.GetComponentInParent<Animator>(true);
            _alivePrefab = Npc.Controller.AlivePrefab;
            _npcWeight = Npc.Template.npcWeight;
        }

        public void OnDeath(DamageOutcome damageOutcome, Location location) {
            if (!_shouldRagdollOnDeath || IsRagdollInProgress) {
                return;
            }

            _animator.GetComponent<ARNpcAnimancer>().OnNpcDeath();

            EnableDeathRagdoll(damageOutcome);

            AfterDeathRagdollEnabled();
        }

        public void EnableRagdoll(Vector3 forceDirection, float forceStrength, Vector3 hitPosition) {
            if (Npc != null) {
                Npc.Trigger(DeathElement.Events.RagdollToggled, true);
                Npc.IsInRagdoll = true;
            }

            if (!_canRagdollWhenAlive || IsRagdollInProgress) {
                return;
            }

            var setup = new EnableSetup {
                forceDirection = forceDirection,
                forceMagnitude = forceStrength,
                hitPosition = hitPosition
            };
            TryToEnableRagdoll(setup, AdditionalRigidbodySetup);
        }

        public void DisableRagdoll() {
            _cancellationToken?.Cancel();
            _cancellationToken = new CancellationTokenSource();

            if (_canRagdollWhenAlive) {
                RagdollController.RemoveRagdoll();

                TimeOwnerModel.GetTimeDependent()?.RemoveInvalidComponentsAfterFrame().Forget();
                IsRagdollInProgress = false;
                ToggleComponents(IsRagdollInProgress);
                _fixedRagdollToken?.Cancel();
                _fixedRagdollToken = null;
            }

            if (Npc != null) {
                Npc.Trigger(DeathElement.Events.RagdollToggled, false);
                Npc.IsInRagdoll = false;
            }
        }

        public void EnableDeathRagdoll(DamageOutcome damageOutcome) {
            _animator.GetComponent<ARNpcAnimancer>().OnNpcDeath();

            var setup = SetupFromDamageOutcome(damageOutcome);
            TryToEnableRagdoll(setup, AdditionalRigidbodySetup);

            AfterDeathRagdollEnabled();
        }
        
        public void EnableDeathRagdoll(UnsafeArray<float3>.Span bonesLinearVelocity, UnsafeArray<float3>.Span bonesAngularVelocity) {
            _animator.GetComponent<ARNpcAnimancer>().OnNpcDeath();

            if (!IsRagdollInProgress) {
                var setup = default(EnableSetup);

                TryToEnableRagdoll(setup, RigidbodySetup);

                void RigidbodySetup(Rigidbody rb) {
                    rb.linearVelocity = bonesLinearVelocity[0];
                    rb.angularVelocity = bonesAngularVelocity[0];
                    AdditionalRigidbodySetup(rb);
                }
            }

            AfterDeathRagdollEnabled();
        }

        public void EnableDeathRagdoll() {
            _animator.GetComponent<ARNpcAnimancer>().OnNpcDeath();

            if (!IsRagdollInProgress) {
                var setup = default(EnableSetup);
                TryToEnableRagdoll(setup, AdditionalRigidbodySetup);
            }

            AfterDeathRagdollEnabled();
        }

        public async UniTaskVoid SetActiveRagdollConstraints(bool active) {
            _fixedRagdollToken?.Cancel();
            _fixedRagdollToken = new CancellationTokenSource();
            bool cancelled = await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, cancellationToken: _fixedRagdollToken.Token).SuppressCancellationThrow();
            if (cancelled) {
                return;
            }

            if (!IsRagdollInProgress || RagdollController == null) {
                return;
            }

            if (active) {
                RagdollController.RemoveFixedJoint();
            } else {
                RagdollController.ReplaceJointWithFixedJoint();
            }
        }

        void TryToEnableRagdoll(EnableSetup setup, Action<Rigidbody> additionalRigidbodySetup) {
            if (!RagdollController.gameObject.activeInHierarchy) {
                EnableRagdollWhenActiveInHierarchy(setup, additionalRigidbodySetup).Forget();
                return;
            }
            if (!IsRagdollInProgress && _hipsBone.TryGetComponent<Rigidbody>(out _)) {
                // Hips has Rigidbody and Ragdoll is not enabled? It means ragdoll is currently being discarded, we need to wait a frame.
                TryToEnableRagdollNextFrame(setup, additionalRigidbodySetup).Forget();
                return;
            }
            EnableRagdollInternal(setup, additionalRigidbodySetup);
        }
        
        async UniTaskVoid TryToEnableRagdollNextFrame(EnableSetup setup, Action<Rigidbody> additionalRigidbodySetup) {
            _cancellationToken?.Cancel();
            _cancellationToken = new CancellationTokenSource();
            if (!await AsyncUtil.DelayFrame(RagdollController, 1, _cancellationToken.Token)) {
                return;
            }
            TryToEnableRagdoll(setup, additionalRigidbodySetup);
        }

        async UniTaskVoid EnableRagdollWhenActiveInHierarchy(EnableSetup setup, Action<Rigidbody> additionalRigidbodySetup) {
            _cancellationToken?.Cancel();
            _cancellationToken = new CancellationTokenSource();
            if (!await AsyncUtil.WaitUntil(RagdollController, () => RagdollController.gameObject.activeInHierarchy, _cancellationToken.Token)) {
                return;
            }
            EnableRagdollInternal(setup, additionalRigidbodySetup);
        }

        void EnableRagdollInternal(in EnableSetup setup, Action<Rigidbody> additionalRigidbodySetup) {
            bool wasRagdollEnabled = IsRagdollInProgress;
            IsRagdollInProgress = true;
            ToggleComponents(true);

            // --- Add Ragdoll to bones
            if (!wasRagdollEnabled) {
                RagdollController.ApplyRagdoll(_npcWeight, additionalRigidbodySetup);
            }

            if (setup.forceMagnitude > 0) {
                AddForceToRagdoll(setup);
            }
        }

        void ToggleComponents(bool ragdollEnabled) {
            _animator.enabled = !ragdollEnabled;
            if (_alivePrefab != null) {
                _alivePrefab.SetActive(!ragdollEnabled);
            }

            ARNpcAnimancer npcAnimancer = _animator.GetComponent<ARNpcAnimancer>();
            if (npcAnimancer != null) {
                npcAnimancer.enabled = !ragdollEnabled;
            }
            
            AnimationPostProcessing[] animationPP = _animator.GetComponents<AnimationPostProcessing>();
            foreach (var animationPostProcessing in animationPP) {
                animationPostProcessing.enabled = !ragdollEnabled;
            }
        }

        void AfterDeathRagdollEnabled() {
            var feetIK = _animator.GetComponent<VCFeetIK>();
            if (feetIK != null) {
                Object.Destroy(feetIK);
            }
        }
    }
}