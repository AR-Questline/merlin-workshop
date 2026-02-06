using System;
using Awaken.Utility.LowLevel.Collections;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.Utility.Animations {
    public class RagdollController : MonoBehaviour {
        public RagdollSetupSO ragdollSetup;
        public Transform rootBone;
        
        Transform[] _excludedBones;

        public void ApplyRagdoll(Action<Rigidbody> additionalRigidbodySetup = null) {
            ApplyRagdoll(ragdollSetup.wholeMass, additionalRigidbodySetup);
        }

        public void ApplyRagdoll(float ragdollMassOverride, Action<Rigidbody> additionalRigidbodySetup = null) {
            var massRatio = ragdollMassOverride / ragdollSetup.wholeMass;
            var boneIndex = 0;
            var rigidbodyIndex = 0;
            if (_excludedBones == null) {
                ApplyRagdollWithoutExclusion(rootBone, ref boneIndex, ref rigidbodyIndex, massRatio, additionalRigidbodySetup);
            } else {
                ApplyRagdollWithExclusion(rootBone, ref boneIndex, ref rigidbodyIndex, massRatio, additionalRigidbodySetup);
            }
        }

        public void RemoveRagdoll() {
            RemoveRagdoll(rootBone);
        }

        public void ReplaceJointWithFixedJoint() {
            ReplaceJointWithFixedJoint(rootBone);
        }

        public int RemoveFixedJoint() {
            return RemoveFixedJoint(rootBone, 0);
        }

        public void RemoveBoneFromRagdoll(Transform bone) {
            if (bone.gameObject.layer == RenderLayers.Ragdolls) {
                GameObjects.GameObjects.DestroySafely(bone.GetComponent<FixedJoint>());
                GameObjects.GameObjects.DestroySafely(bone.GetComponent<ConfigurableJoint>());
                GameObjects.GameObjects.DestroySafely(bone.GetComponent<CharacterJoint>());
                GameObjects.GameObjects.DestroySafely(bone.GetComponent<Rigidbody>());
                GameObjects.GameObjects.DestroySafely(bone.GetComponent<Collider>());
            }

            AddToExcluded(bone);

            for (var i = 0; i < bone.childCount; i++) {
                var child = bone.GetChild(i);
                RemoveBoneFromRagdoll(child);
            }
        }

        public void CacheRigidbodyTransforms(Allocator allocator, out UnsafeArray<float3> positions, out UnsafeArray<quaternion> rotations) {
            positions = new UnsafeArray<float3>(ragdollSetup.rigidBodyCount, allocator);
            rotations = new UnsafeArray<quaternion>(ragdollSetup.rigidBodyCount, allocator);

            var boneIndex = 0;
            var rigidbodyIndex = 0;
            if (_excludedBones == null) {
                CacheRigidbodyTransformsWithoutExclusion(rootBone, ref boneIndex, ref rigidbodyIndex, positions, rotations);
            } else {
                CacheRigidbodyTransformsWithExclusion(rootBone, ref boneIndex, ref rigidbodyIndex, positions, rotations);
            }
        }

        void ApplyRagdollWithoutExclusion(Transform currentBone, ref int boneIndex, ref int rigidbodyIndex, float massRatio, Action<Rigidbody> additionalRigidbodySetup) {
            if (currentBone.gameObject.layer == RenderLayers.Ragdolls) {
                string boneName = string.Empty;
#if UNITY_EDITOR && !ADDRESSABLES_BUILD
                boneName = currentBone.gameObject.name;
#endif
                ref readonly var ragdollConfig = ref ragdollSetup.GetBoneConfig(boneIndex, boneName, this);
                ragdollConfig.CopyTo(currentBone, massRatio, additionalRigidbodySetup);

                if (ragdollConfig.HasRigidbody) {
                    ++rigidbodyIndex;
                }

                ++boneIndex;
            }
            for (var i = 0; i < currentBone.childCount; i++) {
                var child = currentBone.GetChild(i);
                ApplyRagdollWithoutExclusion(child, ref boneIndex, ref rigidbodyIndex, massRatio, additionalRigidbodySetup);
            }
        }
        
        void ApplyRagdollWithExclusion(Transform currentBone, ref int boneIndex, ref int rigidbodyIndex, float massRatio, Action<Rigidbody> additionalRigidbodySetup) {
            if (currentBone.gameObject.layer == RenderLayers.Ragdolls) {
                string boneName = string.Empty;
#if UNITY_EDITOR && !ADDRESSABLES_BUILD
                boneName = currentBone.gameObject.name;
#endif
                ref readonly var ragdollConfig = ref ragdollSetup.GetBoneConfig(boneIndex, boneName, this);

                if (!IsExcluded(currentBone)) {
                    ragdollConfig.CopyTo(currentBone, massRatio, additionalRigidbodySetup);
                }

                if (ragdollConfig.HasRigidbody) {
                    ++rigidbodyIndex;
                }

                ++boneIndex;
            }
            for (var i = 0; i < currentBone.childCount; i++) {
                var child = currentBone.GetChild(i);
                ApplyRagdollWithExclusion(child, ref boneIndex, ref rigidbodyIndex, massRatio, additionalRigidbodySetup);
            }
        }

        void RemoveRagdoll(Transform root) {
            if (root.gameObject.layer == RenderLayers.Ragdolls) {
                GameObjects.GameObjects.DestroySafely(root.GetComponent<FixedJoint>());
                GameObjects.GameObjects.DestroySafely(root.GetComponent<ConfigurableJoint>());
                GameObjects.GameObjects.DestroySafely(root.GetComponent<CharacterJoint>());
                GameObjects.GameObjects.DestroySafely(root.GetComponent<Rigidbody>());
                GameObjects.GameObjects.DestroySafely(root.GetComponent<Collider>());
            }
            
            for (var i = 0; i < root.childCount; i++) {
                var child = root.GetChild(i);
                RemoveRagdoll(child);
            }
        }

        void ReplaceJointWithFixedJoint(Transform currentBone) {
            if (currentBone.gameObject.layer == RenderLayers.Ragdolls && currentBone.TryGetComponent(out Joint joint)) {
                Rigidbody connectedBody = joint.connectedBody;
                var addComponent = currentBone.gameObject.AddComponent<FixedJoint>();
                addComponent.enablePreprocessing = false;
                addComponent.connectedBody = connectedBody;
                GameObjects.GameObjects.DestroySafely(joint);
            }
            for (var i = 0; i < currentBone.childCount; i++) {
                var child = currentBone.GetChild(i);
                ReplaceJointWithFixedJoint(child);
            }
        }

        int RemoveFixedJoint(Transform currentBone, int index) {
            if (currentBone.gameObject.layer == RenderLayers.Ragdolls) {
                if (currentBone.TryGetComponent(out FixedJoint fixedJoint)) {
                    GameObjects.GameObjects.DestroySafely(fixedJoint);
                    string boneName = string.Empty;
#if UNITY_EDITOR && !ADDRESSABLES_BUILD
                    boneName = currentBone.gameObject.name;
#endif
                    ref readonly var ragdollConfig = ref ragdollSetup.GetBoneConfig(index, boneName, this);
                    ragdollConfig.CopyJointData(currentBone);
                }

                ++index;
            }
            for (var i = 0; i < currentBone.childCount; i++) {
                var child = currentBone.GetChild(i);
                index = RemoveFixedJoint(child, index);
            }

            return index;
        }

        void CacheRigidbodyTransformsWithoutExclusion(Transform currentBone, ref int boneIndex, ref int rigidbodyIndex, UnsafeArray<float3> positions, UnsafeArray<quaternion> rotations) {
            if (currentBone.gameObject.layer == RenderLayers.Ragdolls) {
                string boneName = string.Empty;
#if UNITY_EDITOR && !ADDRESSABLES_BUILD
                boneName = currentBone.gameObject.name;
#endif
                ref readonly var ragdollConfig = ref ragdollSetup.GetBoneConfig(boneIndex, boneName, this);
                if (ragdollConfig.HasRigidbody) {
                    currentBone.GetPositionAndRotation(out var position, out var rotation);
                    positions[(uint)rigidbodyIndex] = position;
                    rotations[(uint)rigidbodyIndex] = rotation;
                    ++rigidbodyIndex;
                }

                ++boneIndex;
            }
            for (var i = 0; i < currentBone.childCount; i++) {
                var child = currentBone.GetChild(i);
                CacheRigidbodyTransformsWithoutExclusion(child, ref boneIndex, ref rigidbodyIndex, positions, rotations);
            }
        }

        void CacheRigidbodyTransformsWithExclusion(Transform currentBone, ref int boneIndex, ref int rigidbodyIndex, UnsafeArray<float3> positions, UnsafeArray<quaternion> rotations) {
            if (currentBone.gameObject.layer == RenderLayers.Ragdolls) {
                string boneName = string.Empty;
#if UNITY_EDITOR && !ADDRESSABLES_BUILD
                boneName = currentBone.gameObject.name;
#endif
                ref readonly var ragdollConfig = ref ragdollSetup.GetBoneConfig(boneIndex, boneName, this);
                if (ragdollConfig.HasRigidbody) {
                    if (!IsExcluded(currentBone)) {
                        currentBone.GetPositionAndRotation(out var position, out var rotation);
                        positions[(uint)rigidbodyIndex] = position;
                        rotations[(uint)rigidbodyIndex] = rotation;
                    }
                    ++rigidbodyIndex;
                }

                ++boneIndex;
            }
            for (var i = 0; i < currentBone.childCount; i++) {
                var child = currentBone.GetChild(i);
                CacheRigidbodyTransformsWithoutExclusion(child, ref boneIndex, ref rigidbodyIndex, positions, rotations);
            }
        }

        void AddToExcluded(Transform bone) {
            if (_excludedBones == null) {
                _excludedBones = new Transform[1];
                _excludedBones[0] = bone;
                return;
            }

            var newArray = new Transform[_excludedBones.Length + 1];
            Array.Copy(_excludedBones, newArray, _excludedBones.Length);
            newArray[_excludedBones.Length] = bone;
            Array.Clear(_excludedBones, 0, _excludedBones.Length);
            _excludedBones = newArray;
        }

        bool IsExcluded(Transform bone) {
            for (int i = 0; i < _excludedBones.Length; i++) {
                if (_excludedBones[i] == bone) return true;
            }
            return false;
        }
    }
}

