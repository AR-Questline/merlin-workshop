using System;
using Awaken.Utility.Maths;
using Awaken.Utility.Maths.Data;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.Utility {
    public static class TerrainUtil {
        public static void CopyTerrainData(UnityEngine.Terrain destination, UnityEngine.Terrain source) {
            destination.groupingID = source.groupingID;
            destination.allowAutoConnect = source.allowAutoConnect;
            destination.drawHeightmap = source.drawHeightmap;
            destination.drawInstanced = source.drawInstanced;
            destination.enableHeightmapRayTracing = source.enableHeightmapRayTracing;
            destination.heightmapPixelError = source.heightmapPixelError;
            destination.basemapDistance = source.basemapDistance;
            destination.shadowCastingMode = source.shadowCastingMode;
            destination.reflectionProbeUsage = source.reflectionProbeUsage;
            destination.materialTemplate = source.materialTemplate;
            
            destination.drawTreesAndFoliage = source.drawTreesAndFoliage;
            destination.preserveTreePrototypeLayers = source.preserveTreePrototypeLayers;
            destination.detailObjectDistance = source.detailObjectDistance;
            destination.detailObjectDensity = source.detailObjectDensity;
            destination.treeDistance = source.treeDistance;
            destination.treeBillboardDistance = source.treeBillboardDistance;
            destination.treeCrossFadeLength = source.treeCrossFadeLength;
            destination.treeMaximumFullLODCount = source.treeMaximumFullLODCount;
            
            destination.renderingLayerMask = source.renderingLayerMask;
#if UNITY_EDITOR
            destination.bakeLightProbesForTrees = source.bakeLightProbesForTrees;
            destination.deringLightProbesForTrees = source.deringLightProbesForTrees;
#endif
        }
        
        [UnityEngine.Scripting.Preserve]
        public static void FillColorsBuffer(Vector3 worldPoint, Color[] colorsBuffer, Color chosenColor, float radius,
            TerrainValues values) {
            // store world position
            Vector3 worldPos = worldPoint;
            Vector3 current = worldPos;
            // iterate fragment of ground in which the radius is contained
            for (current.z = worldPos.z - radius; current.z <= worldPos.z + radius; current.z += values.StepZ) {
                float angle = Mathf.Acos((current.z - worldPos.z) / radius);
                float xRadius = Mathf.Sin(angle) * radius;

                for (current.x = worldPos.x - xRadius; current.x <= worldPos.x + xRadius; current.x += values.StepX) {
                    WorldToTexPixels(current, out int x, out int y, values);
                    // check if given pixel is inside terrain texture
                    if (x >= 0 && y >= 0 && x < values.TextureWidth && y < values.TextureHeight) {
                        int index = y * values.TextureWidth + x;
                        colorsBuffer[index] = chosenColor;
                    }
                }
            }
        }

        public static void DoNeighbouring(float tileSize, ReadOnlySpan<Terrain> terrainsToConnect, in MinMaxAABR bounds) {
            int size = 1 + math.cmax((int2)math.ceil(bounds.Extents / tileSize));
            var grid = new Terrain[size, size];

            foreach (var terrain in terrainsToConnect) {
                var position = terrain.transform.position;
                var xz = (int2)math.round((position.xz() - bounds.min) / tileSize);
                grid[xz.x, xz.y] = terrain;
            }

            for (int x = 0; x < size; x++) {
                for (int y = 0; y < size; y++) {
                    if (!grid[x, y]) {
                        continue;
                    }
                    var left = x > 0 ? grid[x - 1, y] : null;
                    var top = y < size - 1 ? grid[x, y + 1] : null;
                    var right = x < size - 1 ? grid[x + 1, y] : null;
                    var bottom = y > 0 ? grid[x, y - 1] : null;
                    grid[x, y].SetNeighbors(left, top, right, bottom);
                }
            }
        }

        static void WorldToTexPixels(Vector3 worldPos, out int x, out int y, TerrainValues values) {
            float u = (worldPos.x - values.MinX) / values.SizeX;
            float v = (worldPos.z - values.MinZ) / values.SizeZ;
            x = (int) (u * values.TextureWidth);
            y = (int) (v * values.TextureHeight);
        }
    }

    public struct TerrainValues {
        public float MinX;
        public float MinZ;
        public float SizeX;
        public float SizeZ;
        public int TextureWidth;
        public int TextureHeight;
        public float StepX;
        public float StepZ;
    }
}
