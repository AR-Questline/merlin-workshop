using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.Utility.Maths.Data {
    /// <summary>
    /// Axis aligned bounding rectangle with min and max points
    /// </summary>
    [Serializable]
    public struct MinMaxAABR : IEquatable<MinMaxAABR> {
        public float2 min;
        public float2 max;

        public static MinMaxAABR Empty => new() { min = new float2(float.PositiveInfinity), max = new float2(float.NegativeInfinity) };

        public readonly float2 Extents => max - min;
        public readonly float2 HalfExtents => (max - min) * 0.5f;
        public readonly float2 Center => (max + min) * 0.5f;
        public readonly bool IsValid => math.all(min <= max);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MinMaxAABR(float2 min, float2 max) {
            this.min = min;
            this.max = max;
        }

        public MinMaxAABR(Bounds bounds) {
            this.min = new float2(bounds.min.x, bounds.min.z);
            this.max = new float2(bounds.max.x, bounds.max.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Contains(float2 point) {
            return math.all(point >= min & point <= max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Contains(MinMaxAABR aabb) {
            return math.all((min <= aabb.min) & (max >= aabb.max));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Overlaps(MinMaxAABR aabb) {
            return math.all(max >= aabb.min & min <= aabb.max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Expand(float signedDistance) {
            min -= signedDistance;
            max += signedDistance;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Encapsulate(MinMaxAABR aabb) {
            min = math.min(min, aabb.min);
            max = math.max(max, aabb.max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Encapsulate(float2 point) {
            min = math.min(min, point);
            max = math.max(max, point);
        }

        public readonly void FillCorners(ref Span<float2> corners) {
            corners[0] = new float2(min.x, min.y);
            corners[1] = new float2(max.x, min.y);
            corners[2] = new float2(max.x, max.y);
            corners[3] = new float2(min.x, max.y);
        }

        public readonly AABR ToAABR() {
            return new AABR(Center, Extents);
        }

        public MinMaxAABB ToMinMaxAABB(float minY, float maxY) {
            return new MinMaxAABB() { Min = min.xcy(minY), Max = max.xcy(maxY), };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(MinMaxAABR other) {
            return min.Equals(other.min) && max.Equals(other.max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override string ToString() {
            return $"MinMaxAABR({min}, {max})";
        }

        public override bool Equals(object obj) {
            return obj is MinMaxAABR other && Equals(other);
        }

        public override int GetHashCode() {
            unchecked {
                return (min.GetHashCode() * 397) ^ max.GetHashCode();
            }
        }
    }
}
