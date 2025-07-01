using Awaken.TG.Main.Utility.Terrain;
using Awaken.Utility;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Terrains.Operations {
    public static class TerrainSplitter {
        public static void Split(Terrain terrain) {
            var cache = new TerrainCache(terrain);
            var sizes = new ChunkSizes(cache.data, 2);
            
            CreateSplitted(cache, sizes, "a", 0, 0);
            CreateSplitted(cache, sizes, "b", 0, 1);
            CreateSplitted(cache, sizes, "c", 1, 0);
            CreateSplitted(cache, sizes, "d", 1, 1);
            
            Object.DestroyImmediate(terrain.GetComponent<TerrainCollider>());
            Object.DestroyImmediate(terrain);
            AssetDatabase.DeleteAsset(cache.dataPath);
        }
         
        static Terrain CreateSplitted(in TerrainCache original, in ChunkSizes sizes, string name, int xChunk, int yChunk) {
            var heightmap = new float[sizes.heightmapResolution, sizes.heightmapResolution];
            var heightmapXStart = xChunk * (sizes.heightmapResolution - 1);
            var heightmapYStart = yChunk * (sizes.heightmapResolution - 1);
            for (int x = 0; x < sizes.heightmapResolution; x++) {
                for (int y = 0; y < sizes.heightmapResolution; y++) {
                    heightmap[x , y] = original.heightmap[heightmapYStart + x, heightmapXStart + y];
                }
            }

            var alphamapDepth = original.alphamap.GetLength(2);
            var alphamap = new float[sizes.alphamapResolution, sizes.alphamapResolution, alphamapDepth];
            var alphamapXStart = xChunk * (sizes.alphamapResolution - 1);
            var alphamapYStart = yChunk * (sizes.alphamapResolution - 1);
            for (int x = 0; x < sizes.alphamapResolution; x++) {
                for (int y = 0; y < sizes.alphamapResolution; y++) {
                    for (int z = 0; z < alphamapDepth; z++) {
                        alphamap[x , y, z] = original.alphamap[alphamapYStart + x, alphamapXStart + y, z];
                    }
                }
            }

            var data = CopySplitData(original, name);
            data.heightmapResolution = sizes.heightmapResolution;
            data.alphamapResolution = sizes.alphamapResolution;
            data.baseMapResolution = sizes.baseMapResolution;
            data.size = sizes.size;
            data.SetHeights(0, 0, heightmap);
            data.SetAlphamaps(0, 0, alphamap);
            
            var transform = Terrain.CreateTerrainGameObject(data).transform;
            transform.SetParent(original.transform);
            transform.position = original.transform.position + new Vector3(xChunk * sizes.size.x, 0, yChunk * sizes.size.z);
            transform.name = data.name;

            var terrain = transform.GetComponent<Terrain>();
            TerrainUtil.CopyTerrainData(terrain, original.terrain);

            EditorUtility.SetDirty(data);
            EditorUtility.SetDirty(transform);

            return terrain;
        }

        static TerrainData CopySplitData(in TerrainCache cache, string postfix) {
            var path = $"{cache.dataPath[..^6]}_{postfix}.asset";
            AssetDatabase.CopyAsset(cache.dataPath, path);
            return AssetDatabase.LoadAssetAtPath<TerrainData>(path);
        }

        readonly struct TerrainCache {
            public readonly Terrain terrain;
            public readonly Transform transform;
            public readonly TerrainData data;
            public readonly string dataPath;
            public readonly float[,] heightmap;
            public readonly float[,,] alphamap;

            public TerrainCache(Terrain terrain) {
                this.terrain = terrain;
                transform = terrain.transform;
                data = terrain.terrainData;
                dataPath = AssetDatabase.GetAssetPath(data);
                var heightmapResolution = data.heightmapResolution;
                heightmap = data.GetHeights(0, 0, heightmapResolution, heightmapResolution);
                var alphamapResolution = data.alphamapResolution;
                alphamap = data.GetAlphamaps(0, 0, alphamapResolution, alphamapResolution);
            }
        }
        readonly struct ChunkSizes {
            public readonly int heightmapResolution;
            public readonly int alphamapResolution;
            public readonly int baseMapResolution;
            public readonly Vector3 size;
            
            public ChunkSizes(TerrainData data, int split) {
                heightmapResolution = (data.heightmapResolution - 1) / split + 1;
                alphamapResolution = (data.alphamapResolution - 1) / split + 1;
                baseMapResolution = data.baseMapResolution / split;
                size = data.size;
                size.x /= split;
                size.z /= split;
            }
        }
    }
}