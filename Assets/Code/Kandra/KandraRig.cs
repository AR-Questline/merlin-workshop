using System;
using System.Collections.Generic;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Awaken.Utility.LowLevel.Collections;
using Sirenix.OdinInspector;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UniversalProfiling;

namespace Awaken.Kandra {
    [ExecuteInEditMode]
    [BurstCompile]
    public class KandraRig : MonoBehaviour {
        const float ExpandFactor = 1.2f;
        const int PreAllocatedAddedBones = 40;

        static readonly UniversalProfilerMarker MargeMarker = new UniversalProfilerMarker("KandraRig.Merge");
        static readonly UniversalProfilerMarker RemoveMergeMarker = new UniversalProfilerMarker("KandraRig.RemoveMerge");
        static readonly UniversalProfilerMarker CopyBoneMarker = new UniversalProfilerMarker("KandraRig.CopyBone");

        public Animator animator;

        public Transform[] bones;
        public ushort[] boneParents;
        [ListDrawerSettings(OnBeginListElementGUI = "BeginDrawBoneName", OnEndListElementGUI = "EndDrawBoneName")]
        public FixedString64Bytes[] boneNames;

        public ushort baseBoneCount;

        [ShowInInspector, NonSerialized, Sirenix.OdinInspector.ReadOnly]
        readonly List<KandraRenderer> _mergedRenderers = new List<KandraRenderer>();
        UnsafeList<UnsafeArray<ushort>> _addedBones;

        [ShowInInspector, NonSerialized, Sirenix.OdinInspector.ReadOnly]
        readonly List<KandraRenderer> _activeRenderers = new List<KandraRenderer>();

        UnsafeList<int> _addedBonesRefCount;
        [ShowInInspector]
        bool _isRegistered;

        void Awake() {
            EnsureInitialized();
            KandraRendererManager.Instance.RigManager.StopRigTracking(this);
        }

        void Start() {
            EnsureInitialized();
            KandraRendererManager.Instance.RigManager.StopRigTracking(this);
        }

        public void OnDestroy() {
            if(_addedBonesRefCount.IsCreated) {
                _addedBonesRefCount.Dispose();
                for (int i = 0; i < _addedBones.Length; i++) {
                    _addedBones[i].Dispose();
                }
                _addedBones.Dispose();
                _addedBones = default;
            }
        }

        public void EnsureInitialized() {
            if (!_addedBonesRefCount.IsCreated) {
                _addedBonesRefCount = new UnsafeList<int>(PreAllocatedAddedBones, ARAlloc.Persistent);
                _addedBones = new UnsafeList<UnsafeArray<ushort>>(PreAllocatedAddedBones, ARAlloc.Persistent);
                KandraRendererManager.Instance.RigManager.AddRigToTrack(this);
            }
        }

        public void RegisterActiveRenderer(KandraRenderer renderer) {
            _activeRenderers.Add(renderer);
        }

        public void UnregisterActiveRenderer(KandraRenderer renderer) {
            _activeRenderers.Remove(renderer);
        }

        public void MarkRegistered() {
            _isRegistered = true;
        }

        public void MarkUnregistered() {
            _isRegistered = false;
        }

        public void Merge(KandraRig otherRig, KandraRenderer renderer, ushort[] otherRendererBones, ref ushort otherRendererRootBone) {
            var bonesCatalog = CreateBonesMap(0.5f, ARAlloc.Temp);

            Merge(otherRig, renderer, otherRendererBones, ref bonesCatalog, ref otherRendererRootBone);

            bonesCatalog.Dispose();
        }

