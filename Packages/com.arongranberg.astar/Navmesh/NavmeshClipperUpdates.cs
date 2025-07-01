using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Jobs;

namespace Pathfinding {
    public class NavmeshClipperUpdates {
        static readonly float3 ResetPosition = new float3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        
        TransformAccessArray _cutsTransformsAccess;
        NativeList<byte> _requiresUpdate;

        NativeList<float3> _previousPositions;
        NativeList<quaternion> _previousRotations;
        NativeList<float> _updateDistancesSq;
        NativeBitArray _useRotationsAndScales;
        NativeList<float> _updateRotationDistance;
        NativeReference<byte> _requiresUpdatesFlag;

        NativeList<int> _freeIds;
        Dictionary<NavmeshClipper, int> _indexByClipper;

        JobHandle _ongoingJob;

        public NavmeshClipperUpdates(int capacity) {
        }

        public void Dispose()
        {
        }

        public void Register(NavmeshClipper clipper)
        {
        }

        public void Unregister(NavmeshClipper clipper)
        {
        }

        public void ScheduleUpdateRequiresUpdate()
        {
        }

        public bool RequiresUpdate(out SingularUpdateRequirement singularUpdate)
        {
            singularUpdate = default(SingularUpdateRequirement);
            return default;
        }

        bool RequiresUpdate(NavmeshClipper clipper)
        {
            return default;
        }

        public void CleanRequiresUpdate() {
        }

        public void ForceUpdate(NavmeshClipper clipper)
        {
        }

        int BitArrayCapacity(int numbBits)
        {
            return default;
        }

        [BurstCompile]
        public struct CheckIfRequiresUpdate : IJobParallelForTransform {
            [ReadOnly] public NativeArray<float> updateDistancesSq;
            [ReadOnly] public NativeBitArray useRotationsAndScales;
            [ReadOnly] public NativeArray<float> updateRotationDistance;

            [WriteOnly, NativeDisableParallelForRestriction]
            public NativeReference<byte> updateCounter;

            public NativeArray<byte> requiresUpdate;
            public NativeArray<float3> previousPositions;
            public NativeArray<quaternion> previousRotations;

            public void Execute(int index, TransformAccess transform)
            {
            }

            static float Angle(quaternion a, quaternion b)
            {
                return default;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool IsEqualUsingDot(float dot) => dot > 0.9999989867210388f;
        }

        public readonly struct SingularUpdateRequirement
        {
            readonly NavmeshClipperUpdates _updates;

            public SingularUpdateRequirement(NavmeshClipperUpdates updates) : this()
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool RequiresUpdate(NavmeshClipper clipper) {
                return default;
            }
        }
    }
}