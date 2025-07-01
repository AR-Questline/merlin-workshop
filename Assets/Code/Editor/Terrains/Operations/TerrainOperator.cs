using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths;
using Awaken.Utility.Maths.Data;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using Object = System.Object;

namespace Awaken.TG.Editor.Terrains.Operations {
    public class TerrainOperator : OdinEditorWindow {
        [SerializeField] ProjectCleanup cleanup;
        [SerializeField] Splitter splitter;
        [SerializeField] Merger merger;
        [SerializeField] Neighbourer neighbourer;
        [SerializeField] SplatmapDeleter splatmapDeleter;

        [MenuItem("TG/Scene Tools/Terrain Operator")]
        static void Open() => GetWindow<TerrainOperator>().Show();

        [Serializable]
        class ProjectCleanup {
            [SerializeField, FolderPath] string path;
            [SerializeField] GameObject root;

            [Button]
            void Cleanup() {
                foreach(var terrain in root.GetComponentsInChildren<Terrain>(true)) {
                    var data = terrain.terrainData;
                    var originPath = AssetDatabase.GetAssetPath(data);
                    AssetDatabase.MoveAsset(originPath, $"{path}\\{terrain.name}.asset");
                }
            }
        }
        
        [Serializable]
        class Splitter {
            [SerializeField] List<Terrain> terrains;

            [Button]
            void Split() {
                foreach (var terrain in terrains) {
                    TerrainSplitter.Split(terrain);
                }
                terrains.Clear();
            }
        }
        
        [Serializable]
        class Merger {
            [SerializeField] List<GameObject> terrains;

            [Button]
            void Merge() {
                foreach (var terrain in terrains) {
                    TerrainMerger.Merge(terrain);
                }
                terrains.Clear();
            }
        }

        [Serializable]
        class Neighbourer {
            [SerializeField] GameObject root;
            [SerializeField] Terrain source;

            [Button]
            void FixNeighboursGlobally() {
                var terrains = root.GetComponentsInChildren<Terrain>(true);

                MinMaxAABR bounds = MinMaxAABR.Empty;
                var compatibleTerrains = new UnsafePinnableList<Terrain>(terrains.Length);

                GetSelector(source, out var sourceSize, out var sourceResolution);
                foreach (Terrain terrain in terrains) {
                    GetSelector(terrain, out var terrainSize, out var terrainResolution);
                    if (terrainSize == sourceSize && terrainResolution == sourceResolution) {
                        var position = terrain.transform.position;
                        bounds.Encapsulate(position.xz());
                        terrain.groupingID = 0;
                        terrain.allowAutoConnect = true;
                        compatibleTerrains.Add(terrain);
                    } else {
                        terrain.groupingID = 1;
                        terrain.allowAutoConnect = false;
                        terrain.SetNeighbors(null, null, null, null);
                    }
                }

                TerrainUtil.DoNeighbouring(sourceSize, compatibleTerrains.AsSpan(), bounds);
            }

            static void GetSelector(Terrain terrain, out float size, out int resolution) {
                var data = terrain.terrainData;
                size = data.size.x;
                resolution = data.heightmapResolution;
            }
        }

        [Serializable]
        class SplatmapDeleter {
            [SerializeField] TerrainData data;
            [SerializeField] Texture2D alphamap;

            [ShowInInspector] TerrainLayer[] Layers => data?.terrainLayers;
            
            [Button]
            void Fix() {
                DestroyImmediate(alphamap, true);
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssetIfDirty(data);
            }

            bool IsEmpty(float[,,] alphamaps, int width, int height, int layer) {
                for (int x = 0; x < width; x++) {
                    for (int y = 0; y < height; y++) {
                        if (alphamaps[x, y, layer] > 0) {
                            return false;
                        }
                    }
                }
                return true;
            }
        }
    }
}