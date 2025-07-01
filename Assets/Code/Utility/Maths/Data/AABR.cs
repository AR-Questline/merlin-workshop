using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Awaken.Utility.Maths.Data {
    /// <summary>
    /// Axis aligned bounding rectangle with center and extents
    /// </summary>
    [Serializable]
    public struct AABR {
        public float2 center;
        public float2 extents;

        public readonly float2 Min => center - extents;
        public readonly float2 Max => center + extents;

        public AABR(float2 center, float2 extents) {
            this.center = center;
            this.extents = extents;
        }

        public readonly bool Contains(float2 point) {
            return !math.any(point < Min | Max < point);
        }

        public void Encapsulate(float2 point) {
            var min = math.min(Min, point);
            var max = math.max(Max, point);
            center = (min + max) * 0.5f;
            extents = (max - min) * 0.5f;
        }

        public void Encapsulate(AABR other) {
            var min = math.min(Min, other.Min);
            var max = math.max(Max, other.Max);
            center = (min + max) * 0.5f;
            extents = (max - min) * 0.5f;
        }

        public readonly float DistanceSq(float2 point) {
            return math.lengthsq(math.max(math.abs(point - center), extents) - extents);
        }

        public readonly MinMaxAABR ToMinMaxAABR() {
            return new MinMaxAABR(Min, Max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(AABR other) {
            return center.Equals(other.center) && extents.Equals(other.extents);
        }

        public readonly override string ToString() {
            return $"AABR(C:{center}, E:{extents})";
        }
    }
}
