using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Awaken.ECS.DrakeRenderer.Authoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.ECS.Editor.DrakeRenderer {
    public static class DrakeRendererAuthoringHackManagerEditor {
        const string DrakeAuthoringViewName = "DrakeAuthoringViewHack";
        
        static Dictionary<DrakeLodGroup, GameObject> s_spawnedAuthoringByDrakeLodGroup = new Dictionary<DrakeLodGroup, GameObject>();
        static Dictionary<DrakeMeshRenderer, GameObject> s_spawnedAuthoringByDrakeMeshRenderer = new Dictionary<DrakeMeshRenderer, GameObject>();
        static readonly List<DrakeLodGroup> ToRemoveLodGroups = new List<DrakeLodGroup>();
        static readonly List<DrakeLodGroup> ToSpawnLodGroups = new List<DrakeLodGroup>();
        static readonly List<DrakeMeshRenderer> ConsistencyToRemoveRenderers = new List<DrakeMeshRenderer>();
        static readonly List<DrakeMeshRenderer> ConsistencyToSpawnRenderers = new List<DrakeMeshRenderer>();

        public static void EDITOR_RuntimeReset() {
            foreach ((DrakeLodGroup key, GameObject value) in s_spawnedAuthoringByDrakeLodGroup.ToList()) {
                if (key == null || value == null) {
                    s_spawnedAuthoringByDrakeLodGroup.Remove(key);
                } else {
                    DrakeRendererManagerEditor.DrakeLodGroups.Add(key);
                }
            }

            foreach ((DrakeMeshRenderer key, GameObject value) in s_spawnedAuthoringByDrakeMeshRenderer.ToList()) {
                if (key == null || value == null) {
                    s_spawnedAuthoringByDrakeMeshRenderer.Remove(key);
                } else {
                    DrakeRendererManagerEditor.DrakeMeshRenderers.Add(key);
                }
            }
        }
        
        public static void Start() {
            DrakeRendererManagerEditor.AddedDrakeLodGroups -= OnAddedDrakeLodGroups;
            DrakeRendererManagerEditor.AddedDrakeLodGroups += OnAddedDrakeLodGroups;
            DrakeRendererManagerEditor.AddedDrakeMeshRenderers -= OnAddedDrakeMeshRenderers;
            DrakeRendererManagerEditor.AddedDrakeMeshRenderers += OnAddedDrakeMeshRenderers;
            DrakeRendererManagerEditor.RemovedDrakeLodGroups -= OnRemovedDrakeLodGroups;
            DrakeRendererManagerEditor.RemovedDrakeLodGroups += OnRemovedDrakeLodGroups;
            DrakeRendererManagerEditor.RemovedDrakeMeshRenderer -= OnRemovedDrakeMeshRenderer;
            DrakeRendererManagerEditor.RemovedDrakeMeshRenderer += OnRemovedDrakeMeshRenderer;
            
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;

            foreach (var drakeLodGroup in DrakeRendererManagerEditor.DrakeLodGroups) {
                // Happens with open prefabs when exiting playmode
                if (s_spawnedAuthoringByDrakeLodGroup.ContainsKey(drakeLodGroup)) {
                    continue;
                }
                SpawnAuthoringHack(drakeLodGroup, false);
            }
            foreach (var drakeMeshRenderer in DrakeRendererManagerEditor.DrakeMeshRenderers) {
                // Happens with open prefabs when exiting playmode
                if (s_spawnedAuthoringByDrakeMeshRenderer.ContainsKey(drakeMeshRenderer)) {
                    continue;
                }
                SpawnAuthoringHack(drakeMeshRenderer, false);
            }
        }

        public static void Stop() {
            DrakeRendererManagerEditor.AddedDrakeLodGroups -= OnAddedDrakeLodGroups;
            DrakeRendererManagerEditor.AddedDrakeMeshRenderers -= OnAddedDrakeMeshRenderers;
            DrakeRendererManagerEditor.RemovedDrakeLodGroups -= OnRemovedDrakeLodGroups;
            DrakeRendererManagerEditor.RemovedDrakeMeshRenderer -= OnRemovedDrakeMeshRenderer;
            
            EditorApplication.update -= OnEditorUpdate;

            foreach (var drakeLodGroup in DrakeRendererManagerEditor.DrakeLodGroups) {
                RemoveAuthoringHack(drakeLodGroup);
            }
            foreach (var drakeMeshRenderer in DrakeRendererManagerEditor.DrakeMeshRenderers) {
                RemoveAuthoringHack(drakeMeshRenderer);
            }
        }

        static void OnAddedDrakeLodGroups(HashSet<DrakeLodGroupTransformPair> drakeLodGroups) {
            foreach (var drakeLodGroup in drakeLodGroups) {
                SpawnAuthoringHack(drakeLodGroup.drakeLodGroup, false);
            }
        }

        static void OnAddedDrakeMeshRenderers(HashSet<DrakeMeshRendererTransformPair> drakeMeshRenderers) {
            foreach (var drakeMeshRenderer in drakeMeshRenderers) {
                SpawnAuthoringHack(drakeMeshRenderer.drakeMeshRenderer, false);
            }
        }

        static void OnRemovedDrakeLodGroups(HashSet<DrakeLodGroupTransformPair> drakeLodGroups) {
            foreach (var drakeLodGroup in drakeLodGroups) {
                RemoveAuthoringHack(drakeLodGroup.drakeLodGroup);
            }
        }

        static void OnRemovedDrakeMeshRenderer(HashSet<DrakeMeshRendererTransformPair> drakeMeshRenderers) {
            foreach (var drakeMeshRenderer in drakeMeshRenderers) {
                RemoveAuthoringHack(drakeMeshRenderer.drakeMeshRenderer);
            }
        }
        
        static void OnEditorUpdate() {
            GameObject editingPrefab = default;
            bool hiddenContext = default;
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage) {
                hiddenContext = CoreUtils.IsSceneViewPrefabStageContextHidden();
                editingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabStage.assetPath);
            }

            CheckConsistencyLodGroup(prefabStage, editingPrefab, hiddenContext);
            CheckConsistencyMeshRenderer(prefabStage, editingPrefab, hiddenContext);
        }

        static void SpawnAuthoringHack(DrakeLodGroup drakeLodGroup, bool replace) {
            var authoringView = new GameObject(DrakeAuthoringViewName);
            authoringView.hideFlags = HideFlags.HideInHierarchy |
                                      HideFlags.HideInInspector |
                                      HideFlags.NotEditable |
                                      HideFlags.DontSaveInBuild |
                                      HideFlags.DontSaveInEditor;
            authoringView.transform.SetParent(drakeLodGroup.transform, false);
            authoringView.AddComponent<HackBugRemoval>();
            DrakeEditorHelpers.SpawnAuthoring(drakeLodGroup, authoringView);
            if (replace) {
                s_spawnedAuthoringByDrakeLodGroup[drakeLodGroup] = authoringView;
            } else {
                s_spawnedAuthoringByDrakeLodGroup.Add(drakeLodGroup, authoringView);
            }
        }

        static void SpawnAuthoringHack(DrakeMeshRenderer drakeMeshRenderer, bool replace) {
            if (drakeMeshRenderer.Parent) {
                return;
            }
            var authoringView = new GameObject(DrakeAuthoringViewName);
            authoringView.hideFlags = HideFlags.HideInHierarchy |
                                      HideFlags.HideInInspector |
                                      HideFlags.NotEditable |
                                      HideFlags.DontSaveInBuild |
                                      HideFlags.DontSaveInEditor;
            authoringView.transform.SetParent(drakeMeshRenderer.transform, false);

            var authoringViewHack = new GameObject($"{DrakeAuthoringViewName}2");
            authoringViewHack.hideFlags = HideFlags.DontSaveInBuild |
                                          HideFlags.DontSaveInEditor;
            authoringViewHack.transform.SetParent(authoringView.transform, false);
            authoringViewHack.AddComponent<HackBugRemoval>();
            
            DrakeMeshRendererEditor.SpawnAuthoring(drakeMeshRenderer, authoringViewHack);

            if (replace) {
                s_spawnedAuthoringByDrakeMeshRenderer[drakeMeshRenderer] = authoringView;
            } else {
                s_spawnedAuthoringByDrakeMeshRenderer.Add(drakeMeshRenderer, authoringView);
            }
        }

        static void RemoveAuthoringHack(DrakeLodGroup drakeLodGroup) {
            if (s_spawnedAuthoringByDrakeLodGroup.TryGetValue(drakeLodGroup, out var spawned)) {
                if (spawned) {
                    Object.DestroyImmediate(spawned);
                }
                s_spawnedAuthoringByDrakeLodGroup.Remove(drakeLodGroup);
            }
        }

        static void RemoveAuthoringHack(DrakeMeshRenderer drakeMeshRenderer) {
            if (s_spawnedAuthoringByDrakeMeshRenderer.TryGetValue(drakeMeshRenderer, out var spawned)) {
                if (spawned) {
                    Object.DestroyImmediate(spawned);
                }
                s_spawnedAuthoringByDrakeMeshRenderer.Remove(drakeMeshRenderer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CheckConsistencyLodGroup(PrefabStage prefabStage, GameObject editingPrefab, bool hiddenContext) {
            foreach (var (drakeLodGroup, spawned) in s_spawnedAuthoringByDrakeLodGroup) {
                bool shouldBeSpawned = ShouldBeSpawned(drakeLodGroup, prefabStage, editingPrefab, hiddenContext);
                if (shouldBeSpawned && !spawned) {
                    ToSpawnLodGroups.Add(drakeLodGroup);
                } else if (!shouldBeSpawned && spawned) {
                    ToRemoveLodGroups.Add(drakeLodGroup);
                    Object.DestroyImmediate(spawned);
                }
            }
            foreach (var drakeLodGroup in DrakeRendererManagerEditor.DrakeLodGroups) {
                if (!s_spawnedAuthoringByDrakeLodGroup.ContainsKey(drakeLodGroup) &&
                    ShouldBeSpawned(drakeLodGroup, prefabStage, editingPrefab, hiddenContext)) {
                    SpawnAuthoringHack(drakeLodGroup, false);
                }
            }
            foreach (var toSpawn in ToSpawnLodGroups) {
                SpawnAuthoringHack(toSpawn, true);
            }
            ToSpawnLodGroups.Clear();
            foreach (var toRemoveLodGroup in ToRemoveLodGroups) {
                s_spawnedAuthoringByDrakeLodGroup.Remove(toRemoveLodGroup);
            }
            ToRemoveLodGroups.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CheckConsistencyMeshRenderer(PrefabStage prefabStage, GameObject editingPrefab, bool hiddenContext) {
            ConsistencyToRemoveRenderers.Clear();
            ConsistencyToSpawnRenderers.Clear();
            foreach (var (drakeLodGroup, spawned) in s_spawnedAuthoringByDrakeMeshRenderer) {
                bool shouldBeSpawned = ShouldBeSpawned(drakeLodGroup, prefabStage, editingPrefab, hiddenContext);
                if (shouldBeSpawned && !spawned) {
                    ConsistencyToSpawnRenderers.Add(drakeLodGroup);
                } else if (!shouldBeSpawned && spawned) {
                    ConsistencyToRemoveRenderers.Add(drakeLodGroup);
                    Object.DestroyImmediate(spawned);
                }
            }
            foreach (var drakeMeshRenderer in DrakeRendererManagerEditor.DrakeMeshRenderers) {
                if (!s_spawnedAuthoringByDrakeMeshRenderer.ContainsKey(drakeMeshRenderer) &&
                    ShouldBeSpawned(drakeMeshRenderer, prefabStage, editingPrefab, hiddenContext)) {
                    SpawnAuthoringHack(drakeMeshRenderer, false);
                }
            }
            foreach (var toSpawn in ConsistencyToSpawnRenderers) {
                SpawnAuthoringHack(toSpawn, true);
            }
            ConsistencyToSpawnRenderers.Clear();
            foreach (var toRemoveLodGroup in ConsistencyToRemoveRenderers) {
                s_spawnedAuthoringByDrakeMeshRenderer.Remove(toRemoveLodGroup);
            }
            ConsistencyToRemoveRenderers.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool ShouldBeSpawned(DrakeLodGroup drakeLodGroup, PrefabStage prefabStage, GameObject editingPrefab, bool hiddenContext) {
            return ShouldBeSpawned(drakeLodGroup.gameObject, prefabStage, editingPrefab, hiddenContext);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool ShouldBeSpawned(DrakeMeshRenderer drakeMeshRenderer, PrefabStage prefabStage, GameObject editingPrefab, bool hiddenContext) {
            if (drakeMeshRenderer.Parent) {
                return false;
            }
            return ShouldBeSpawned(drakeMeshRenderer.gameObject, prefabStage, editingPrefab, hiddenContext);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool ShouldBeSpawned(GameObject drakeGameObject, PrefabStage prefabStage, GameObject editingPrefab, bool hiddenContext) {
            if (prefabStage == null) {
                return true;
            }
            if (prefabStage.scene == drakeGameObject.scene) {
                return true;
            }
            return !hiddenContext && !DrakeRendererManagerEditor.IsPartOfEditingPrefab(drakeGameObject, editingPrefab);
        }
    }
}
