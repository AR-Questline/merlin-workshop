using Unity.Mathematics;

namespace Awaken.Utility.Maths {
    public static class float2Util {
        // === float3 Swizzle
        public static float3 x0y(this in float2 vector) {
            return new(vector.x, 0, vector.y);
        }

        public static float3 xcy(this in float2 vector, float c) {
            return new(vector.x, c, vector.y);
        }
    }
}
