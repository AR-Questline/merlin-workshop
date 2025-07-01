using Unity.Mathematics;

namespace Awaken.Utility.Graphics.Mipmaps {
    public static class MipmapsStreamingUtils {
        public static float CalculateMipmapFactorFactor(in CameraData cameraData, in float3 center, float radius, float scaleSq = 1f) {
            // based on  http://web.cse.ohio-state.edu/~crawfis.3/cse781/Readings/MipMapLevels-Blog.html
            float distanceSq = DistanceSq(center, radius, cameraData.cameraPosition);
            return distanceSq / (cameraData.cameraEyeToScreenDistanceSq * scaleSq);
        }

        public static float DistanceSq(in float3 center, float radius, in float3 cameraPosition) {
            return math.lengthsq(math.max(math.abs(cameraPosition - center), radius) - radius);
        }

        public static float4 CalculateMipmapFactorFactorSimd(in CameraData cameraData, in float4 simdXs, in float4 simdYs, in float4 simdZs, in float4 simdRadii) {
            // based on  http://web.cse.ohio-state.edu/~crawfis.3/cse781/Readings/MipMapLevels-Blog.html
            float4 distanceSq = DistanceSqSimd(simdXs, simdYs, simdZs, simdRadii, cameraData.cameraPosition);
            return distanceSq / cameraData.cameraEyeToScreenDistanceSq;
        }

        public static float4 DistanceSqSimd(in float4 simdXs, in float4 simdYs, in float4 simdZs, in float4 simdRadii, in float3 cameraPosition) {
            var simdCameraXs = new float4(cameraPosition.x);
            var simdCameraYs = new float4(cameraPosition.y);
            var simdCameraZs = new float4(cameraPosition.z);

            var xDiffs = math.abs(simdCameraXs - simdXs);
            var yDiffs = math.abs(simdCameraYs - simdYs);
            var zDiffs = math.abs(simdCameraZs - simdZs);

            xDiffs = math.max(xDiffs, simdRadii);
            yDiffs = math.max(yDiffs, simdRadii);
            zDiffs = math.max(zDiffs, simdRadii);

            xDiffs -= simdRadii;
            yDiffs -= simdRadii;
            zDiffs -= simdRadii;

            return xDiffs * xDiffs + yDiffs * yDiffs + zDiffs * zDiffs;
        }
    }
}
