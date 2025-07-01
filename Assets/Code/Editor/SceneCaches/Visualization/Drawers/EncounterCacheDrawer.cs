using System;
using System.Collections.Generic;
using Awaken.TG.Editor.Helpers;
using Awaken.TG.Main.General;
using Awaken.TG.Main.General.Caches;
using Awaken.TG.Main.Locations.Setup;
using Awaken.Utility.UI;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using static Awaken.TG.Editor.SceneCaches.Visualization.SceneCacheDrawer;

namespace Awaken.TG.Editor.SceneCaches.Visualization.Drawers {
    [Serializable]
    public struct EncounterCacheDrawer : ISceneCacheDrawer<EncounterData, EncounterCacheDrawer.Metadata> {
        [SerializeField, BoxGroup("Settings")] float width;
        [SerializeField, BoxGroup("Settings")] int maxDisplayCount;
        [SerializeField, BoxGroup("Filter")] FloatRange difficulty;

        public void Init() {
            width = 200;
            maxDisplayCount = 10;
            difficulty = new(0, 10000);
        }
        
        public int FilterHash() {
            return difficulty.min.GetHashCode() ^ difficulty.max.GetHashCode();
        }

        public int PartsHash() {
            return width.GetHashCode() ^ maxDisplayCount.GetHashCode();
        }

        public bool Filter(ref Metadata metadata) {
            metadata.filteredSpecs.Clear();
            if (!difficulty.Contains(metadata.difficulty)) {
                return false;
            }
            foreach (var spec in metadata.specs) {
                metadata.filteredSpecs.Add(spec);
            }
            return metadata.filteredSpecs.Count > 0;
        }

        public void GetSize(in Metadata metadata, out float width, out float height) {
            width = 0;
            var specsLines = math.min(metadata.filteredSpecs.Count, maxDisplayCount);
            for (int i = 0; i < specsLines; i++) {
                width = math.max(width, metadata.filteredSpecs[i].width);
            }
            width = math.min(width, this.width);
            
            var lines = math.min(metadata.filteredSpecs.Count, maxDisplayCount + 1);
            height = lines * EditorGUIUtility.singleLineHeight;
        }

        public void Draw(in Metadata metadata, Rect rect) {
            var rects = new PropertyDrawerRects(rect);
            
            var specsLines = math.min(metadata.filteredSpecs.Count, maxDisplayCount);
            for (int i = 0; i < specsLines; i++) {
                GUIDraw(rects.AllocateLine(), metadata.filteredSpecs[i]);
            }

            if (metadata.filteredSpecs.Count > maxDisplayCount) {
                GUIDraw(rects.AllocateLine(), $"{metadata.filteredSpecs.Count - maxDisplayCount} more");
            }
        }

        public string LOD1Name(in Metadata metadata) {
            return $"{metadata.filteredSpecs.Count} enemies";
        }

        public Vector3 GetPosition(in Metadata metadata) {
            return metadata.center;
        }

        public Metadata CreateMetadata(EncounterData source) {
            var center = Vector3.zero;
            if (source.npcs.Count > 0) {
                foreach (var npc in source.npcs) {
                    center += npc.pos;
                }
                center /= source.npcs.Count;
            }

            var specs = new AssetData<LocationSpec>[source.npcs.Count];
            for (int i = 0; i < source.npcs.Count; i++) {
                var npc = source.npcs[i].npc;
                var go = npc.SceneGameObject;
                var spec = npc.LocationTemplate != null
                    ? npc.LocationTemplate.GetComponent<LocationSpec>()
                    : go.GetComponent<LocationSpec>();
                specs[i] = GetAssetData(spec);
            }

            return new Metadata {
                specs = specs,
                center = center,
                difficulty = source.DifficultyScore,
                filteredSpecs = new(),
            };
        }
        
        public struct Metadata {
            public AssetData<LocationSpec>[] specs;
            public Vector3 center;
            public float difficulty;

            public List<AssetData<LocationSpec>> filteredSpecs;
        }
    }
}