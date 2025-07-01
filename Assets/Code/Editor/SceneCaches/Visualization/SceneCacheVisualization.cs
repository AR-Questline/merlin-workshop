using System;
using System.Collections.Generic;
using Awaken.TG.Editor.SceneCaches.Items;
using Awaken.TG.Editor.SceneCaches.Locations;
using Awaken.TG.Editor.SceneCaches.Visualization.Drawers;
using Awaken.TG.Main.General.Caches;
using Awaken.Utility.Editor;
using Awaken.Utility.Extensions;
using Awaken.Utility.UI;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Editor.SceneCaches.Visualization {
    public class SceneCacheVisualization : OdinEditorWindow {
        [SerializeField, InlineProperty, HideLabel, FoldoutGroup("Settings")] Settings settings;
        
        [SerializeField] DrawerData<NpcCache, SceneNpcSources, NpcSource, NpcCacheDrawer.Metadata, NpcCacheDrawer> npc;
        [SerializeField] DrawerData<LootCache, SceneItemSources, ItemSource, LootCacheDrawer.Metadata, LootCacheDrawer> loot;
        [SerializeField] DrawerData<EncountersCache, SceneEncountersSources, EncounterData, EncounterCacheDrawer.Metadata, EncounterCacheDrawer> encounters;
        [SerializeField] DrawerData<LocationCache, SceneLocationSources, LocationSource, LocationCacheDrawer.Metadata, LocationCacheDrawer> locations;
        
        
        [MenuItem("TG/Design/Scene Cache Visualization")]
        public static void Open() {
            GetWindow<SceneCacheVisualization>().Show();
        }
        
        protected override void OnEnable() {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneClosed += OnSceneClosed;
            SceneView.duringSceneGui += OnFirstSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        protected override void OnDisable() {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneClosed -= OnSceneClosed;
            SceneView.duringSceneGui -= OnFirstSceneGUI;
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        void OnSceneOpened(Scene scene, OpenSceneMode mode) {
            RefreshMetadata();
        }

        void OnSceneClosed(Scene scene) {
            RefreshMetadata();
        }
        
        void RefreshMetadata() {
            npc.RefreshMetadata();
            loot.RefreshMetadata();
            encounters.RefreshMetadata();
            locations.RefreshMetadata();
        }

        void OnFirstSceneGUI(SceneView _) {
            npc.Setup(NpcCache.Get);
            loot.Setup(LootCache.Get);
            encounters.Setup(EncountersCache.Get);
            locations.Setup(LocationCache.Get);
            
            RefreshMetadata();
            
            SceneView.duringSceneGui -= OnFirstSceneGUI;
        }
        
        void OnSceneGUI(SceneView sceneView) {
            Handles.BeginGUI();
            var previousMatrix = GUI.matrix;
            var viewContext = new DrawContext(sceneView, settings);

            npc.Draw(viewContext);
            loot.Draw(viewContext);
            encounters.Draw(viewContext);
            locations.Draw(viewContext);
            
            GUI.matrix = previousMatrix;
            Handles.EndGUI();
        }
        
        readonly struct DrawContext {
            public readonly SceneView sceneView;
            public readonly int screenWidth;
            public readonly int screenHeight;
            public readonly Vector2 mousePosition;
            public readonly Settings settings;

            public DrawContext(SceneView sceneView, Settings settings) {
                this.sceneView = sceneView;
                screenWidth = Screen.width;
                screenHeight = Screen.height;
                mousePosition = Event.current.mousePosition;
                this.settings = settings;
            }
        }

        [Serializable]
        class Settings {
            public const float DefaultBaseDistance = 6;
            public bool scaleWithDistance = true;
            public float scale = 1f;
            public Vector3 worldOffset;
            public Vector2 screenOffset;
            public Vector2 devOffset = new(0, -50);
            public Vector2 padding = new(6, 6);
            public float outline = 1;
        }
        
        [Serializable, Toggle("enabled")]
        struct DrawerData<TCache, TData, TSource, TDrawerMetadata, TDrawer> 
            where TCache : ISceneCache<TData, TSource> 
            where TData : ISceneCacheData<TSource> 
            where TSource : ISceneCacheSource, IEquatable<TSource> 
            where TDrawerMetadata : struct
            where TDrawer : struct, ISceneCacheDrawer<TSource, TDrawerMetadata>
        {
            static readonly List<InstanceToDraw> ReusableInstances = new();

            [SerializeField] bool enabled;
            [SerializeField, InlineProperty, HideLabel] TDrawer drawer;
            
            TCache _cache;
            Metadata[][] _metadatas;
            int _previousFilterHash;
            int _previousPartsHash;
            
            TSource _hoveredSource;

            public void Setup(TCache cache) {
                drawer = new();
                drawer.Init();
                _cache = cache;
                _metadatas = new Metadata[_cache.Data.Count][];
            }
            
            public void RefreshMetadata() {
                for (int i = 0; i < _cache.Data.Count; i++) {
                    var data = _cache.Data[i];
                    if (data.SceneRef.LoadedScene.isLoaded) {
                        var sources = data.Sources;
                        _metadatas[i] = new Metadata[data.Sources.Count];
                        for (int j = 0; j < data.Sources.Count; j++) {
                            var drawerMetadata = drawer.CreateMetadata(sources[j]);
                            _metadatas[i][j].drawer = drawerMetadata;
                            _metadatas[i][j].system.position = drawer.GetPosition(drawerMetadata);
                        }
                    } else {
                        _metadatas[i] = null;
                    }
                }
                _previousFilterHash = 0;
                _previousPartsHash = 0;
            }

            public void Draw(in DrawContext drawContext) {
                if (!enabled) {
                    return;
                }

                var instances = ReusableInstances;
                instances.Clear();
                var filterHash = drawer.FilterHash();
                var partsHash = drawer.PartsHash();
                var newFilter = filterHash != _previousFilterHash;
                var newParts = partsHash != _previousPartsHash;
                var newContent = newFilter | newParts;
                for (int i = 0; i < _cache.Data.Count; i++) {
                    var data = _cache.Data[i];
                    if (data.SceneRef.LoadedScene.isLoaded) {
                        for (int j = 0; j < data.Sources.Count; j++) {
                            var source = data.Sources[j];
                            ref var metadata = ref _metadatas[i][j];
                            
                            if (newFilter) {
                                metadata.system.filter = drawer.Filter(ref metadata.drawer);
                            }
                            if (!metadata.system.filter) {
                                continue;
                            }
                            if (newContent) {
                                drawer.GetSize(metadata.drawer, out metadata.system.lod0Width, out metadata.system.lod0Height);
                                metadata.system.lod1Label = drawer.LOD1Name(metadata.drawer);
                                metadata.system.lod1Width = math.min(GUIUtils.LabelWidth(SceneCacheDrawer.BigNameStyle, metadata.system.lod1Label), 300);
                            }
                            if (!Cull(drawContext, source, metadata, out var instance)) {
                                continue;
                            }
                            instances.Add(instance);
                        }
                    }
                }
                _previousFilterHash = filterHash;
                _previousPartsHash = partsHash;

                if (Event.current.type is EventType.Repaint) {
                    // we draw all instances from bottom to top
                    instances.Sort((lhs, rhs) => -lhs.screenPoint.z.CompareTo(rhs.screenPoint.z));
                
                    bool hoveredLock = false;
                    DrawPass hoveredPass = default;
                    foreach (var instance in instances) {
                        var pass = CreatePass(drawContext, drawer, instance, out var hovered);
                        if (!hoveredLock & hovered) {
                            if (hoveredPass.source != null) {
                                Draw(drawContext, drawer, hoveredPass);
                            }
                            hoveredPass = pass;
                            hoveredLock = instance.source.Equals(_hoveredSource);
                        } else {
                            Draw(drawContext, drawer, pass);
                        }
                    }
                    if (hoveredPass.source != null) {
                        Draw(drawContext, drawer, hoveredPass);
                    }
                    _hoveredSource = hoveredPass.source;
                } else {
                    // we handle other events only from the top hovered one
                    DrawPass topPass = default;
                    float topDepth = float.MaxValue;
                    foreach (var instance in instances) {
                        var previouslyHovered = instance.source.Equals(_hoveredSource);
                        if (!previouslyHovered & instance.screenPoint.z > topDepth) {
                            continue;
                        }
                        var pass = CreatePass(drawContext, drawer, instance, out var hovered);
                        if (hovered) {
                            topDepth = instance.screenPoint.z;
                            topPass = pass;
                            if (previouslyHovered) {
                                break;
                            }
                        }
                    }
                    if (topPass.source != null) {
                        Draw(drawContext, drawer, topPass);
                    }
                    _hoveredSource = topPass.source;
                }
                instances.Clear();
            }
            
            bool Cull(in DrawContext drawContext, in TSource source, in Metadata metadata, out InstanceToDraw instance) {
                var worldPosition = metadata.system.position + drawContext.settings.worldOffset;
                instance.source = source;
                instance.metadata = metadata;
                instance.screenPoint = drawContext.sceneView.camera.WorldToScreenPoint(worldPosition);
                instance.scale = 1f;
                if (instance.screenPoint.z <= 0) {
                    return false;
                }
                if (instance.screenPoint.x < 0 | instance.screenPoint.x > drawContext.screenWidth | instance.screenPoint.y < 0 | instance.screenPoint.y > drawContext.screenHeight) {
                    return false;
                }
                instance.scale = drawContext.settings.scaleWithDistance ? (Settings.DefaultBaseDistance * drawContext.settings.scale) / instance.screenPoint.z : drawContext.settings.scale;
                if (instance.scale < SceneCacheDrawer.CullScale) {
                    return false;
                }
                return true;
            }
            
            DrawPass CreatePass(in DrawContext drawContext, TDrawer drawer, in InstanceToDraw instance, out bool hovered) {
                GetSize(drawContext, instance.metadata, instance.scale, out var width, out var height);
                var offset = drawContext.settings.devOffset + (drawContext.settings.screenOffset - new Vector2(width, height) * 0.5f) * instance.scale;
                var translation = new Vector3(instance.screenPoint.x + offset.x, drawContext.screenHeight - instance.screenPoint.y + offset.y, 0);
                var screenRect = new Rect(translation.x, translation.y, width * instance.scale, height * instance.scale);
                hovered = screenRect.Contains(drawContext.mousePosition);
                return new DrawPass(translation, width, height, instance.scale, instance.source, instance.metadata);
            }
            
            void GetSize(in DrawContext drawContext, in Metadata metadata, float scale, out float width, out float height) {
                if (scale < SceneCacheDrawer.LOD2Scale) {
                    width = SceneCacheDrawer.LOD2Width;
                    height = SceneCacheDrawer.LOD2Height;
                } else if (scale < SceneCacheDrawer.LOD1Scale) {
                    width = metadata.system.lod1Width;
                    height = SceneCacheDrawer.BigNameHeight;
                } else {
                    width = metadata.system.lod0Width;
                    height = metadata.system.lod0Height;
                }
                width += drawContext.settings.padding.x * 2 + drawContext.settings.outline / scale * 2;
                height += drawContext.settings.padding.y * 2 + drawContext.settings.outline / scale * 2;
            }
            
            void Draw(in DrawContext drawContext, TDrawer drawer, in DrawPass pass) {
                GUI.matrix = Matrix4x4.TRS(pass.translation, Quaternion.identity, Vector3.one * pass.scale);
                var rect = new Rect(0, 0, pass.width, pass.height);
            
                EditorGUI.DrawRect(rect, Color.white);
                rect = rect.Inflated(-drawContext.settings.outline / pass.scale, -drawContext.settings.outline / pass.scale);
                EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 1.0f));
                rect = rect.Inflated(-drawContext.settings.padding.x, -drawContext.settings.padding.y);
            
                if (pass.scale < SceneCacheDrawer.LOD2Scale) {
                    // draw just border
                } else if (pass.scale < SceneCacheDrawer.LOD1Scale) {
                    EditorGUI.LabelField(rect, pass.metadata.system.lod1Label, SceneCacheDrawer.BigNameStyle);
                } else {
                    drawer.Draw(pass.metadata.drawer, rect);
                }
            }
            
            struct InstanceToDraw {
                public TSource source;
                public Metadata metadata;
                public Vector3 screenPoint;
                public float scale;
            }

            readonly struct DrawPass {
                public readonly Vector3 translation;
                public readonly float width;
                public readonly float height;
                public readonly float scale;
                public readonly TSource source;
                public readonly Metadata metadata;

                public DrawPass(Vector3 translation, float width, float height, float scale, TSource source, Metadata metadata) {
                    this.translation = translation;
                    this.width = width;
                    this.height = height;
                    this.scale = scale;
                    this.source = source;
                    this.metadata = metadata;
                }
            }

            struct Metadata {
                public TDrawerMetadata drawer;
                public SystemMetadata system;
            }
        }
    }
}