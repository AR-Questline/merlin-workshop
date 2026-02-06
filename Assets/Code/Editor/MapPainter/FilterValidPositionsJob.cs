using Awaken.Utility.Maths;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Editor.MapPainter {
    [BurstCompile]
    public struct FilterValidPositionsJob : IJobFor {
        [ReadOnly] public NativeArray<RaycastHit> raycastResults;
        [ReadOnly] public byte maxHits;
        [ReadOnly] public float3 brushCenter;
        [ReadOnly] public float brushSizeSq;
        [ReadOnly] public float minSpawnDistance;
        [ReadOnly] public bool useSlopeFilter;
        [ReadOnly] public float2 slopeRangeRad;
        [ReadOnly] public bool useHeightFilter;
        [ReadOnly] public float2 heightRange;

        public NativeList<int> validIndices;
        
        public void Execute(int index) {
            for (int i = 0; i < maxHits; i++) {
                var hitIndex = maxHits * index + i;
                var hit = raycastResults[hitIndex];

                if (hit.colliderInstanceID == 0) {
                    return;
                }

                // Brush size filter
                float distanceFromCenterSq = math.distancesq(brushCenter, hit.point);
                if (distanceFromCenterSq > brushSizeSq) {
                    continue;
                }
                
                // Check slope filter
                if (useSlopeFilter) {
                    float slope = mathExt.angle(hit.normal, math.up());
                    if (slope < slopeRangeRad.x || slope > slopeRangeRad.y) {
                        continue;
                    }
                }
                
                // Check height filter
                if (useHeightFilter) {
                    if (hit.point.y < heightRange.x || hit.point.y > heightRange.y) {
                        continue;
                    }
                }
                
                validIndices.AddNoResize(hitIndex);
                break;
            }
        }
    }
}