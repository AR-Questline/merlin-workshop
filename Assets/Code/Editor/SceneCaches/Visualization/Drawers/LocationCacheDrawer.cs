using System;
using Awaken.TG.Editor.Helpers;
using Awaken.TG.Editor.SceneCaches.Locations;
using Awaken.TG.Main.Locations.Setup;
using Awaken.Utility.UI;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using static Awaken.TG.Editor.SceneCaches.Visualization.SceneCacheDrawer;

namespace Awaken.TG.Editor.SceneCaches.Visualization.Drawers {
    [Serializable]
    public struct LocationCacheDrawer : ISceneCacheDrawer<LocationSource, LocationCacheDrawer.Metadata> {
        [SerializeField, BoxGroup("Settings")] float width;
        [SerializeField, BoxGroup("Parts")] bool showName;
        [SerializeField, BoxGroup("Parts")] bool showSpec;

        public void Init() {
            width = 200;
            showName = true;
            showSpec = true;
        }
        
        public int FilterHash() {
            return 1;
        }

        public int PartsHash() {
            return (showName ? 1 : 0) + (showSpec ? 2 : 0) + width.GetHashCode();
        }

        public bool Filter(ref Metadata metadata) {
            if (metadata.gameObject == null) {
                return false;
            }
            return true;
        }

        public void GetSize(in Metadata metadata, out float width, out float height) {
            int lines = 0;
            width = 0;
            if (showName) {
                width = math.max(width, metadata.specDisplayNameWidth);
                lines++;
            }
            if (showSpec) {
                width = math.max(width, metadata.spec.width);
                lines++;
            }
            width = math.min(width, this.width);
            height = lines * EditorGUIUtility.singleLineHeight;
        }

        public void Draw(in Metadata metadata, Rect rect) {
            var rects = new PropertyDrawerRects(rect);
            if (showName) {
                GUIDraw(rects.AllocateLine(), metadata.spec.asset.displayName);
            }
            if (showSpec) {
                GUIDraw(rects.AllocateLine(), metadata.spec);
            }
        }

        public string LOD1Name(in Metadata metadata) {
            return metadata.spec.asset.displayName;
        }

        public Vector3 GetPosition(in Metadata metadata) {
            return metadata.gameObject?.transform.position ?? Vector3.zero;
        }

        public Metadata CreateMetadata(LocationSource source) {
            var go = source.SceneGameObject;
            if (go == null) {
                return new Metadata { gameObject = null };
            }
            var spec = source.SpawnedLocationTemplate != null
                ? source.SpawnedLocationTemplate.GetComponent<LocationSpec>()
                : go.GetComponent<LocationSpec>();
            return new Metadata {
                gameObject = go,
                spec = GetAssetData(spec),
                specDisplayNameWidth = GetWidth(spec.displayName),
            };
        }
        
        public struct Metadata {
            public GameObject gameObject;
            public AssetData<LocationSpec> spec;
            public float specDisplayNameWidth;
        }
    }
}