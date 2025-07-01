using System;
using Awaken.Utility.LowLevel.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Awaken.TG.Graphics.Culling {
    [BurstCompile]
    public struct CalculateDistanceVisibilityStateJob : IJobParallelFor {
        // Using float4 instead of float3 for better memory alignment
        [ReadOnly] public UnsafeArray<float4>.Span positions;
        [ReadOnly] public NativeArray<float> drawDistancesSqs;
        public float4 cameraPosition;
        public float hdrpVisibilityDistanceSq;
        public float distanceMultiplierSq;
        [NativeDisableParallelForRestriction] public NativeArray<byte> outVisibilityStates;

        public void Execute(int index) {
            var distanceSq = math.min(drawDistancesSqs[index], hdrpVisibilityDistanceSq) * distanceMultiplierSq;

            ExecuteElement(positions[(uint)index], cameraPosition, distanceSq, outVisibilityStates[index],
                out var newVisibilityState);
            outVisibilityStates[index] = newVisibilityState;
        }

        public static void ExecuteElement(float4 cameraPosition, float4 position, float visibilityDistanceSq,
            byte visibilityState, out byte newVisibilityState) {
            var distanceSq = math.distancesq(cameraPosition, position);
            var distanceDiff = visibilityDistanceSq - distanceSq;
            // Extracting sign bit and inverting it so that if distanceDiff
            // is positive (so sign bit is 0) - isVisibleState bit is 1
            int isDiffNegativeBit0 = ((BitConverter.SingleToInt32Bits(distanceDiff) >> 31) & 1);
            var isVisibleBit0 = (isDiffNegativeBit0 ^ 1);
            // Extract prev isVisibleState
            int prevIsVisibleBit0 = (visibilityState & 1);
            // Setting second bit value to 1 if state changed.
            // Xor ^ results in 1 if two bit values are not equal.
            var isVisibilityChangedBit1 = ((prevIsVisibleBit0 ^ isVisibleBit0) << 1);
            var initialStateOtherBits = (visibilityState & 0b1111_1100);
            newVisibilityState = (byte)(initialStateOtherBits | isVisibleBit0 | isVisibilityChangedBit1);
        }
    }
}