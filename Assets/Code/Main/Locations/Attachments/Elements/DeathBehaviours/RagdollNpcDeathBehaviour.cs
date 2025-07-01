using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Awaken.Kandra.AnimationPostProcessing;
using Awaken.TG.Main.Animations.FSM.Npc.States.General;
using Awaken.TG.Main.Animations.IK;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements.DeathBehaviours {
    public class DeathRagdollNpcBehaviour : DeathRagdollBehaviour, IDeathBehaviour {
        protected override bool IsForceDirectionOverriden => false;
        protected override Transform RootRagdollBone => _rootRagdollBone;
        protected override Transform RootGameObject => _rootGameObject;
        protected override IModel TimeOwnerModel => Location;
        
        Animator _animator;
        GameObject _alivePrefab;
        DeathElement _death;
        CancellationTokenSource _cancellationToken;
        Transform _rootRagdollBone;
        Transform _hipsBone;
        Transform _rootGameObject;
        
        public bool IsVisualInitialized { get; private set; }
        public bool UseDeathAnimation => false;
        public NpcDeath.DeathAnimType UseCustomDeathAnimation => NpcDeath.DeathAnimType.Default;
        NpcElement Npc => _death.ParentModel;
        Location Location { get; set; }

        public DeathRagdollNpcBehaviour(bool canRagdollWhenAlive, bool shouldRagdollOnDeath) : base(canRagdollWhenAlive, shouldRagdollOnDeath){ }

        public DeathRagdollNpcBehaviour(DeathRagdollNpcBehaviour other, bool canRagdollWhenAlive, bool shouldRagdollOnDeath) : base(other, canRagdollWhenAlive, shouldRagdollOnDeath) {
            IsVisualInitialized = other.IsVisualInitialized;
            _death = other._death;
            Location = other.Location;
            _rootGameObject = other._rootGameObject;
            _rootRagdollBone = other._rootRagdollBone;
            _hipsBone = other._hipsBone;
            _animator = other._animator;
            _alivePrefab = other._alivePrefab;
        }

        public void OnVisualLoaded(DeathElement death, Transform transform) {
            IsVisualInitialized = true;
            _death = death;
            Location = death.ParentModel.ParentModel;
            _rootGameObject = transform;
            _hipsBone = death.ParentModel.Hips;
            CacheRigidBody();
        }

        public override void EnableRagdoll(Vector3 force, Transform parent = null, Collider hitCollider = null, Vector3? hitPosition = null, float radius = 0) {
            if (Npc != null) {
                Npc.Trigger(DeathElement.Events.RagdollToggled, true);
                Npc.IsInRagdoll = true;
            }
            base.EnableRagdoll(force, parent, hitCollider, hitPosition, radius);
        }

        public override void DisableRagdoll() {
            _cancellationToken?.Cancel();
            _cancellationToken = new CancellationTokenSource();
            base.DisableRagdoll();
            if (Npc != null) {
                Npc.Trigger(DeathElement.Events.RagdollToggled, false);
                Npc.IsInRagdoll = false;
            }
        }

        public override void EnableDeathRagdoll(DamageOutcome damageOutcome) {
            _animator.GetComponent<ARNpcAnimancer>().OnNpcDeath();
            base.EnableDeathRagdoll(damageOutcome);
            AfterDeathRagdollEnabled();
        }
        
        public override void EnableDeathRagdoll(Vector3[] bonesLinearVelocity = null, Vector3[] bonesAngularVelocity = null) {
            _animator.GetComponent<ARNpcAnimancer>().OnNpcDeath();
            base.EnableDeathRagdoll(bonesLinearVelocity, bonesAngularVelocity);
            AfterDeathRagdollEnabled();
        }

        protected override void OnDeathInternal(DamageOutcome damageOutcome) {
            _animator.GetComponent<ARNpcAnimancer>().OnNpcDeath();
            base.OnDeathInternal(damageOutcome);
            AfterDeathRagdollEnabled();
        }
        
        void CacheRigidBody() {
            Transform[] ragdollBones = RagdollBones(RootGameObject).ToArray();
            float npcTotalRigidBodyMass = ragdollBones.Sum(r => {
                if (r.TryGetComponent(out Rigidbody rb)) {
                    return rb.mass;
                }
                return 0;
            });
            int npcWeight = Npc.Template.npcWeight;

            CreateDataDictionary(ragdollBones.Length);
            
            foreach (Transform bone in ragdollBones) {
                if (RootRagdollBone == null) {
                    _rootRagdollBone = bone;
                }

                if (bone.TryGetComponent(out Rigidbody rb)) {
                    rb.mass = (rb.mass / npcTotalRigidBodyMass) * npcWeight;
                }
                CacheBone(bone);
            }
            
            Location.GetTimeDependent()?.RemoveInvalidComponentsAfterFrame().Forget();

            _animator = RootRagdollBone!.GetComponentInParent<Animator>(true);
            var aliveTransform = _animator.gameObject.FindChildRecursively("AlivePrefab", true);
            
            if (aliveTransform != null) {
                _alivePrefab = aliveTransform.gameObject;
            }
        }

        protected override void TryToEnableRagdoll(Vector3 force, Transform parent = null, Collider hitCollider = null, Vector3? hitPosition = null, float radius = 0, Vector3[] bonesLinearVelocity = null, Vector3[] bonesAngularVelocity = null) {
            parent = parent == null ? RootGameObject : parent;
            if (!parent.gameObject.activeInHierarchy) {
                EnableRagdollWhenActiveInHierarchy(force, parent, hitCollider, hitPosition, radius, bonesLinearVelocity, bonesAngularVelocity).Forget();
                return;
            }
            if (!IsRagdollEnabled && _hipsBone.TryGetComponent<Rigidbody>(out _)) {
                // Hips has Rigidbody and Ragdoll is not enabled? It means ragdoll ic currently being discarded, we need to wait a frame.
                TryToEnableRagdollNextFrame(force, parent, hitCollider, hitPosition, radius, bonesLinearVelocity, bonesAngularVelocity).Forget();
                return;
            }
            EnableRagdollInternal(force, parent, hitCollider, hitPosition, radius, bonesLinearVelocity, bonesAngularVelocity);
        }
        
        async UniTaskVoid TryToEnableRagdollNextFrame(Vector3 force, Transform parent, Collider hitCollider, Vector3? hitPosition, float radius, Vector3[] bonesLinearVelocity, Vector3[] bonesAngularVelocity ) {
            _cancellationToken?.Cancel();
            _cancellationToken = new CancellationTokenSource();
            if (!await AsyncUtil.DelayFrame(parent, 1, _cancellationToken.Token)) {
                return;
            }
            TryToEnableRagdoll(force, parent, hitCollider, hitPosition, radius, bonesLinearVelocity, bonesAngularVelocity);
        }

        async UniTaskVoid EnableRagdollWhenActiveInHierarchy(Vector3 force, Transform parent , Collider hitCollider, Vector3? hitPosition, float radius, Vector3[] bonesLinearVelocity, Vector3[] bonesAngularVelocity) {
            _cancellationToken?.Cancel();
            _cancellationToken = new CancellationTokenSource();
            if (!await AsyncUtil.WaitUntil(parent, () => parent.gameObject.activeInHierarchy, _cancellationToken.Token)) {
                return;
            }
            EnableRagdollInternal(force, parent, hitCollider, hitPosition, radius, bonesLinearVelocity, bonesAngularVelocity);
        }

        protected override void ToggleComponents(bool ragdollEnabled) {
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

        protected void AfterDeathRagdollEnabled() {
            VCFeetIK feetIK = _animator.GetComponent<VCFeetIK>();
            if (feetIK != null) {
                Object.Destroy(feetIK);
            }
        }

        protected new static IEnumerable<Transform> RagdollBones(Transform parent) => parent.GetComponentsInChildren<Transform>()
            .Where(t => t.gameObject.layer == RagdollsLayer);
    }
}