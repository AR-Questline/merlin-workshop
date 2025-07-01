using System;
using Awaken.TG.Main.Utility.Terrain;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Terrains.Operations {
    public static class TerrainMerger {
        public static void Merge(GameObject terrain) {
            if (!TryGetChildren(terrain, out var childA, out var childB, out var childC, out var childD)) {
                return;
            }

            var dataA = childA.terrainData;
            var dataB = childB.terrainData;
            var dataC = childC.terrainData;
            var dataD = childD.terrainData;

            if (!TryGetEqual(data => data.heightmapResolution, out var childHeightmapResolution, dataA, dataB, dataC, dataD)) {
                Log.Minor?.Error($"Cannot merge terrain {terrain} because its children have different heightmap resolution.");
                return;
            }
            if (!TryGetEqual(data => data.alphamapResolution, out var childAlphamapResolution, dataA, dataB, dataC, dataD)) {
                Log.Minor?.Error($"Cannot merge terrain {terrain} because its children have different alphamap resolution.");
                return;
            }
            if (!TryGetEqual(data => data.size, out var childSize, dataA, dataB, dataC, dataD)) {
                Log.Minor?.Error($"Cannot merge terrain {terrain} because its children have different size.");
                return;
            }

            var heightmapA = dataA.GetHeights(0, 0, childHeightmapResolution, childHeightmapResolution);
            var heightmapB = dataB.GetHeights(0, 0, childHeightmapResolution, childHeightmapResolution);
            var heightmapC = dataC.GetHeights(0, 0, childHeightmapResolution, childHeightmapResolution);
            var heightmapD = dataD.GetHeights(0, 0, childHeightmapResolution, childHeightmapResolution);

            var alphamapA = dataA.GetAlphamaps(0, 0, childAlphamapResolution, childAlphamapResolution);
            var alphamapB = dataB.GetAlphamaps(0, 0, childAlphamapResolution, childAlphamapResolution);
            var alphamapC = dataC.GetAlphamaps(0, 0, childAlphamapResolution, childAlphamapResolution);
            var alphamapD = dataD.GetAlphamaps(0, 0, childAlphamapResolution, childAlphamapResolution);

            if (!TryGetEqual(alphamap => alphamap.GetLength(2), out var alphamapDepth, alphamapA, alphamapB, alphamapC, alphamapD)) {
                Log.Minor?.Error($"Cannot merge terrain {terrain} because its children have different alphamap depth.");
                return;
            }
            
            var heightmapResolution = (childHeightmapResolution - 1) * 2 + 1;
            var alphamapResolution = (childAlphamapResolution - 1) * 2 + 1;
            var child = childSize;
            child.x *= 2;
            child.z *= 2;

            var heightmap = new float[heightmapResolution, heightmapResolution];
            var alphamap = new float[alphamapResolution, alphamapResolution, alphamapDepth];
            SetMaps(0, 0, heightmapA, alphamapA);
            SetMaps(0, 1, heightmapB, alphamapB);
            SetMaps(1, 0, heightmapC, alphamapC);
            SetMaps(1, 1, heightmapD, alphamapD);

            var data = CopyMergedData(dataA);
            data.heightmapResolution = heightmapResolution;
            data.alphamapResolution = alphamapResolution;
            data.size = child;
            data.SetHeights(0, 0, heightmap);
            data.SetAlphamaps(0, 0, alphamap);

            var terrainTransform = terrain.transform;
            var terrainParent = terrainTransform.parent;
            var newTransform = Terrain.CreateTerrainGameObject(data).transform;
            newTransform.SetParent(terrainParent);
            newTransform.SetSiblingIndex(terrainTransform.GetSiblingIndex());
            newTransform.position = terrainTransform.position;
            newTransform.name = terrainTransform.name;
            newTransform.gameObject.layer = terrain.layer;

            var newTerrain = newTransform.GetComponent<Terrain>();
            TerrainUtil.CopyTerrainData(newTerrain, childA);
            
            Object.DestroyImmediate(terrain);
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(dataA));
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(dataB));
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(dataC));
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(dataD));

            EditorUtility.SetDirty(data);
            EditorUtility.SetDirty(newTransform);
            
            bool TryGetEqual<TSource, TValue>(Func<TSource, TValue> getter, out TValue value, TSource sourceA, TSource sourceB, TSource sourceC, TSource sourceD) where TValue : IEquatable<TValue> {
                value = getter(sourceA);
                return value.Equals(getter(sourceB)) && value.Equals(getter(sourceC)) && value.Equals(getter(sourceD));
            }
            
            void SetMaps(int xChunk, int yChunk, float[,] childHeightmap, float[,,] childAlphamap) {
                int xHeightmapOffset = xChunk * (childHeightmapResolution - 1);
                int yHeightmapOffset = yChunk * (childHeightmapResolution - 1);
                for (int x = 0; x < childHeightmapResolution; x++) {
                    for (int y = 0; y < childHeightmapResolution; y++) {
                        heightmap[yHeightmapOffset + x, xHeightmapOffset + y] = childHeightmap[x, y];
                    }
                }

                int xAlphamapOffset = xChunk * (childAlphamapResolution - 1);
                int yAlphamapOffset = yChunk * (childAlphamapResolution - 1);
                for (int x = 0; x < childAlphamapResolution; x++) {
                    for (int y = 0; y < childAlphamapResolution; y++) {
                        for (int z = 0; z < alphamapDepth; z++) {
                            alphamap[yAlphamapOffset + x, xAlphamapOffset + y, z] = childAlphamap[x, y, z];
                        }
                    }
                }
            }
        }
        
        static bool TryGetChildren(GameObject parent, out Terrain childA, out Terrain childB, out Terrain childC, out Terrain childD) {
            childA = null;
            childB = null;
            childC = null;
            childD = null;
            var children = parent.GetComponentsInChildren<Terrain>();
            if (children.Length != 4) {
                Log.Minor?.Error($"Cannot merge terrain {parent} because it has {children.Length} children", parent);
                return false;
            }
            foreach (var child in children) {
                var name = child.name;
                if (name.EndsWith("_a")) {
                    childA = child;
                } else if (name.EndsWith("_b")) {
                    childB = child;
                } else if (name.EndsWith("_c")) {
                    childC = child;
                } else if (name.EndsWith("_d")) {
                    childD = child;
                } else {
                    Log.Minor?.Error($"Cannot merge terrain {parent} because it has invalid child {name}");
                }
            }
            if (childA is null) {
                Log.Minor?.Error($"Cannot merge terrain {parent} because it has no _a child");
                return false;
            }
            if (childB is null) {
                Log.Minor?.Error($"Cannot merge terrain {parent} because it has no _b child");
                return false;
            }
            if (childC is null) {
                Log.Minor?.Error($"Cannot merge terrain {parent} because it has no _c child");
                return false;
            }
            if (childD is null) {
                Log.Minor?.Error($"Cannot merge terrain {parent} because it has no _d child");
                return false;
            }
            return true;
        }

        static TerrainData CopyMergedData(TerrainData data) {
            var childPath = AssetDatabase.GetAssetPath(data);
            var mergedPath = childPath[..^8] + ".asset"; // trim '_a' postfix
            AssetDatabase.CopyAsset(childPath, mergedPath);
            return AssetDatabase.LoadAssetAtPath<TerrainData>(mergedPath);
        }
    }
}