        public unsafe void Merge(KandraRig otherRig, KandraRenderer renderer, ushort[] otherRendererBones, ref UnsafeHashMap<FixedString64Bytes, ushort> bonesCatalog, ref ushort otherRendererRootBone) {
            MargeMarker.Begin();

            EnsureInitialized();
            var allNewBonesCount = (int)(bones.Length * ExpandFactor);

            var allBones = new List<Transform>(allNewBonesCount);
            allBones.AddRange(bones);
            var allBoneParents = new UnsafeList<ushort>(allNewBonesCount, ARAlloc.Temp);
            fixed (ushort* oldBoneParentsPtr = &boneParents[0]) {
                allBoneParents.AddRange(oldBoneParentsPtr, boneParents.Length);
            }
            var allBoneNames = new UnsafeList<FixedString64Bytes>(allNewBonesCount, ARAlloc.Temp);
            fixed (FixedString64Bytes* oldBoneNamesPtr = &boneNames[0]) {
                allBoneNames.AddRange(oldBoneNamesPtr, boneNames.Length);
            }

            var addedBones = new UnsafeList<ushort>(otherRendererBones.Length, ARAlloc.Temp);
            var changed = false;
            for (ushort i = 0; i < otherRig.bones.Length; i++) {
                var boneName = otherRig.boneNames[i];
                if (!bonesCatalog.TryGetValue(boneName, out var boneIndex)) {
                    var (bone, parentIndex) = CopyBone(otherRig, i, allBones, bonesCatalog);
                    allBones.Add(bone);
                    allBoneParents.Add(parentIndex);
                    allBoneNames.Add(boneName);
                    changed = true;
                    boneIndex = (ushort)(allBones.Count - 1);
                    bonesCatalog.TryAdd(boneName, boneIndex);
                }

                if (boneIndex >= baseBoneCount) {
                    addedBones.Add(boneIndex);
                    var index = boneIndex - baseBoneCount;
                    if (index == _addedBonesRefCount.Length) {
                        _addedBonesRefCount.Add(1);
                    } else if (index < _addedBonesRefCount.Length) {
                        var refCount = _addedBonesRefCount[index];
                        _addedBonesRefCount[index] = refCount + 1;
                    } else {
                        Log.Critical?.Error($"Added bone with super weird index {index}, don't know what happened.");
                    }
                }
            }

            _addedBones.Add(addedBones.ToUnsafeArray(Allocator.Persistent));
            addedBones.Dispose();

            if (changed) {
                bones = allBones.ToArray();

                boneNames = new FixedString64Bytes[allBoneNames.Length];
                CopyNativeToArray(boneNames, allBoneNames);

                boneParents = new ushort[allBoneParents.Length];
                CopyNativeToArray(boneParents, allBoneParents);
            }

            allBoneParents.Dispose();
            allBoneNames.Dispose();

            for (var i = 0; i < otherRendererBones.Length; i++) {
                var otherBoneIndex = otherRendererBones[i];
                var oldBoneName = otherRig.boneNames[otherBoneIndex];
                otherRendererBones[i] = bonesCatalog[oldBoneName];
            }
            { // RootBone
                var boneName = otherRig.boneNames[otherRendererRootBone];
                otherRendererRootBone = bonesCatalog[boneName];
            }

            if (changed & _isRegistered) {
                KandraRendererManager.Instance.RigChanged(this, _activeRenderers);
            }

            _mergedRenderers.Add(renderer);

            MargeMarker.End();
        }

        public unsafe void RemoveMerged(KandraRenderer renderer) {
            RemoveMergeMarker.Begin();
            if (!_addedBonesRefCount.IsCreated) {
                RemoveMergeMarker.End();
                return;
            }

            var rendererIndex = _mergedRenderers.IndexOf(renderer);
            if (rendererIndex == -1) {
                RemoveMergeMarker.End();
                return;
            }

            var usedAddedBones = _addedBones[rendererIndex];
            _mergedRenderers.RemoveAtSwapBack(rendererIndex);
            _addedBones.RemoveAtSwapBack(rendererIndex);

            var toDelete = new UnsafeList<ushort>(usedAddedBones.LengthInt, ARAlloc.Temp);
            for (var i = 0u; i < usedAddedBones.Length; ++i) {
                var index = usedAddedBones[i] - baseBoneCount;
                var refCount = _addedBonesRefCount[index];
                if (refCount == 1) {
                    toDelete.Add(usedAddedBones[i]);
                }
                _addedBonesRefCount[index] = refCount - 1;
            }

            usedAddedBones.Dispose();

            if (toDelete.Length == 0) {
                toDelete.Dispose();
                RemoveMergeMarker.End();
                return;
            }

            toDelete.Sort();

            var oldBones = bones;
            var newBones = new Transform[bones.Length - toDelete.Length];
            var newBoneParents = new ushort[bones.Length - toDelete.Length];
            var newBoneNames = new FixedString64Bytes[boneNames.Length - toDelete.Length];

            var startSourceIndex = 0;
            var startTargetIndex = 0;
            for (var i = 0; i < toDelete.Length; ++i) {
                var indexToSkip = toDelete[i];
                var count = indexToSkip - startSourceIndex;
                Array.Copy(bones, startSourceIndex, newBones, startTargetIndex, count);
                Array.Copy(boneParents, startSourceIndex, newBoneParents, startTargetIndex, count);
                Array.Copy(boneNames, startSourceIndex, newBoneNames, startTargetIndex, count);
                startSourceIndex = indexToSkip + 1;
                startTargetIndex += count;
            }

            {
                var count = oldBones.Length - startSourceIndex;
                Array.Copy(bones, startSourceIndex, newBones, startTargetIndex, count);
                Array.Copy(boneParents, startSourceIndex, newBoneParents, startTargetIndex, count);
                Array.Copy(boneNames, startSourceIndex, newBoneNames, startTargetIndex, count);
            }

            bones = newBones;
            boneParents = newBoneParents;
            boneNames = newBoneNames;

            for (int i = toDelete.Length - 1; i >= 0; i--) {
                var bone = oldBones[toDelete[i]];
                GameObjects.DestroySafely(bone.gameObject, true);
                _addedBonesRefCount.RemoveAt(toDelete[i] - baseBoneCount);
            }

            var oldBonesCount = oldBones.Length;
            var oldAddedBonesCount = oldBonesCount - baseBoneCount;
            var fixupMap = new UnsafeArray<ushort>((uint)oldAddedBonesCount, ARAlloc.Temp);

            for (ushort oldIndex = 0, newIndex = 0, toDeleteIndex = 0; oldIndex < oldAddedBonesCount; ++oldIndex) {
                fixupMap[oldIndex] = (ushort)(newIndex + baseBoneCount);

                if (toDeleteIndex == toDelete.Length || oldIndex != toDelete[toDeleteIndex] - baseBoneCount) {
                    ++newIndex;
                } else {
                    ++toDeleteIndex;
                }
            }

            for (int i = 0; i < _mergedRenderers.Count; i++) {
                var mergedRenderer = _mergedRenderers[i];

                // Renderer bones
                var rendererBones = mergedRenderer.rendererData.bones;
                var rendererBonesLength = rendererBones.Length;
                fixed (ushort* rendererBonesPtr = &rendererBones[0]) {
                    FixBonesAfterRemoval(rendererBonesLength, rendererBonesPtr, fixupMap, baseBoneCount);
                }

                // RootBone
                ref var rootBone = ref mergedRenderer.rendererData.rootBone;
                if (rootBone >= baseBoneCount) {
                    rootBone = fixupMap[(uint)(rootBone - baseBoneCount)];
                }

                // Added bones
                var addedBones = _addedBones[i];
                FixBonesAfterRemoval(addedBones.LengthInt, addedBones.Ptr, fixupMap, baseBoneCount);
            }

            toDelete.Dispose();
            fixupMap.Dispose();

            if (_isRegistered) {
                KandraRendererManager.Instance.RigChanged(this, _activeRenderers);
            }

            RemoveMergeMarker.End();
        }

