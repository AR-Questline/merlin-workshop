using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Graphics.Culling {
    // We can trade memory footprint (and store just min/max point) for CPU time (and corners every time)
    [BurstCompile]
    public struct BoundsCorners {
        public const int Count = 8;
        
        public float3 leftBottomFront;
        public float3 rightBottomFront;
        public float3 rightBottomBack;
        public float3 leftBottomBack;
        public float3 leftTopFront;
        public float3 rightTopFront;
        public float3 rightTopBack;
        public float3 leftTopBack;

        public BoundsCorners(float3 min, float3 max) {
            leftBottomFront = new(min.x, min.y, max.z);
            rightBottomFront = new(max.x, min.y, max.z);
            rightBottomBack = new(max.x, min.y, min.z);
            leftBottomBack = new(min.x, min.y, min.z);
            leftTopFront = new(min.x, max.y, max.z);
            rightTopFront = new(max.x, max.y, max.z);
            rightTopBack = new(max.x, max.y, min.z);
            leftTopBack = new(min.x, max.y, min.z);
        }
        
        [UnityEngine.Scripting.Preserve]
        public BoundsCorners(Bounds bounds) : this(bounds.min, bounds.max) {}

        [BurstCompile]
        public float3 Get(int index) {
            return index switch {
                0 => leftBottomFront,
                1 => rightBottomFront,
                2 => rightBottomBack,
                3 => leftBottomBack,
                4 => leftTopFront,
                5 => rightTopFront,
                6 => rightTopBack,
                _ => leftTopBack,
            };
        }

        [BurstCompile]
        public Bounds GetBounds() {
            var bounds = new Bounds();
            bounds.SetMinMax(leftBottomBack, rightTopFront);
            return bounds;
        }
    }
}
