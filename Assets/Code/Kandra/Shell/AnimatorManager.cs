using System;
using Awaken.CommonInterfaces.Animations;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.LowLevel.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Awaken.Kandra.Managers {
    public class AnimatorManager : IMemorySnapshotProvider {
        UnsafeHashMap<int, AnimatorData> _animatorData;
        AnimatorBridge[] _animators;
        UnsafeBitmask _animatorStates;
        UnsafeBitmask _takenAnimators;
        UnsafeArray<byte> _previousVisibility;
        UnsafeArray<int> _registeredAnimatorHash;

        readonly SkinnedBatchRenderGroup _skinnedBatchRenderGroup;

        uint _previousTakenVisibility;

        public AnimatorManager(SkinnedBatchRenderGroup skinnedBatchRenderGroup) {
            throw new NotImplementedException();
        }

        public void Dispose() {
            throw new NotImplementedException();
        }

        public void RegisterAnimator(uint rendererId, Animator animator) {
            throw new NotImplementedException();
        }

        public void UnregisterAnimator(uint rendererId) {
            throw new NotImplementedException();
        }

        public void UpdateAnimators(in UnsafeBitmask takenSlots, in UnsafeBitmask toUnregister) {
            throw new NotImplementedException();
        }

        public struct AnimatorData {
            public readonly uint index;
            public UnsafeList<uint> rendererIndices;
            public int RefCount;

            public AnimatorData(uint index, uint firstRenderer) {
                throw new NotImplementedException();
            }

            public void Dispose() {
                throw new NotImplementedException();
            }
        }

        public int GetMemorySnapshot(Memory<MemorySnapshot> memory, Memory<MemorySnapshot> ownPlace) {
            throw new NotImplementedException();
        }
    }
}