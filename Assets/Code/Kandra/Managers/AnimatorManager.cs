using System;
using Awaken.CommonInterfaces.Animations;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.LowLevel.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace Awaken.Kandra.Managers {
    public class AnimatorManager : IMemorySnapshotProvider {
        // -- Per animator unordered
        UnsafeHashMap<int, AnimatorData> _animatorData;
        // -- Per animator linear space
        AnimatorBridge[] _animators;
        UnsafeBitmask _animatorStates;
        UnsafeBitmask _takenAnimators;
        // -- Per renderer
        UnsafeArray<byte> _previousVisibility;
        UnsafeArray<int> _registeredAnimatorHash;

        readonly SkinnedBatchRenderGroup _skinnedBatchRenderGroup;

        uint _previousTakenVisibility;

        public AnimatorManager(SkinnedBatchRenderGroup skinnedBatchRenderGroup) {
            var maxRenderers = (uint)KandraRendererManager.FinalRenderersCapacity;

            _skinnedBatchRenderGroup = skinnedBatchRenderGroup;

            _animatorData = new UnsafeHashMap<int, AnimatorData>((int)maxRenderers, ARAlloc.Persistent);
            _animators = new AnimatorBridge[maxRenderers];

            _animatorStates = new UnsafeBitmask(maxRenderers, ARAlloc.Persistent);
            _takenAnimators = new UnsafeBitmask(maxRenderers, ARAlloc.Persistent);

            _previousVisibility = new UnsafeArray<byte>(maxRenderers, ARAlloc.Persistent);
            _registeredAnimatorHash = new UnsafeArray<int>(maxRenderers, ARAlloc.Persistent);
        }

        public void Dispose() {
            foreach (var pair in _animatorData) {
                pair.Value.Dispose();
            }
            _animatorData.Dispose();
            _animators = null;
            _animatorStates.Dispose();
            _takenAnimators.Dispose();
            _previousVisibility.Dispose();
            _registeredAnimatorHash.Dispose();
        }

        public void RegisterAnimator(uint rendererId, Animator animator) {
#if UNITY_EDITOR
            if (Application.isPlaying == false) {
                animator = null;
            }
#endif
            if (animator == null) {
                return;
            }

            var hash = animator.GetHashCode();
            _registeredAnimatorHash[rendererId] = hash;
            _previousVisibility[rendererId] = 2;

            if (_animatorData.TryGetValue(hash, out var data)) {
                data.rendererIndices.Add(rendererId);
                _animatorData[hash] = data;
                return;
            }

            var bridge = AnimatorBridge.GetOrAddDefault(animator);
            var animatorSlot = (uint)_takenAnimators.FirstZero();
            _animators[animatorSlot] = bridge;
            _takenAnimators.Up(animatorSlot);

            data = new AnimatorData(animatorSlot, rendererId);

            _animatorData.TryAdd(hash, data);
        }

        public void UnregisterAnimator(uint rendererId) {
            var hash = _registeredAnimatorHash[rendererId];
            _registeredAnimatorHash[rendererId] = 0;

            if (hash == 0) {
                return;
            }

            if (_animatorData.TryGetValue(hash, out var data)) {
                var indexToRemove = data.rendererIndices.IndexOf(rendererId);
                data.rendererIndices.RemoveAtSwapBack(indexToRemove);

                if (data.RefCount == 0) {
                    _animatorData.Remove(hash);
                    _takenAnimators.Down(data.index);
                    _animators[data.index] = null;
                    data.Dispose();
                } else {
                    _animatorData[hash] = data;
                }
            }
        }

        public unsafe void UpdateAnimators(in UnsafeBitmask takenSlots, in UnsafeBitmask toUnregister) {
            var cameraVisibility = _skinnedBatchRenderGroup.cameraSplitMaskVisibility;
            var lightVisibility = _skinnedBatchRenderGroup.lightsAggregatedSplitMaskVisibility;

            _previousTakenVisibility = (uint)(takenSlots.LastOne() + 1);

            if (_previousTakenVisibility == 0) {
                UnsafeUtility.MemClear(lightVisibility.Ptr, lightVisibility.Length * sizeof(ushort));
                return;
            }

            var changedVisibility = new UnsafeBitmask(_previousTakenVisibility, ARAlloc.TempJob);
            new FilterChangedVisibilityJob {
                cameraVisibility = cameraVisibility,
                lightVisibility = lightVisibility,
                takenSlots = takenSlots,
                toUnregister = toUnregister,

                outPreviousVisibility = _previousVisibility,
                outChangedVisibility = changedVisibility
            }.Run((int)_previousTakenVisibility);

            var animatorsDataValues = _animatorData.GetValueArray(ARAlloc.TempJob);
            var changedAnimators = new NativeList<int>(animatorsDataValues.Length, ARAlloc.TempJob);
            new FilterChangedAnimatorsJob {
                animatorData = animatorsDataValues,
                changedVisibility = changedVisibility,
                visibility = _previousVisibility,
                outAnimatorState = _animatorStates
            }.RunAppend(changedAnimators, animatorsDataValues.Length);
            changedVisibility.Dispose();

            var changedAnimatorsCount = changedAnimators.Length;
            var changeAnimatorsPtr = changedAnimators.GetUnsafeReadOnlyPtr();
            for (var i = 0; i < changedAnimatorsCount; i++) {
                var changedAnimatorDataIndex = changeAnimatorsPtr[i];
                var index = animatorsDataValues[changedAnimatorDataIndex].index;
                var animator = _animators[index];
                if (!animator.IsValid) {
                    Log.Critical?.Error("Discarded animator in AnimatorManager. More info in warnings");
                    var logDebug = Log.Debug;
                    if (logDebug != null) {
                        foreach (var rendererIndex in animatorsDataValues[changedAnimatorDataIndex].rendererIndices) {
                            var renderer = KandraRendererManager.Instance.ActiveRenderers[rendererIndex];
                            logDebug.Warning($"AnimatorManager: Renderer: {renderer} has registered discarded animator {animator}.", renderer);
                        }
                    }
                }
                animator.SetNonUnityVisible(_animatorStates[index]);
            }

            animatorsDataValues.Dispose();
            changedAnimators.Dispose();

            UnsafeUtility.MemClear(lightVisibility.Ptr, lightVisibility.Length * sizeof(ushort));
        }

        public struct AnimatorData {
            const int InitialCapacity = 6;

            public readonly uint index;
            public UnsafeList<uint> rendererIndices;

            // We don't need to store refCount separately, we can just use rendererIndices.Length
            public int RefCount => rendererIndices.Length;

            public AnimatorData(uint index, uint firstRenderer) {
                this.index = index;
                rendererIndices = new UnsafeList<uint>(InitialCapacity, ARAlloc.Persistent);
                rendererIndices.Add(firstRenderer);
            }

            public void Dispose() {
                rendererIndices.Dispose();
            }
        }

        [BurstCompile]
        struct FilterChangedVisibilityJob : IJobFor {
            [ReadOnly] public UnsafeArray<ushort> cameraVisibility;
            [ReadOnly] public UnsafeArray<ushort> lightVisibility;
            [ReadOnly] public UnsafeBitmask takenSlots;
            [ReadOnly] public UnsafeBitmask toUnregister;

            public UnsafeArray<byte> outPreviousVisibility;
            [WriteOnly] public UnsafeBitmask outChangedVisibility;

            public void Execute(int index) {
                var uIndex = (uint)index;
                if (!takenSlots[uIndex] | toUnregister[uIndex]) {
                    return;
                }

                var currentVisibility = (byte)(cameraVisibility[uIndex] | lightVisibility[uIndex]);
                currentVisibility = currentVisibility != 0 ? (byte)1 : (byte)0;
                if (currentVisibility != outPreviousVisibility[uIndex]) {
                    outPreviousVisibility[uIndex] = currentVisibility;
                    outChangedVisibility.Up(uIndex);
                }
            }
        }

        [BurstCompile]
        struct FilterChangedAnimatorsJob : IJobFilter {
            [ReadOnly] public NativeArray<AnimatorData> animatorData;
            [ReadOnly] public UnsafeBitmask changedVisibility;
            [ReadOnly] public UnsafeArray<byte> visibility;

            [WriteOnly] public UnsafeBitmask outAnimatorState;

            public bool Execute(int animatorIndex) {
                var animator = animatorData[animatorIndex];
                var rendererIndices = animator.rendererIndices;

                var changed = (byte)0;
                var isVisible = (byte)0;
                for (int i = 0; i < rendererIndices.Length; i++) {
                    var rendererIndex = rendererIndices[i];
                    changed += changedVisibility[rendererIndex] ? (byte)1 : (byte)0;
                    isVisible += visibility[rendererIndex];
                }

                outAnimatorState[animator.index] = isVisible != 0;
                return changed != 0;
            }
        }

        public int GetMemorySnapshot(Memory<MemorySnapshot> memory, Memory<MemorySnapshot> ownPlace) {
            var selfAnimatorsData = MemorySnapshotUtils.HashMapSize<int, AnimatorData>(_animatorData.Capacity);
            var selfAnimators = (ulong)(_animators.Length * IntPtr.Size);
            var selfAnimatorStates = (ulong)(_animatorStates.BucketsLength * sizeof(ulong));
            var selfTakenAnimators = (ulong)(_takenAnimators.BucketsLength * sizeof(ulong));
            var selfPreviousVisibility = (ulong)(_previousVisibility.Length * sizeof(byte));
            var selfSize = selfAnimatorsData + selfAnimators + selfAnimatorStates + selfTakenAnimators + selfPreviousVisibility;

            var animatorsInUse = (uint)_animatorData.Count;
            var usedAnimatorsData = MemorySnapshotUtils.HashMapSize<int, AnimatorData>(animatorsInUse);
            var usedAnimators = (ulong)(animatorsInUse * IntPtr.Size);
            var usedAnimatorStates = MemorySnapshotUtils.BitsToBytes(animatorsInUse);
            var usedTakenAnimators = MemorySnapshotUtils.BitsToBytes(animatorsInUse);
            var usedPreviousVisibility = (ulong)(_previousTakenVisibility * sizeof(byte));
            var usedSize = usedAnimatorsData + usedAnimators + usedAnimatorStates + usedTakenAnimators + usedPreviousVisibility;

            ownPlace.Span[0] = new MemorySnapshot(nameof(AnimatorManager), selfSize, usedSize);

            return 0;
        }
    }
}