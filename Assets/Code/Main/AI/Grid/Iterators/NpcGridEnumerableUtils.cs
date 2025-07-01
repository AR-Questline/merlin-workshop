using Awaken.Utility.Collections;
using Awaken.Utility.Maths;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.AI.Grid.Iterators {
    public static class NpcGridEnumerableUtils {
        public static void SphereBounds(NpcGrid grid, float2 worldCenter, float worldRadius, out int xMin, out int xMax, out int yMin, out int yMax) {
            var chunkSize = grid.ChunkSize;
            var continuousCenter = worldCenter / chunkSize;
            var continuousRadius = worldRadius / chunkSize;
                
            var continuousMinX = continuousCenter.x - continuousRadius;
            var continuousMaxX = continuousCenter.x + continuousRadius;
            var continuousMinY = continuousCenter.y - continuousRadius;
            var continuousMaxY = continuousCenter.y + continuousRadius;

            var discreteMinX = (int)math.floor(continuousMinX);
            var discreteMaxX = (int)math.floor(continuousMaxX);
            var discreteMinY = (int)math.floor(continuousMinY);
            var discreteMaxY = (int)math.floor(continuousMaxY);
                
            xMin = math.max(discreteMinX, grid.Center.x - grid.GridHalfSize);
            xMax = math.min(discreteMaxX, grid.Center.x + grid.GridHalfSize);
            yMin = math.max(discreteMinY, grid.Center.y - grid.GridHalfSize);
            yMax = math.min(discreteMaxY, grid.Center.y + grid.GridHalfSize);
        }
        
        public static void SphereBounds(NpcGrid grid, Vector3 worldCenter, float worldRadius, out int xMin, out int xMax, out int yMin, out int yMax) {
            SphereBounds(grid, new float2(worldCenter.x, worldCenter.z), worldRadius, out xMin, out xMax, out yMin, out yMax);
        }

        public static float AxisDistanceToSq(NpcGrid grid, int chunkCoord, float worldCoord) {
            var chunkSize = grid.ChunkSize;
            var halfChunkSize = chunkSize * 0.5f;
            return AABBUtils.DistanceToSq(chunkCoord * chunkSize + halfChunkSize, halfChunkSize, worldCoord);
        }
    }
}