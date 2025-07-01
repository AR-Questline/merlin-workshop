using Awaken.Utility.LowLevel.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Jobs;

namespace Awaken.TG.Graphics.Culling {
    [BurstCompile]
    struct CollectPositionsJob : IJobParallelForTransform {
        public UnsafeArray<float3>.Span scales;
        public UnsafeArray<float3>.Span pivots;

        [WriteOnly] public UnsafeArray<float4>.Span outPositions;

        public void Execute(int index, TransformAccess transform) {
            transform.GetPositionAndRotation(out var position, out var rotation);
            var scale = scales[(uint)index];

            var trs = float4x4.TRS(position, rotation, scale);

            var outputPosition = math.mul(trs, new float4(pivots[(uint)index], 1));

            outPositions[(uint)index] = new float4(outputPosition.x, outputPosition.y, outputPosition.z, 0);
        }
    }
}