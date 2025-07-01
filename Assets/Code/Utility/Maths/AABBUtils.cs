using Unity.Burst;
using Unity.Mathematics;

namespace Awaken.Utility.Maths {
    [BurstCompile]
    public static class AABBUtils {
        [BurstCompile]
        public static float DistanceToSq(float center, float extend, float point) {
            var distance = math.abs(center - point);
            return math.lengthsq(math.max(distance - extend, 0));
        }

        [BurstCompile]
        public static float DistanceToSq(in float2 center, in float2 extend, in float2 point) {
            var distance = math.abs(center - point);
            return math.lengthsq(math.max(distance - extend, 0));
        }

        [BurstCompile]
        public static float DistanceToSq(in float3 center, in float3 extend, in float3 point) {
            var distance = math.abs(center - point);
            return math.lengthsq(math.max(distance - extend, 0));
        }

        [BurstCompile]
        public static float DistanceToSq(in float4 center, in float4 extend, in float4 point) {
            var distance = math.abs(center - point);
            return math.lengthsq(math.max(distance - extend, 0));
        }
        
        [BurstCompile]
        public static class Cone {
            [BurstCompile]
            public static void XZAABB(in float3 center, in float3 forward, float radius, float coneCos, float coneSin, out float2 min, out float2 max) {
                var directionXs = new float4(-1, 1, 0, 0);
                var directionYs = new float4(0, 0, 0, 0);
                var directionZs = new float4(0, 0, -1, 1);
                
                var directionCos = forward.x * directionXs + forward.y * directionYs + forward.z * directionZs;
                
                ClosestDirectionsOnCone(forward.x, forward.y, forward.z, directionXs, directionYs, directionZs, coneSin, coneCos, directionCos, out var onConeXs, out var onConeYs, out var onConeZs);

                var inCones = directionCos > coneCos;

                var resultXs = math.select(onConeXs, directionXs, inCones);
                var resultZs = math.select(onConeZs, directionZs, inCones);
                
                min = center.xz + math.min(0, new float2(resultXs.x, resultZs.z) * radius);
                max = center.xz + math.max(0, new float2(resultXs.y, resultZs.w) * radius);
            }

            [BurstCompile]
            static void ClosestDirectionsOnCone(float forwardX, float forwardY, float forwardZ, in float4 directionXs, in float4 directionYs, in float4 directionZs, float coneSin, float coneCos, in float4 directionCos, out float4 onConeXs, out float4 onConeYs, out float4 onConeZs) {
                var orthogonalTangentXs = directionXs - forwardX * directionCos;
                var orthogonalTangentYs = directionYs - forwardY * directionCos;
                var orthogonalTangentZs = directionZs - forwardZ * directionCos;
                
                var orthogonalTangentLengthSqs = orthogonalTangentXs * orthogonalTangentXs + orthogonalTangentYs * orthogonalTangentYs + orthogonalTangentZs * orthogonalTangentZs;
                var orthogonalTangentInverseLengths = math.rsqrt(orthogonalTangentLengthSqs);

                var orthogonalTangentNormalXs = orthogonalTangentXs * orthogonalTangentInverseLengths;
                var orthogonalTangentNormalYs = orthogonalTangentYs * orthogonalTangentInverseLengths;
                var orthogonalTangentNormalZs = orthogonalTangentZs * orthogonalTangentInverseLengths;

                // in case forward i directions are perfectly aligned we may choose a random vector on cone
                var isAligned = orthogonalTangentLengthSqs == 0;
                var orthogonalRandomXs = new float4(-forwardZ);
                var orthogonalRandomYs = new float4(0);
                var orthogonalRandomZs = new float4(forwardX);
                
                var orthogonalXs = math.select(orthogonalTangentNormalXs, orthogonalRandomXs, isAligned);
                var orthogonalYs = math.select(orthogonalTangentNormalYs, orthogonalRandomYs, isAligned);
                var orthogonalZs = math.select(orthogonalTangentNormalZs, orthogonalRandomZs, isAligned);
                
                onConeXs = orthogonalXs * coneSin + forwardX * coneCos;
                onConeYs = orthogonalYs * coneSin + forwardY * coneCos;
                onConeZs = orthogonalZs * coneSin + forwardZ * coneCos;
            }
        }

        [BurstCompile] [UnityEngine.Scripting.Preserve]
        static class Sphere {
            public static void XZAABB(in float3 center, float radius, out float2 min, out float2 max) {
                min.x = center.x - radius;
                min.y = center.z - radius;
                max.x = center.x + radius;
                max.y = center.z + radius;
            }
        }
    }
}