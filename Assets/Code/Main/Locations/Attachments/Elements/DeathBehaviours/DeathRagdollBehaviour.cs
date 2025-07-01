using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Timing.ARTime.TimeComponents;
using Awaken.TG.MVC;
using Awaken.Utility;
using Awaken.Utility.Animations;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements.DeathBehaviours {
    public abstract class DeathRagdollBehaviour {
        protected const int RagdollsLayer = RenderLayers.Ragdolls;
        bool _ragdollInProgress;
        CancellationTokenSource _fixedRagdollToken;

        Dictionary<string, RagdollUtilities.RagdollData> _ragdollData;
        readonly bool _canRagdollWhenAlive;
        readonly bool _shouldRagdollOnDeath;
        
        protected abstract bool IsForceDirectionOverriden { get; }
        protected abstract Transform RootRagdollBone { get; }
        protected abstract Transform RootGameObject { get; }
        protected abstract IModel TimeOwnerModel { get; }
        protected virtual Vector3 ForceDirectionOverride { get; }
        public bool IsRagdollEnabled => _ragdollInProgress;
        
        protected DeathRagdollBehaviour(bool canRagdollWhenAlive, bool shouldRagdollOnDeath) {
            _canRagdollWhenAlive = canRagdollWhenAlive;
            _shouldRagdollOnDeath = shouldRagdollOnDeath;
        }

        protected DeathRagdollBehaviour(DeathRagdollBehaviour other, bool canRagdollWhenAlive, bool shouldRagdollOnDeath) {
            _canRagdollWhenAlive = canRagdollWhenAlive;
            _shouldRagdollOnDeath = shouldRagdollOnDeath;
            this._ragdollData = other._ragdollData;
        }

        public void OnDeath(DamageOutcome damageOutcome, Location location = null) {
            if (!_shouldRagdollOnDeath || _ragdollInProgress) {
                return;
            }

            OnDeathInternal(damageOutcome);
        }

        public virtual void EnableRagdoll(Vector3 force, Transform parent = null, Collider hitCollider = null, Vector3? hitPosition = null, float radius = 0) {
            if (!_canRagdollWhenAlive || _ragdollInProgress) {
                return;
            }

            TryToEnableRagdoll(force, parent, hitCollider, hitPosition, radius);
        }

        public virtual void EnableDeathRagdoll(DamageOutcome damageOutcome) {
            TryToEnableRagdoll(damageOutcome.RagdollForce, RootGameObject, damageOutcome.HitCollider, damageOutcome.Position, damageOutcome.Radius);
        }
        
        public virtual void EnableDeathRagdoll(Vector3[] bonesLinearVelocity = null, Vector3[] bonesAngularVelocity = null) {
            if (_ragdollInProgress) {
                return;
            }
            
            TryToEnableRagdoll(Vector3.zero, bonesLinearVelocity: bonesLinearVelocity, bonesAngularVelocity: bonesAngularVelocity);
        }

        public virtual void DisableRagdoll() {
            if (!_canRagdollWhenAlive) return;

            foreach (var bone in RagdollBones(RootGameObject)) {
                Object.Destroy(bone.GetComponent<FixedJoint>());
                Object.Destroy(bone.GetComponent<ConfigurableJoint>());
                Object.Destroy(bone.GetComponent<CharacterJoint>());
                Object.Destroy(bone.GetComponent<Rigidbody>());
                Object.Destroy(bone.GetComponent<Collider>());
            }

            TimeOwnerModel.GetTimeDependent()?.RemoveInvalidComponentsAfterFrame().Forget();
            _ragdollInProgress = false;
            ToggleComponents(_ragdollInProgress);
            _fixedRagdollToken?.Cancel();
            _fixedRagdollToken = null;
        }

        public async UniTaskVoid SetActiveRagdollConstraints(bool active) {
            _fixedRagdollToken?.Cancel();
            _fixedRagdollToken = new CancellationTokenSource();
            bool cancelled = await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, cancellationToken: _fixedRagdollToken.Token).SuppressCancellationThrow();
            if (cancelled) {
                return;
            }
            
            if (!_ragdollInProgress || RootGameObject == null) {
                return;
            }
            
            foreach (var bone in RagdollBones(RootGameObject)) {
                if (!active) {
                    if (bone.TryGetComponent(out Joint joint)) {
                        Rigidbody connectedBody = joint.connectedBody;
                        var addComponent = bone.AddComponent<FixedJoint>();
                        addComponent.enablePreprocessing = false;
                        addComponent.connectedBody = connectedBody;
                        Object.Destroy(joint);
                    }
                } else if (bone.TryGetComponent(out FixedJoint fixedJoint)) {
                    Object.Destroy(fixedJoint);
                    _ragdollData[bone.name].CopyJointData(bone);
                }
            }
        }

        protected virtual void OnDeathInternal(DamageOutcome damageOutcome) {
            EnableDeathRagdoll(damageOutcome);
        }

        protected virtual void TryToEnableRagdoll(Vector3 force, Transform parent = null, Collider hitCollider = null, Vector3? hitPosition = null, float radius = 0, Vector3[] bonesLinearVelocity = null, Vector3[] bonesAngularVelocity = null) {
            parent = parent == null ? RootGameObject : parent;
            if (!parent.gameObject.activeInHierarchy) {
                Log.Important?.Error($"Trying to enable ragdoll on inactive object: {parent} {parent.gameObject.PathInSceneHierarchy()}, it will cause errors");
                return;
            }
            EnableRagdollInternal(force, parent, hitCollider, hitPosition, radius, bonesLinearVelocity, bonesAngularVelocity);
        }

        protected void EnableRagdollInternal(Vector3 force, Transform parent, Collider hitCollider, Vector3? hitPosition , float radius, Vector3[] bonesLinearVelocity, Vector3[] bonesAngularVelocity) {
            bool wasRagdollEnabled = _ragdollInProgress;
            _ragdollInProgress = true;
            ToggleComponents(_ragdollInProgress);

            hitCollider = hitCollider == null ? RootRagdollBone.GetComponent<Collider>() : hitCollider;
            Vector3 position = hitPosition ?? RootRagdollBone.position;

            // --- Add Ragdoll to bones
            var bones = BonesInRagdollData(parent).ToArray();
            if (!wasRagdollEnabled) {
                foreach (Transform bone in bones) {
                    _ragdollData[bone.name].CopyTo(bone, AdditionalRigidbodySetup);
                }
            }

            if (bonesLinearVelocity != null || bonesAngularVelocity != null) {
                for (int i = 0; i < bones.Length; i++) {
                    if (bones[i].TryGetComponent(out Rigidbody rb)) {
                        if (bonesLinearVelocity != null) {
                            rb.linearVelocity = bonesLinearVelocity[i];
                        }
                        if (bonesAngularVelocity != null) {
                            rb.angularVelocity = bonesAngularVelocity[i];
                        }
                    }
                }
            }

            if (force.magnitude > 0) {
                Rigidbody rb = RootRagdollBone.GetComponentInChildren<Rigidbody>();
                if (rb == null) {
                    Log.Important?.Error($"ColliderHit: {hitCollider} has no rigidbody in parent!");
                    return;
                }

                float timeScale = TimeOwnerModel.GetTimeScale();
                if (radius > 0) {
                    rb.AddExplosionForce(force.magnitude * timeScale, position, radius, 1, ForceMode.Impulse);
                } else {
                    var direction = IsForceDirectionOverriden 
                        ? (force * timeScale).magnitude * ForceDirectionOverride 
                        : force * timeScale;
                    rb.AddForceAtPosition(direction, position, ForceMode.Impulse);
                }
            }
        }

        public TransformCache[] GetBoneTransformCache() {
            return BonesInRagdollData(RootGameObject).Select(boneTransform => new TransformCache(boneTransform)).ToArray();
        }

        protected void CreateDataDictionary(int capacity) {
            _ragdollData = new Dictionary<string, RagdollUtilities.RagdollData>(capacity);
        }

        protected void CacheBone(Transform bone) {
            // --- Gather data
            var ragdollData = new RagdollUtilities.RagdollData();
            var rigidbody = bone.GetComponent<Rigidbody>();
            var ragdollCollider = bone.GetComponent<Collider>();
            var characterJoint = bone.GetComponent<CharacterJoint>();
            var configurableJoint = bone.GetComponent<ConfigurableJoint>();
            // --- Cache Data
            ragdollData.Save(rigidbody, ragdollCollider, characterJoint, configurableJoint);
            _ragdollData[bone.name] = ragdollData;
            // --- Remove components
            Object.Destroy(configurableJoint);
            Object.Destroy(characterJoint);
            Object.Destroy(rigidbody);
            Object.Destroy(ragdollCollider);
        }

        protected abstract void ToggleComponents(bool ragdollEnabled);

        protected static IEnumerable<Transform> RagdollBones(Transform parent) => parent.GetComponentsInChildren<Transform>()
            .Where(t => t.gameObject.layer == RagdollsLayer);
        
        IEnumerable<Transform> BonesInRagdollData(Transform parent) => RagdollBones(parent).Where(bone => _ragdollData.ContainsKey(bone.name));
        
        void AdditionalRigidbodySetup(Rigidbody rigidbody) {
            TimeOwnerModel?.GetTimeDependent()?.WithTimeComponent(new TimeRigidbody(rigidbody));
        }
        
        public struct TransformCache {
            public Vector3 position;
            public Quaternion rotation;
            
            public TransformCache(Transform transform) {
                position = transform.position;
                rotation = transform.rotation;
            }
        }
    }
}