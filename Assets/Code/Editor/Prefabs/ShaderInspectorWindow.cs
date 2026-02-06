using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.Utility;
using Awaken.Utility.GameObjects;
using Awaken.Utility.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Editor.Prefabs {
    public class ShaderInspectorWindow : EditorWindow {
        TableEntry[] _entries = Array.Empty<TableEntry>();
        TableEntry[] _shaders = Array.Empty<TableEntry>();
        TableEntry[] _prefabs = Array.Empty<TableEntry>();

        HashSet<Shader> _expandedShaders = new HashSet<Shader>(new ShaderComparer());
        HashSet<GameObject> _expandedPrefabs = new HashSet<GameObject>(new PrefabComparer());

        Vector2 _tableScroll;
        ImguiTable<TableEntry> _table;

        void OnEnable() {
            Initialize();
        }

        void Initialize() {
            _table = new ImguiTable<TableEntry>(
                CanRenderEntry,
                EditorGUIUtility.singleLineHeight,

                new ImguiTable<TableEntry>.ColumnDefinition("Expand", Width.Fixed(26), DrawExpandShader, _ => 0, TotalDrawer, ShaderNameAscending, ShaderNameDescending),
                new ImguiTable<TableEntry>.ColumnDefinition("Shader", Width.Fixed(256), DrawShader, _ => 0, TotalDrawer, ShaderNameAscending, ShaderNameDescending),

                new ImguiTable<TableEntry>.ColumnDefinition("Expand", Width.Fixed(26), DrawExpandPrefab, _ => 0, TotalDrawer, PrefabNameAscending, PrefabNameDescending),
                new ImguiTable<TableEntry>.ColumnDefinition("Prefab", Width.Fixed(256), DrawPrefab, _ => 0, TotalDrawer, PrefabNameAscending, PrefabNameDescending),

                new ImguiTable<TableEntry>.ColumnDefinition("Scene object", Width.Fixed(256), DrawSceneObject, _ => 0, TotalDrawer, SceneObjectNameAscending, SceneObjectNameDescending),

                ImguiTable<TableEntry>.ColumnDefinition.CreateNumeric("Count", Width.Fixed(36), ImguiTableUtils.FloatDrawer, e => e.count)
            );

            _table.ShowToolbar = false;
            _table.ShowFooter = false;
            _table.Margin = EditorGUIUtility.standardVerticalSpacing / 2f;

            Sort();
        }


        void OnDisable() {
            _entries = null;
            _expandedShaders.Clear();
            _expandedPrefabs.Clear();
            _table.Dispose();
        }

        void OnGUI() {
            var wholeRect = new PropertyDrawerRects(new Rect(0, 0, position.width, position.height));

            var findButtonRect = wholeRect.AllocateBottom(EditorGUIUtility.singleLineHeight);
            if (GUI.Button(findButtonRect, "Find Shaders")) {
                FindShaders();
            }

            wholeRect.AllocateBottom(EditorGUIUtility.standardVerticalSpacing);

            var tableRect = (Rect)wholeRect;
            _tableScroll = GUILayout.BeginScrollView(_tableScroll, GUILayout.Height(tableRect.height), GUILayout.Width(tableRect.width));
            if (_entries != null) {
                if (_table.Draw(_entries, tableRect.height, _tableScroll.y, tableRect.width)) {
                    Sort();
                }
            }
            GUILayout.EndScrollView();
        }

        bool CanRenderEntry(TableEntry entry, SearchPattern _) {
            if (entry.sceneObject == null) {
                return entry.prefab == null || _expandedShaders.Contains(entry.shader);
            }

            return _expandedShaders.Contains(entry.shader) && (!entry.prefab || _expandedPrefabs.Contains(entry.prefab));
        }

        void DrawExpandShader(in Rect rect, TableEntry element) {
            if (element.sceneObject || element.prefab) {
                return;
            }
            var isExpanded = _expandedShaders.Contains(element.shader);
            if (GUI.Button(rect, isExpanded ? "▼" : "▶", EditorStyles.miniButton)) {
                if (isExpanded) {
                    _expandedShaders.Remove(element.shader);
                } else {
                    _expandedShaders.Add(element.shader);
                }
            }
        }

        void DrawShader(in Rect rect, TableEntry element) {
            GUI.enabled = false;
            EditorGUI.ObjectField(rect, element.shader, typeof(Shader), false);
            GUI.enabled = true;
        }

        void DrawExpandPrefab(in Rect rect, TableEntry element) {
            if (element.sceneObject || element.prefab == null) {
                return;
            }
            var isExpanded = _expandedPrefabs.Contains(element.prefab);
            if (GUI.Button(rect, isExpanded ? "▼" : "▶", EditorStyles.miniButton)) {
                if (isExpanded) {
                    _expandedPrefabs.Remove(element.prefab);
                } else {
                    _expandedPrefabs.Add(element.prefab);
                }
            }
        }

        void DrawPrefab(in Rect rect, TableEntry element) {
            if (element.prefab == null) {
                return;
            }
            GUI.enabled = false;
            EditorGUI.ObjectField(rect, element.prefab, typeof(GameObject), false);
            GUI.enabled = true;
        }

        void DrawSceneObject(in Rect rect, TableEntry element) {
            if (element.sceneObject == null) {
                return;
            }
            GUI.enabled = false;
            EditorGUI.ObjectField(rect, element.sceneObject, typeof(GameObject), false);
            GUI.enabled = true;
        }

        void FindShaders() {
            var renderers = new List<DrakeMeshRenderer>(1024);
            for (int i = 0; i < SceneManager.sceneCount; i++) {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) {
                    continue;
                }
                GameObjects.FindComponentsByTypeInScene(scene, false, ref renderers);
            }

            var entries = new List<TableEntry>();
            _expandedShaders.Clear();
            _expandedPrefabs.Clear();

            var uniqueShaders = new Dictionary<Shader, uint>();
            var uniquePrefabs = new Dictionary<ShaderPrefabEntry, uint>();

            foreach (var renderer in renderers) {
                GameObject prefab = null;
                var source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(renderer.gameObject);
                if (source && source != renderer.gameObject) {
                    prefab = source.transform.root.gameObject;
                }

                var materials = renderer.EDITOR_GetMaterials();

                for (var i = 0; i < materials.Length; i++) {
                    var material = materials[i];
                    if (material == null) {
                        continue;
                    }
                    var shader = material.shader;
                    if (shader == null) {
                        continue;
                    }

                    var shaderCounter = uniqueShaders.GetValueOrDefault(shader);
                    uniqueShaders[shader] = shaderCounter + 1;

                    var entry = new TableEntry {
                        shader = shader,
                        prefab = prefab,
                        sceneObject = renderer.gameObject,
                        count = 1
                    };

                    entries.Add(entry);

                    if (prefab) {
                        var shaderPrefabEntry = new ShaderPrefabEntry {
                            shader = shader,
                            prefab = prefab
                        };
                        var prefabCounter = uniquePrefabs.GetValueOrDefault(shaderPrefabEntry);
                        uniquePrefabs[shaderPrefabEntry] = prefabCounter + 1;
                    }
                }
            }

            foreach (var (uniqueShader, count) in uniqueShaders) {
                entries.Add(new TableEntry {
                    shader = uniqueShader,
                    prefab = null,
                    sceneObject = null,
                    count = count
                });
            }

            foreach (var (uniquePrefab, count) in uniquePrefabs) {
                entries.Add(new TableEntry {
                    shader = uniquePrefab.shader,
                    prefab = uniquePrefab.prefab,
                    sceneObject = null,
                    count = count
                });
            }

            _entries = entries.ToArray();

            _shaders = entries
                .Where(e => e.sceneObject == null && e.prefab == null)
                .ToArray();

            _prefabs = entries
                .Where(e => e.sceneObject == null && e.prefab != null)
                .ToArray();
            Sort();
        }

        void Sort() {
            Array.Sort(_shaders, _table.Sorter);

            Array.Sort(_prefabs, (l, r) => {
                var shaderLIndex = Array.FindIndex(_shaders, e => e.shader == l.shader);
                var shaderRIndex = Array.FindIndex(_shaders, e => e.shader == r.shader);
                if (shaderLIndex < shaderRIndex) {
                    return -1;
                }
                if (shaderLIndex > shaderRIndex) {
                    return 1;
                }
                return _table.Sorter(l, r);
            });

            Array.Sort(_entries, (l, r) => {
                var shaderLIndex = Array.FindIndex(_shaders, e => e.shader == l.shader);
                var shaderRIndex = Array.FindIndex(_shaders, e => e.shader == r.shader);
                if (shaderLIndex < shaderRIndex) {
                    return -1;
                }
                if (shaderLIndex > shaderRIndex) {
                    return 1;
                }

                var prefabLIndex = Array.FindIndex(_prefabs, e => e.prefab == l.prefab);
                var prefabRIndex = Array.FindIndex(_prefabs, e => e.prefab == r.prefab);
                if (prefabLIndex < prefabRIndex) {
                    return -1;
                }
                if (prefabLIndex > prefabRIndex) {
                    return 1;
                }
                return _table.Sorter(l, r);
            });
        }

        static int ShaderNameAscending(TableEntry l, TableEntry r) {
            return string.Compare(l.shader.name, r.shader.name, StringComparison.Ordinal);
        }
        static int ShaderNameDescending(TableEntry l, TableEntry r) {
            return string.Compare(r.shader.name, l.shader.name, StringComparison.Ordinal);
        }

        static int PrefabNameAscending(TableEntry l, TableEntry r) {
            return string.Compare(l.prefab?.name ?? "", r.prefab?.name ?? "", StringComparison.Ordinal);
        }
        static int PrefabNameDescending(TableEntry l, TableEntry r) {
            return string.Compare(r.prefab?.name ?? "", l.prefab?.name ?? "", StringComparison.Ordinal);
        }

        static int SceneObjectNameAscending(TableEntry l, TableEntry r) {
            return string.Compare(l.sceneObject?.name ?? "", r.sceneObject?.name ?? "", StringComparison.Ordinal);
        }
        static int SceneObjectNameDescending(TableEntry l, TableEntry r) {
            return string.Compare(r.sceneObject?.name ?? "", l.sceneObject?.name ?? "", StringComparison.Ordinal);
        }

        static void TotalDrawer(in Rect rect, float _) {
            GUI.Label(rect, "Total");
        }

        struct TableEntry {
            public Shader shader;
            public GameObject prefab;
            public GameObject sceneObject;
            public float count; // Should be uint but table API takes float
        }

        struct ShaderPrefabEntry : IEquatable<ShaderPrefabEntry> {
            public Shader shader;
            public GameObject prefab;

            public bool Equals(ShaderPrefabEntry other) {
                return Equals(shader, other.shader) && Equals(prefab, other.prefab);
            }

            public override bool Equals(object obj) {
                return obj is ShaderPrefabEntry other && Equals(other);
            }

            public override int GetHashCode() {
                unchecked {
                    return ((shader != null ? shader.GetHashCode() : 0) * 397) ^ (prefab != null ? prefab.GetHashCode() : 0);
                }
            }
        }

        [MenuItem("TG/Assets/Shaders inspector", false, Int32.MaxValue)]
        static void ShowWindow() {
            var window = GetWindow<ShaderInspectorWindow>();
            window.titleContent = new GUIContent("Shaders inspector");
            window.Show();
        }

        [InitializeOnLoadMethod] // Runs after compilation
        static void OnScriptReload() {
            // Get all open EditorWindows of this type and reinitialize them
            var windows = Resources.FindObjectsOfTypeAll<ShaderInspectorWindow>();
            foreach (var window in windows) {
                window.Initialize();
            }
        }

        class ShaderComparer : IEqualityComparer<Shader> {
            public bool Equals(Shader x, Shader y) {
                return x.GetHashCode() == y.GetHashCode();
            }

            public int GetHashCode(Shader obj) {
                return obj.GetHashCode();
            }
        }

        class PrefabComparer : IEqualityComparer<GameObject> {
            public bool Equals(GameObject x, GameObject y) {
                return x.GetHashCode() == y.GetHashCode();
            }

            public int GetHashCode(GameObject obj) {
                return obj.GetHashCode();
            }
        }
    }
}