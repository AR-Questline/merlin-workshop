using System.Runtime.CompilerServices;
using Awaken.Utility.Debugging;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.Utility.Maths {
    public static class mathExt {
        public const float DegreeToRadian = 0.0174532925f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Get(this float4x2 vector, int index) {
            var first = (index & 0b100) >> 2;
            var second = index & 0b011;
            return vector[first][second];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Set(this ref float4x2 vector, int index, float value) {
            var first = (index & 0b100) >> 2;
            var second = index & 0b011;
            ref var column = ref vector[first];
            column[second] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Scale(in this float4x4 m) =>
            new float3(math.length(m.c0.xyz), math.length(m.c1.xyz), math.length(m.c2.xyz));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Translation(in this float4x4 m) => new float3(m.c3.x, m.c3.y, m.c3.z);

        public static quaternion Rotation(in this float4x4 m) => new quaternion(math.orthonormalize(new float3x3(m)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion FromToRotation(float3 fromDirection, float3 toDirection) {
            float3 cross = math.cross(fromDirection, toDirection);
            float dot = math.dot(fromDirection, toDirection);

            var q = new quaternion(
                cross.x,
                cross.y,
                cross.z,
                dot + math.sqrt(math.length(fromDirection) * math.length(toDirection))
            );

            return math.normalize(q);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ProjectOnPlane(float3 vector, float3 planeNormal) {
            float num1 = math.dot(planeNormal, planeNormal);
            if (num1 < math.EPSILON)
                return vector;

            float num2 = math.dot(vector, planeNormal);
            return new float3(vector.x - planeNormal.x * num2 / num1,
                vector.y - planeNormal.y * num2 / num1,
                vector.z - planeNormal.z * num2 / num1);
        }

        /// <param name="vector">The location of the vector above the plane.</param>
        /// <param name="planeNormal">The direction from the vector towards the plane. MUST BE NORMALIZED.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ProjectOnPlaneUnsafe(float3 vector, float3 planeNormal) {
            float num2 = math.dot(vector, planeNormal);
            return new float3(vector.x - planeNormal.x * num2, vector.y - planeNormal.y * num2, vector.z - planeNormal.z * num2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float MoveTowards(float current, float target, float maxDistanceDelta) {
            float dst = math.distance(target, current);
            if (dst == 0 || dst <= maxDistanceDelta) {
                return target;
            }

            return math.lerp(current, target, maxDistanceDelta / dst);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 MoveTowards(float3 current, float3 target, float maxDistanceDelta) {
            float dst = math.distance(target, current);
            if (dst == 0 || dst <= maxDistanceDelta) {
                return target;
            }

            return math.lerp(current, target, maxDistanceDelta / dst);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion RotateTowardsDegrees(quaternion from, quaternion to, float maxDegreesDelta) {
            float num = math.degrees(math.angle(from, to));
            return num == 0.0f ? to : math.slerp(from, to, math.min(1f, maxDegreesDelta / num));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion RotateTowardsRadians(quaternion from, quaternion to, float maxRadiansDelta) {
            float num = math.angle(from, to);
            return num == 0.0f ? to : math.slerp(from, to, math.min(DegreeToRadian, maxRadiansDelta / num));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 RotatePointAroundPivot(float3 point, float3 pivot, quaternion rotation) {
            return math.mul(rotation, (point - pivot)) + pivot;
        }

        public static float3x4 mul(in float3x4 left, in float3x4 right) {
            var x = default(float4x4);
            x.c0 = new float4(left.c0, 0);
            x.c1 = new float4(left.c1, 0);
            x.c2 = new float4(left.c2, 0);
            x.c3 = new float4(left.c3, 1);

            var y = default(float4x4);
            y.c0 = new float4(right.c0, 0);
            y.c1 = new float4(right.c1, 0);
            y.c2 = new float4(right.c2, 0);
            y.c3 = new float4(right.c3, 1);

            var r = math.mul(x, y);
            return new float3x4(r.c0.xyz, r.c1.xyz, r.c2.xyz, r.c3.xyz);
        }

        public static Optional<float3> IntersectRayTriangle(float3 rayOrigin, float3 rayDirection, float3 v0, float3 v1, float3 v2) {
            const float Epsilon = 0.000001f;

            // edges from v1 & v2 to v0.
            var e1 = v1 - v0;
            var e2 = v2 - v0;

            var h = math.cross(rayDirection, e2);
            var a = math.dot(e1, h);
            if ((a > -Epsilon) & (a < Epsilon)) {
                return Optional<float3>.None;
            }

            var f = 1.0f / a;

            var s = rayOrigin - v0;
            float u = f * math.dot(s, h);
            if ((u < 0.0f) | (u > 1.0f)) {
                return Optional<float3>.None;
            }

            var q = math.cross(s, e1);
            var v = f * math.dot(rayDirection, q);
            if ((v < 0.0f) | (u + v > 1.0f)) {
                return Optional<float3>.None;
            }

            var t = f * math.dot(e2, q);
            if (t > Epsilon) {
                return rayOrigin + rayDirection * t;
            }
            return Optional<float3>.None;
        }

        public static float3x4 orthonormal(in this float4x4 matrix) {
            return new float3x4(
                matrix.c0.xyz,
                matrix.c1.xyz,
                matrix.c2.xyz,
                matrix.c3.xyz
            );
        }

        public static float4x4 expandOrhonormal(in this float3x4 matrix) {
            return new float4x4(
                new float4(matrix.c0, 0),
                new float4(matrix.c1, 0),
                new float4(matrix.c2, 0),
                new float4(matrix.c3, 1)
            );
        }

        public static float3x4 Orthonormal(this Matrix4x4 matrix) {
            return new float3x4(
                new float3(matrix.m00, matrix.m10, matrix.m20),
                new float3(matrix.m01, matrix.m11, matrix.m21),
                new float3(matrix.m02, matrix.m12, matrix.m22),
                new float3(matrix.m03, matrix.m13, matrix.m23)
            );
        }

        public static float3 MirrorVector(float3 vector, float3 mirrorPlaneNormal) {
            return vector - 2 * math.dot(vector, mirrorPlaneNormal) * mirrorPlaneNormal;
        }

        public static float3 GetExactUpDirection(float3 forward, float3 approximateUp) {
            return math.normalize(math.cross(math.cross(forward, approximateUp), forward));
        }

        public static float GetDistanceSqToOutsideSphereSurface(float3 sphereCenter, float sphereRadius, float3 point) {
            return math.square(math.max(math.distance(point, sphereCenter), sphereRadius) - sphereRadius);
        }

        public static bool Approximately(float a, float b) {
            return math.abs(b - a) < math.EPSILON * 8f;
        }
        
        public static bool Contains(this int4 vector, int value) {
            return IndexOf(value, vector) != -1;
        }
        
        public static int IndexOf(int value, int4 vector) {
            return math.select(math.select(math.select(math.select(-1, 3, vector.w == value), 2, vector.z == value), 1, vector.y == value), 0, vector.x == value);
        }
        
        public static int IndexOf(int value, int3 vector) {
            return math.select(math.select(math.select(-1, 2, vector.z == value), 1, vector.y == value), 0, vector.x == value);
        }
        
        public static int IndexOf(float value, float4 vector) {
            return math.select(math.select(math.select(math.select(-1, 3, vector.w == value), 2, vector.z == value), 1, vector.y == value), 0, vector.x == value);
        }

        public static float FindLerpEndValue(float lerpStart, float t, float lerpResult) {
            Asserts.IsTrue(t != 0);
            return (lerpResult - (lerpStart * (1 - t))) / t;
        }
    }
}