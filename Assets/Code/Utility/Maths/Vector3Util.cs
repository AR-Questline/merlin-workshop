using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.Utility.Maths {
    public static class Vector3Util {
        public static Vector3 Abs(this Vector3 vector3) {
            vector3.x = Mathf.Abs(vector3.x);
            vector3.y = Mathf.Abs(vector3.y);
            vector3.z = Mathf.Abs(vector3.z);
            return vector3;
        }
        
        public static float Max(this Vector3 vector3) {
            return Mathf.Max(vector3.x, Mathf.Max(vector3.y, vector3.z));
        }
        public static float Min(this Vector3 vector3) {
            return Mathf.Min(vector3.x, Mathf.Min(vector3.y, vector3.z));
        }
    
        /// <summary>
        /// Lerp, where argument t is also a vector.
        /// </summary>
        public static Vector3 Lerp(Vector3 a, Vector3 b, Vector3 t) {
            Vector3 res = Vector3.zero;
            for (int i = 0; i < 3; i++) {
                res[i] = Mathf.Lerp(a[i], b[i], t[i]);
            }
            return res;
        }
        
        public static bool EqualsApproximately(this Vector3 vector, Vector3 other, float approximation) {
            return Mathf.Abs(vector.x - other.x) <= approximation && Mathf.Abs(vector.z - other.z) <= approximation && Mathf.Abs(vector.y - other.y) <= approximation;
        }

        public static float DistanceTo(this Vector3 vector, Vector3 other) {
            return (vector - other).magnitude;
        }
        
        public static float DistanceTo2D(this Vector3 vector, Vector3 other) {
            return (vector.ToVector2() - other.ToVector2()).magnitude;
        }
        
        public static float SquaredDistanceTo(this Vector3 vector, Vector3 other) {
            return (vector - other).sqrMagnitude;
        }

        public static Vector3 UniformVector3(this float component) {
            return new(component, component, component);
        }
        
        /// <summary>
        /// Inverts a scale vector by dividing 1 by each component
        /// </summary>
        public static Vector3 Invert(this Vector3 vec) {
            return new Vector3(1 / vec.x, 1 / vec.y, 1 / vec.z);
        }
        
        // === Vector3 Swizzle
        public static Vector3 X0Z(this in Vector3 vector) {
            return new(vector.x, 0, vector.z);
        }
        
        // === Vector2 Swizzle
        public static Vector2 ToVector2(this Vector3 vector) {
            return new(vector.x, vector.z);
        }

        public static Vector2 XY(this Vector3 vector) {
            return new(vector.x, vector.y);
        }
        
        public static Vector2 XZ(this Vector3 vector) {
            return new(vector.x, vector.z);
        }

        public static float2 xz(this Vector3 vector) {
            return new(vector.x, vector.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 cmul(in Vector3 lhs, in Vector3 rhs) {
            return new Vector3(lhs.x * rhs.x, lhs.y * rhs.y, lhs.z * rhs.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 cdiv(in Vector3 lhs, in Vector3 rhs) {
            return new Vector3(lhs.x / rhs.x, lhs.y / rhs.y, lhs.z / rhs.z);
        }
        
        public static bool IsInvalid(this Vector3 vector) {
            return float.IsNaN(vector.x) | float.IsNaN(vector.y) | float.IsNaN(vector.z) | float.IsInfinity(vector.x) | float.IsInfinity(vector.y) | float.IsInfinity(vector.z);
        }
    }
}