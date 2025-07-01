using Unity.Mathematics;
using UnityEngine;

namespace Awaken.Utility.Maths {
    public static class Vector2Util {
        public static Vector2 Rotate(this Vector2 v, float degrees) {
            float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
            float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);
         
            float tx = v.x;
            float ty = v.y;
            v.x = (cos * tx) - (sin * ty);
            v.y = (sin * tx) + (cos * ty);
            return v;
        }

        public static Vector2 Validate(this ref Vector2 v) {
            if (float.IsNaN(v.x)) {
                v.x = 0;
            }
            if (float.IsNaN(v.y)) {
                v.y = 0;
            }
            return v;
        }

        public static Vector2 Clamp(this in Vector2 v, float min, float max) {
            return new(Mathf.Clamp(v.x, min, max), Mathf.Clamp(v.y, min, max));
        }
        
        public static float SquaredDistanceTo(this Vector2 vector, Vector2 other) {
            return (vector - other).sqrMagnitude;
        }
        
        public static Vector2Int Uniform(this int component) {
            return new(component, component);
        }
        
        /// <summary>
        /// Bi-directional range check
        /// </summary>
        public static bool InRange(this in Vector2Int v, int value) {
            if (v.x < v.y) {
                return value >= v.x && value <= v.y;
            }
            return value >= v.y && value <= v.x;
        }

        // === Vector3 Swizzle
        public static Vector3 X0Y(this in Vector2 vector) {
            return new(vector.x, 0, vector.y);
        }

        public static Vector3 XCY(this in Vector2 vector, float c) {
            return new(vector.x, c, vector.y);
        }
        
        // === Vector2 angle conversion
        public static Vector2 AngleToHorizontal2(float degrees) {
            float angleRad = degrees * math.TORADIANS;
            return new Vector2(math.sin(angleRad), math.cos(angleRad));
        }

        public static float Horizontal2ToAngle(this Vector2 v) {
            return math.atan2(v.x, v.y) * math.TODEGREES;
        }
    }
}
