using System;
using System.Runtime.CompilerServices;
using Awaken.Utility.Debugging;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Awaken.TG.Graphics.Culling {
    public struct DistanceCullerImpl : IDisposable {
        // -- Data
        NativeArray<BoundsCorners> _corners;
        NativeArray<int> _cullDistanceIndices;
        NativeArray<DistanceCullerData> _data;
        NativeList<int> _changedIndices;

        JobHandle _handle;

        int _nextElementIndex;

        // -- Profiling
        public NativeArray<int>.ReadOnly CullDistanceIndices => _cullDistanceIndices.AsReadOnly();
        public NativeArray<DistanceCullerData>.ReadOnly States => _data.AsReadOnly();
        public NativeArray<BoundsCorners>.ReadOnly Corners => _corners.AsReadOnly();
        public NativeArray<int>.ReadOnly ChangedIndices => _changedIndices.AsArray().AsReadOnly();
        public int ChangedCount => _changedIndices.Length;
        public int ChangedCapacity => _changedIndices.Capacity;
        public int NextElementIndex => _nextElementIndex;

        // === Lifetime
        public void Create(int count) {
            _corners = new(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _cullDistanceIndices = new(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _data = new(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            // Worst case scenario is everything changed so we need to allocate as much memory as it requires
            _changedIndices = new(count, Allocator.Persistent);
        }

        public void Dispose() {
            if (_corners.IsCreated) {
                _corners.Dispose();
            }
            if (_cullDistanceIndices.IsCreated) {
                _cullDistanceIndices.Dispose();
            }
            if (_data.IsCreated) {
                _data.Dispose();
            }
            if (_changedIndices.IsCreated) {
                _changedIndices.Dispose();
            }
        }

        // === Operations
        public int Register(in BoundsCorners corners, int distanceIndex) {
            _corners[_nextElementIndex] = corners;
            _cullDistanceIndices[_nextElementIndex] = distanceIndex;
            _data[_nextElementIndex] = new() { data = DistanceCullerData.IsVisibleMask };

            var registerIndex = _nextElementIndex;
            ++_nextElementIndex;
            return registerIndex;
        }

        public void UpdateBoundsData(int index, in BoundsCorners corners, int distanceIndex) {
            _corners[index] = corners;
            _cullDistanceIndices[index] = distanceIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateDistanceIndex(int elementIndex, int distanceIndex) {
            _cullDistanceIndices[elementIndex] = distanceIndex;
        }

        public void RemoveSwapBack(int toRemove) {
            var lastElement = _nextElementIndex - 1;
            RemoveSwapBack(_corners, toRemove, lastElement);
            RemoveSwapBack(_cullDistanceIndices, toRemove, lastElement);
            RemoveSwapBack(_data, toRemove, lastElement);
            --_nextElementIndex;
        }

        public void RemoveLast() {
            --_nextElementIndex;
        }

        // ===
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void RemoveSwapBack<T>(NativeArray<T> array, int toRemove, int lastElement) where T : struct {
            array[toRemove] = array[lastElement];
        }

        public JobHandle Execute(float3 cameraPosition, float3 cameraForward, NativeArray<float> cullDistancesSq) {
            _changedIndices.Clear();

            if (_nextElementIndex > 128) {
                _handle = new UpdateDistanceCullJob {
                    cameraPosition = cameraPosition,
                    cameraForward = cameraForward,
                    corners = _corners,
                    cullDistanceIndices = _cullDistanceIndices,
                    states = _data,
                    cullDistancesSq = cullDistancesSq,
                }.Schedule(_nextElementIndex, 64);

                _handle = new FilterChangedJob {
                    states = _data,
                    changedIndex = _changedIndices.AsParallelWriter(),
                }.Schedule(_nextElementIndex, 128, _handle);
            } else {
                _handle = default;
                if (_nextElementIndex != 0) {
                    new UpdateDistanceCullJob {
                        cameraPosition = cameraPosition,
                        cameraForward = cameraForward,
                        corners = _corners,
                        cullDistanceIndices = _cullDistanceIndices,
                        states = _data,
                        cullDistancesSq = cullDistancesSq,
                    }.Run(_nextElementIndex);

                    new FilterChangedJob {
                        states = _data,
                        changedIndex = _changedIndices.AsParallelWriter(),
                    }.Run(_nextElementIndex);
                }
            }

            return _handle;
        }

        [UnityEngine.Scripting.Preserve]
        public void CheckChanged() {
            for (int i = 0; i < _nextElementIndex; i++) {
                if (_data[i].HasChange() && !_changedIndices.Contains(i)) {
                    Log.Important?.Error($"Index {i} has change in data but is not listed");
                }
            }
        }

        // === Jobs
        [BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
        struct UpdateDistanceCullJob : IJobParallelFor {
            // We dont want to hide close renderers behind because of shadows.
            // 6% of the frontal distance was chosen by manual process of scene behaviour probing
            const float MultiplierBehind = 0.06f;
            const float MultiplierAside = 0.8f;
            const float MultiplierInFront = 1f;
            static readonly float[] MultipliersSq = {
                MultiplierBehind * MultiplierBehind, MultiplierAside * MultiplierAside,
                MultiplierInFront * MultiplierInFront,
            };
            
            public float3 cameraPosition;
            public float3 cameraForward;

            [ReadOnly, NativeDisableContainerSafetyRestriction]
            public NativeArray<BoundsCorners> corners;
            [ReadOnly, NativeDisableContainerSafetyRestriction]
            public NativeArray<int> cullDistanceIndices;
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<DistanceCullerData> states;

            [ReadOnly, NativeDisableContainerSafetyRestriction]
            public NativeArray<float> cullDistancesSq;

            public void Execute(int index) {
                var state = states[index];
                var myCorners = corners[index];
                var cullDistanceSq = cullDistancesSq[cullDistanceIndices[index]];

                int visible = DistanceCullerData.NothingInt;
                for (var i = 0; i < BoundsCorners.Count && visible == DistanceCullerData.NothingInt; i++) {
                    var direction = myCorners.Get(i) - cameraPosition;
                    var distanceSq = math.lengthsq(direction);
                    // Dot tells if point is behind us (-value) or ahead (+value)
                    var dotSign = (int)math.sign(math.dot(cameraForward, direction));
                    // Change sign value to be index into array
                    var multiplierIndex = dotSign+1;
                    var multiplier = MultipliersSq[multiplierIndex];
                    
                    var cullDistance = cullDistanceSq * multiplier;
                    visible = math.select(DistanceCullerData.NothingInt, DistanceCullerData.IsVisibleMaskInt,
                        distanceSq <= cullDistance);
                }

                var hasChange = math.select(DistanceCullerData.HasChangeMask, DistanceCullerData.NothingInt,
                    (state.data & DistanceCullerData.IsVisibleMask) == visible);

                state.data = (byte)(hasChange + visible);
                states[index] = state;
            }
        }

        [BurstCompile]
        struct FilterChangedJob : IJobParallelFor {
            [ReadOnly, NativeDisableContainerSafetyRestriction]
            public NativeArray<DistanceCullerData> states;
            [NativeDisableContainerSafetyRestriction]
            public NativeList<int>.ParallelWriter changedIndex;

            public void Execute(int index) {
                if (states[index].HasChange()) {
                    changedIndex.AddNoResize(index);
                }
            }
        }
    }
}
