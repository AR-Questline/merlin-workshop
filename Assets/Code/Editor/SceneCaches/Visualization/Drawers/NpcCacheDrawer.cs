using System;
using Awaken.TG.Editor.Helpers;
using Awaken.TG.Main.Fights.NPCs;
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
    public struct NpcCacheDrawer : ISceneCacheDrawer<NpcSource, NpcCacheDrawer.Metadata> {
        [SerializeField, BoxGroup("Settings")] float width;
        [SerializeField, BoxGroup("Parts")] bool showName;
        [SerializeField, BoxGroup("Parts")] bool showTemplate;
        [SerializeField, BoxGroup("Parts")] bool showSpec;
        [SerializeField, BoxGroup("Parts")] bool showActor;

        public void Init() {
            width = 200;
            showName = true;
            showSpec = true;
        }
        
        public int FilterHash() {
            return 1;
        }

        public int PartsHash() {
            return (showName ? 1 : 0) + (showTemplate ? 2 : 0) + (showSpec ? 4 : 0) + (showActor ? 8 : 0) + width.GetHashCode();
        }

        public bool Filter(ref Metadata metadata) {
            if (metadata.gameObject == null) {
                return false;
            }
            return true;
        }

        public void GetSize(in Metadata metadata, out float width, out float height) {
            width = 0;
            int lines = 0;
            
            if (showName) {
                lines++;
                width = math.max(width, metadata.specDisplayNameWidth);
            }
            if (showTemplate) {
                lines++;
                width = math.max(width, metadata.template.width);
            }
            if (showSpec) {
                lines++;
                width = math.max(width, metadata.spec.width);
            }
            if (showActor) {
                lines++;
                width = math.max(width, metadata.actorLabelWidth);
            }
            
            width = math.min(width, this.width);
            height = lines * EditorGUIUtility.singleLineHeight;
        }

        public void Draw(in Metadata metadata, Rect rect) {
            var rects = new PropertyDrawerRects(rect);
            if (showName) {
                GUIDraw(rects.AllocateLine(), metadata.spec.asset.displayName);
            }
            if (showTemplate) {
                GUIDraw(rects.AllocateLine(), metadata.template);
            }
            if (showSpec) {
                GUIDraw(rects.AllocateLine(), metadata.spec);
            }
            if (showActor) {
                GUIDraw(rects.AllocateLine(), metadata.actorLabel);
            }
        }

        public string LOD1Name(in Metadata metadata) {
            return metadata.spec.asset.displayName;
        }

        public Vector3 GetPosition(in Metadata metadata) {
            return metadata.gameObject?.transform.position ?? Vector3.zero;
        }

        public Metadata CreateMetadata(NpcSource source) {
            var go = source.SceneGameObject;
            if (go == null) {
                return new Metadata { gameObject = null };
            }
            var spec = source.LocationTemplate != null
                ? source.LocationTemplate.GetComponent<LocationSpec>()
                : go.GetComponent<LocationSpec>();
            var npcTemplate = source.NpcTemplate;
            var npcAttachment = spec.GetComponent<NpcAttachment>();
            GetActorLabel("Actor", npcAttachment.Editor_GetActorForCache(), out var actorLabel, out var actorLabelWidth);
            return new Metadata {
                gameObject = go,
                spec = GetAssetData(spec),
                template = GetAssetData(npcTemplate),
                actorLabel = actorLabel,
                specDisplayNameWidth = GetWidth(spec.displayName),
                actorLabelWidth = actorLabelWidth,
            };
        }
        
        public struct Metadata {
            public GameObject gameObject;
            public AssetData<LocationSpec> spec;
            public AssetData<NpcTemplate> template;
            public string actorLabel;
            public float specDisplayNameWidth;
            public float actorLabelWidth;
        }
    }
}