        /// <summary> Runtime merged kandra renderers will be no longer RUNTIME merged. </summary>
        public void MarkAsBase() {
            baseBoneCount = (ushort)bones.Length;
            _mergedRenderers.Clear();
            
            for (int i = 0; i < _addedBones.Length; i++) {
                _addedBones[i].Dispose();
            }
            _addedBones.Clear();
            _addedBonesRefCount.Clear();
        }

        public UnsafeHashMap<FixedString64Bytes, ushort> CreateBonesMap(float additionalCapacity, Allocator allocator) {
            var allNewBonesCount = (int)(bones.Length * (1f + additionalCapacity));
            var bonesCatalog = new UnsafeHashMap<FixedString64Bytes, ushort>(allNewBonesCount, allocator);
            for (ushort i = 0; i < baseBoneCount; i++) {
                bonesCatalog.TryAdd(boneNames[i], i);
            }

            return bonesCatalog;
        }

        (Transform, ushort) CopyBone(KandraRig otherRig, ushort otherRigBoneIndex, List<Transform> allBones, in UnsafeHashMap<FixedString64Bytes, ushort> bonesCatalog) {
            CopyBoneMarker.Begin();

            var parentIndex = otherRig.boneParents[otherRigBoneIndex];

            Transform parent;
            ushort targetParentIndex;
            if (parentIndex == ushort.MaxValue) {
                parent = transform;
                targetParentIndex = ushort.MaxValue;
            } else {
                var parentName = otherRig.boneNames[parentIndex];
                targetParentIndex = bonesCatalog[parentName];
                parent = allBones[targetParentIndex];
            }

            string boneName = "CopiedBone";
#if UNITY_EDITOR
            boneName = otherRig.boneNames[otherRigBoneIndex].ToString();
#endif
            var copiedBoneGo = new GameObject(boneName);
            var copiedBone = copiedBoneGo.transform;
            var sourceBone = otherRig.bones[otherRigBoneIndex];

            copiedBone.SetParent(parent);
            sourceBone.GetLocalPositionAndRotation(out var localPosition, out var localRotation);
            copiedBone.SetLocalPositionAndRotation(localPosition, localRotation);
            copiedBone.localScale = sourceBone.localScale;

            CopyBoneMarker.End();

            return (copiedBone, targetParentIndex);
        }

        [BurstCompile]
        static unsafe void FixBonesAfterRemoval(int rendererBonesLength, ushort* rendererBonesPtr, in UnsafeArray<ushort> fixupMap, ushort baseBoneCount) {
            for (int j = 0; j < rendererBonesLength; j++) {
                if (rendererBonesPtr[j] < baseBoneCount) {
                    continue;
                }

                rendererBonesPtr[j] = fixupMap[(uint)(rendererBonesPtr[j] - baseBoneCount)];
            }
        }

        static unsafe void CopyNativeToArray<T>(T[] dest, UnsafeList<T> source) where T : unmanaged {
            fixed (T* destPtr = &dest[0]) {
                UnsafeUtility.MemCpy(destPtr, source.Ptr, source.Length * UnsafeUtility.SizeOf<T>());
            }
        }

        // === Odin
        // ReSharper disable UnusedMember.Local
        void BeginDrawBoneName(int index) {
            GUILayout.BeginHorizontal();
            GUILayout.Label(boneNames[index].ToString());
        }

        void EndDrawBoneName(int _) {
            GUILayout.EndHorizontal();
        }
        // ReSharper restore UnusedMember.Local
    }
}