using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.Utility.Maths {
    public static class BoundsUtils {
        public static Bounds Expanded(this Bounds bounds, float amount) {
            bounds.Expand(amount);
            return bounds;
        }

        public static Bounds Expanded(this Bounds bounds, Vector3 amount) {
            bounds.Expand(amount);
            return bounds;
        }
        public static Bounds Expanded(this Bounds bounds, float x, float y, float z) {
            return Expanded(bounds, new Vector3(x, y, z));
        }

        public static Bounds OverridenByY(this in Bounds bounds, float y, float height) {
            return new Bounds(
                new Vector3(bounds.center.x, y, bounds.center.z),
                new Vector3(bounds.size.x, height, bounds.size.z)
            );
        }

        public static void Encapsulate(this ref Bounds? bounds, in Bounds other) {
            if (bounds.HasValue) {
                var value = bounds.Value;
                value.Encapsulate(other);
                bounds = value;
            } else {
                bounds = other;
            }
        }

        public static void Encapsulate(this ref Bounds? bounds, Vector3 point) {
            if (bounds.HasValue) {
                var value = bounds.Value;
                value.Encapsulate(point);
                bounds = value;
            } else {
                bounds = new Bounds(point, Vector3.zero);
            }
        }

        public static bool Contains(this in Bounds? bounds, Vector3 point) {
            if (bounds.HasValue) {
                return bounds.Value.Contains(point);
            } else {
                return false;
            }
        }
        
        public static float Volume(this in Bounds bounds) {
            return bounds.size.x * bounds.size.y * bounds.size.z;
        }
        
        public static float VolumeOfAverage(this in Bounds bounds) {
            var avg = (bounds.size.x + bounds.size.y + bounds.size.z)/3f;
            return avg * avg * avg;
        }
        
        public static float VolumeOfAverage(float3 size) {
            var avg = (size.x + size.y + size.z)/3f;
            return avg * avg * avg;
        }
        
        public static Bounds Intersection(this in Bounds bounds, in Bounds other) {
            var min = Vector3.Max(bounds.min, other.min);
            var max = Vector3.Min(bounds.max, other.max);
            var output = new Bounds();
            output.SetMinMax(min, max);
            return output;
        }

        /// <summary>
        /// From local to world
        /// </summary>
        public static Bounds ToWorld(this Bounds localBounds, Transform transform) {
            return localBounds.Transform(transform.localToWorldMatrix);
        }

        /// <summary>
        /// From world to local
        /// </summary>
        public static Bounds ToLocal(this Bounds worldBounds, Transform transform) {
            return worldBounds.Transform(transform.worldToLocalMatrix);
        }
        
        public static Bounds Transform(this Bounds bounds, Matrix4x4 matrix) {
            Span<Vector3> corners = stackalloc Vector3[8];
            bounds.Corners(corners);
            
            for (var i = 0; i < corners.Length; i++) {
                corners[i] = matrix.MultiplyPoint3x4(corners[i]);
            }

            return Containing(corners);
        }

        public static Bounds Containing(Span<Vector3> points) {
            var min = points[0];
            var max = points[0];
            
            for (var i = 1; i < points.Length; i++) {
                min.x = Mathf.Min(min.x, points[i].x);
                min.y = Mathf.Min(min.y, points[i].y);
                min.z = Mathf.Min(min.z, points[i].z);
                
                max.x = Mathf.Max(max.x, points[i].x);
                max.y = Mathf.Max(max.y, points[i].y);
                max.z = Mathf.Max(max.z, points[i].z);
            }

            var newBounds = new Bounds();
            newBounds.SetMinMax(min, max);
            return newBounds;
        }

        public static void Corners(this Bounds bounds, Span<Vector3> output) {
            output[0] = bounds.min;
            output[1] = new(bounds.max.x, bounds.min.y, bounds.min.z);
            output[2] = new(bounds.max.x, bounds.min.y, bounds.max.z);
            output[3] = new(bounds.min.x, bounds.min.y, bounds.max.z);
            output[4] = bounds.max;
            output[5] = new(bounds.max.x, bounds.max.y, bounds.min.z);
            output[6] = new(bounds.min.x, bounds.max.y, bounds.min.z);
            output[7] = new(bounds.min.x, bounds.max.y, bounds.max.z);
        }

        public static void Corners(this Bounds bounds, NativeArray<Vector3> output) {
            output[0] = bounds.min;
            output[1] = new(bounds.max.x, bounds.min.y, bounds.min.z);
            output[2] = new(bounds.max.x, bounds.min.y, bounds.max.z);
            output[3] = new(bounds.min.x, bounds.min.y, bounds.max.z);
            output[4] = bounds.max;
            output[5] = new(bounds.max.x, bounds.max.y, bounds.min.z);
            output[6] = new(bounds.min.x, bounds.max.y, bounds.min.z);
            output[7] = new(bounds.min.x, bounds.max.y, bounds.max.z);
        }

        public static MinMaxAABB Transform(this in MinMaxAABB bounds, in float4x4 matrix) {
            var min = bounds.Min;
            var max = bounds.Max;

            Span<float4> points = stackalloc[] {
                new float4(min.x, min.y, min.z, 1),
                new float4(max.x, min.y, min.z, 1),
                new float4(min.x, min.y, max.z, 1),
                new float4(max.x, min.y, max.z, 1),
                new float4(min.x, max.y, min.z, 1),
                new float4(max.x, max.y, min.z, 1),
                new float4(min.x, max.y, max.z, 1),
                new float4(max.x, max.y, max.z, 1),
            };

            for (int i = 0; i < points.Length; ++i) {
                points[i] = math.mul(matrix, points[i]);
            }

            var newMin = points[0];
            var newMax = points[0];

            for (int i = 1; i < points.Length; ++i) {
                if (newMin.x > points[i].x) newMin.x = points[i].x;
                if (newMax.x < points[i].x) newMax.x = points[i].x;

                if (newMin.y > points[i].y) newMin.y = points[i].y;
                if (newMax.y < points[i].y) newMax.y = points[i].y;

                if (newMin.z > points[i].z) newMin.z = points[i].z;
                if (newMax.z < points[i].z) newMax.z = points[i].z;
            }


            MinMaxAABB newBounds = new MinMaxAABB() {
                Min = newMin.xyz,
                Max = newMax.xyz
            };
            return newBounds;
        }

        public static AABB Transform(this in AABB bounds, in float4x4 matrix) {
            var min = bounds.Min;
            var max = bounds.Max;

            Span<float4> points = stackalloc[] {
                new float4(min.x, min.y, min.z, 1),
                new float4(max.x, min.y, min.z, 1),
                new float4(min.x, min.y, max.z, 1),
                new float4(max.x, min.y, max.z, 1),
                new float4(min.x, max.y, min.z, 1),
                new float4(max.x, max.y, min.z, 1),
                new float4(min.x, max.y, max.z, 1),
                new float4(max.x, max.y, max.z, 1),
            };

            for (int i = 0; i < points.Length; ++i) {
                points[i] = math.mul(matrix, points[i]);
            }

            var newMin = points[0];
            var newMax = points[0];

            for (int i = 1; i < points.Length; ++i) {
                if (newMin.x > points[i].x) newMin.x = points[i].x;
                if (newMax.x < points[i].x) newMax.x = points[i].x;

                if (newMin.y > points[i].y) newMin.y = points[i].y;
                if (newMax.y < points[i].y) newMax.y = points[i].y;

                if (newMin.z > points[i].z) newMin.z = points[i].z;
                if (newMax.z < points[i].z) newMax.z = points[i].z;
            }


            MinMaxAABB newBounds = new MinMaxAABB() {
                Min = newMin.xyz,
                Max = newMax.xyz
            };
            return newBounds;
        }

        public static MinMaxAABB ToMinMaxAABB(this in Bounds bounds) {
            return new MinMaxAABB() {
                Max = bounds.max,
                Min = bounds.min
            };
        }

        public static bool IsPartOf(this in MinMaxAABB bounds, in MinMaxAABB target) {
            return bounds.Min.x >= target.Min.x &
                   bounds.Max.x <= target.Max.x &
                   bounds.Min.y >= target.Min.y &
                   bounds.Max.y <= target.Max.y &
                   bounds.Min.z >= target.Min.z &
                   bounds.Max.z <= target.Max.z;
        }

        public static float3 Center(this in MinMaxAABB bounds) {
            return (bounds.Min + bounds.Max) * 0.5F;
        }

        public static float3 Size(this in MinMaxAABB bounds) {
            return bounds.Max - bounds.Min;
        }

        public static bool Contains(this in MinMaxAABB bounds, float3 point) {
            return bounds.Min.x <= point.x && point.x <= bounds.Max.x &&
                   bounds.Min.y <= point.y && point.y <= bounds.Max.y &&
                   bounds.Min.z <= point.z && point.z <= bounds.Max.z;
        }

        public static Bounds ToBounds(this in MinMaxAABB bounds) {
            return new Bounds(bounds.Center(), bounds.Size());
        }
        
        public static bool Intersects(this in MinMaxAABB bounds, in MinMaxAABB other) {
            return bounds.Min.x <= other.Max.x &&
                   bounds.Max.x >= other.Min.x &&
                   bounds.Min.y <= other.Max.y &&
                   bounds.Max.y >= other.Min.y &&
                   bounds.Min.z <= other.Max.z &&
                   bounds.Max.z >= other.Min.z;
        }

        public static MinMaxAABB Intersection(this in MinMaxAABB bounds, in MinMaxAABB other) {
            var min = math.max(bounds.Min, other.Min);
            var max = math.min(bounds.Max, other.Max);
            return new MinMaxAABB() {
                Max = max,
                Min = min
            };
        }

        public static float Volume(this in MinMaxAABB bounds) {
            var size = bounds.Size();
            return size.x * size.y * size.z;
        }

        public static float TopDownArea(this in MinMaxAABB bounds) {
            var size = bounds.Size();
            return size.x * size.z;
        }
        
        public static void EnsureExtents(this ref AABB aabb, float3 biggerExtents) {
            if (biggerExtents.x > aabb.Extents.x) {
                aabb.Extents.x = biggerExtents.x;
            }
            if (biggerExtents.y > aabb.Extents.y) {
                aabb.Extents.y = biggerExtents.y;
            }
            if (biggerExtents.z > aabb.Extents.z) {
                aabb.Extents.z = biggerExtents.z;
            }
        }
    }
}