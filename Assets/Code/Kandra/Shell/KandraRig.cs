using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Awaken.Kandra {
    public class KandraRig : MonoBehaviour {
        public Animator animator;
        public Transform[] bones = Array.Empty<Transform>();
        public ushort[] boneParents = Array.Empty<ushort>();
        public FixedString64Bytes[] boneNames = Array.Empty<FixedString64Bytes>();
        public ushort baseBoneCount;
        private List<KandraRenderer> _mergedRenderers;
        private List<KandraRenderer> _activeRenderers;
        private bool _isRegistered;

        public void OnDestroy() {
            throw new NotImplementedException();
        }

        public void EnsureInitialized() {
            throw new NotImplementedException();
        }

        public void RegisterActiveRenderer(KandraRenderer renderer) {
            throw new NotImplementedException();
            ;
        }

        public void UnregisterActiveRenderer(KandraRenderer renderer) {
            throw new NotImplementedException();
        }

        public void MarkRegistered() {
            throw new NotImplementedException();
        }

        public void MarkUnregistered() {
            throw new NotImplementedException();
        }

        public void Merge(KandraRig otherRig, KandraRenderer renderer, ushort[] otherRendererBones,
            ref ushort otherRendererRootBone) {
            throw new NotImplementedException();
        }

        public void Merge(KandraRig otherRig, KandraRenderer renderer, ushort[] otherRendererBones,
            ref UnsafeHashMap<FixedString64Bytes, ushort> bonesCatalog, ref ushort otherRendererRootBone) {
            throw new NotImplementedException();
        }

        public void RemoveMerged(KandraRenderer renderer) {
            throw new NotImplementedException();
        }

        public void MarkAsBase() {
            throw new NotImplementedException();
        }

        public UnsafeHashMap<FixedString64Bytes, ushort> CreateBonesMap(float additionalCapacity, Allocator allocator) {
            throw new NotImplementedException();
        }

        public struct EditorAccessor {
            public static List<KandraRenderer> MergedRenderers(KandraRig rig) => rig._mergedRenderers;
            public static List<KandraRenderer> ActiveRenderers(KandraRig rig) => rig._activeRenderers;
            public static bool IsRegistered(KandraRig rig) => rig._isRegistered;
        }
    }
}