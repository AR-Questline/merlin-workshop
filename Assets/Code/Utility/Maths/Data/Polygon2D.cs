using System;
using Awaken.Utility.Collections;
using Awaken.Utility.LowLevel.Collections;
using Unity.Mathematics;

namespace Awaken.Utility.Maths.Data {
    public struct Polygon2D : IEquatable<Polygon2D> {
        public static Polygon2D Invalid => new(default, MinMaxAABR.Empty);

        public UnsafeArray<float2> points;
        public readonly MinMaxAABR bounds;

        public uint Length => points.Length;
        public ref float2 this[uint index] => ref points[index];

        public bool IsCreated => points.IsCreated;

        public Polygon2D(UnsafeArray<float2> points, MinMaxAABR bounds) {
            this.points = points;
            this.bounds = bounds;
        }

        public void Dispose() {
            points.Dispose();
        }

        public void CheckedDispose() {
            if (points.IsCreated) {
                points.Dispose();
                points = default;
            }
        }

        public bool Equals(Polygon2D other) {
            return bounds.Equals(other.bounds) &&
                   points.SequenceEqual(other.points);
        }

        public override bool Equals(object obj) {
            return obj is Polygon2D other && Equals(other);
        }

        public override int GetHashCode() {
            unchecked {
                return (points.SequenceHashCode() * 397) ^ bounds.GetHashCode();
            }
        }
    }
}
