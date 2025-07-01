using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Awaken.ECS.Components;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.ECS.DrakeRenderer.Components;
using Awaken.ECS.Utils;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.ECS.Editor.DrakeRenderer {
    public static class DrakeRendererEntitiesManagerEditor {
        static readonly PropertyInfo PrefabStageIsValidProperty = typeof(PrefabStage).GetProperty("isValid", BindingFlags.Instance | BindingFlags.NonPublic);

        static readonly List<GameObject> ToSpawnLater = new List<GameObject>();

        static PrefabStage s_currentPrefabStage;
        static bool s_isPrefabStageContextHidden;

        public static void Start() {
            DrakeRendererManagerEditor.AddedDrakeLodGroups -= OnAddedDrakeLodGroups;
            DrakeRendererManagerEditor.AddedDrakeLodGroups += OnAddedDrakeLodGroups;
            DrakeRendererManagerEditor.AddedDrakeMeshRenderers -= OnAddedDrakeMeshRenderers;
            DrakeRendererManagerEditor.AddedDrakeMeshRenderers += OnAddedDrakeMeshRenderers;
            DrakeRendererManagerEditor.RemovedDrakeLodGroups -= OnRemovedDrakeLodGroups;
            DrakeRendererManagerEditor.RemovedDrakeLodGroups += OnRemovedDrakeLodGroups;
            DrakeRendererManagerEditor.RemovedDrakeMeshRenderer -= OnRemovedDrakeMeshRenderer;
            DrakeRendererManagerEditor.RemovedDrakeMeshRenderer += OnRemovedDrakeMeshRenderer;

            DrakeRendererManagerEditorCallbacks.PrefabStageOpened -= OnPrefabStageOpened;
            DrakeRendererManagerEditorCallbacks.PrefabStageOpened += OnPrefabStageOpened;
            DrakeRendererManagerEditorCallbacks.PrefabStageChanged -= OnPrefabStageChanged;
            DrakeRendererManagerEditorCallbacks.PrefabStageChanged += OnPrefabStageChanged;
            DrakeRendererManagerEditorCallbacks.PrefabStageClosed -= OnPrefabStageClosed;
            DrakeRendererManagerEditorCallbacks.PrefabStageClosed += OnPrefabStageClosed;
            DrakeRendererManagerEditorCallbacks.PrefabStageContextChanged -= OnPrefabStageContextChange;
            DrakeRendererManagerEditorCallbacks.PrefabStageContextChanged += OnPrefabStageContextChange;

            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) {
                return;
            }

            s_currentPrefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            s_isPrefabStageContextHidden = CoreUtils.IsSceneViewPrefabStageContextHidden();

            var editingPrefab = s_currentPrefabStage ?
                AssetDatabase.LoadAssetAtPath<GameObject>(s_currentPrefabStage.assetPath) :
                null;
            foreach (var drakeLodGroup in DrakeRendererManagerEditor.DrakeLodGroups) {
                if (CanBeSpawnInCurrentContext(s_currentPrefabStage, s_isPrefabStageContextHidden, drakeLodGroup.gameObject, editingPrefab)) {
                    drakeLodGroup.Spawn();
                }
            }
            foreach (var drakeMeshRenderer in DrakeRendererManagerEditor.DrakeMeshRenderers) {
                if (CanBeSpawnInCurrentContext(s_currentPrefabStage, s_isPrefabStageContextHidden, drakeMeshRenderer.gameObject, editingPrefab)) {
                    drakeMeshRenderer.Spawn();
                }
            }
        }

        public static void Stop() {
            DrakeRendererManagerEditor.AddedDrakeLodGroups -= OnAddedDrakeLodGroups;
            DrakeRendererManagerEditor.AddedDrakeMeshRenderers -= OnAddedDrakeMeshRenderers;
            DrakeRendererManagerEditor.RemovedDrakeLodGroups -= OnRemovedDrakeLodGroups;
            DrakeRendererManagerEditor.RemovedDrakeMeshRenderer -= OnRemovedDrakeMeshRenderer;

            DrakeRendererManagerEditorCallbacks.PrefabStageOpened -= OnPrefabStageOpened;
            DrakeRendererManagerEditorCallbacks.PrefabStageChanged -= OnPrefabStageChanged;
            DrakeRendererManagerEditorCallbacks.PrefabStageClosed -= OnPrefabStageClosed;
            DrakeRendererManagerEditorCallbacks.PrefabStageContextChanged -= OnPrefabStageContextChange;

            EditorApplication.update -= OnEditorUpdate;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) {
                return;
            }

            var entityManager = world.EntityManager;
            entityManager.CompleteAllTrackedJobs();
            using var query = entityManager.CreateEntityQuery(typeof(SystemRelatedLifeTime<DrakeRendererManager>.IdComponent));
            entityManager.DestroyEntity(query);

            s_currentPrefabStage = null;
        }

        static void OnAddedDrakeLodGroups(HashSet<DrakeLodGroupTransformPair> drakeLodGroups) {
            // If the world is empty then EditorAwakenEcsBootstrap wasn't called yet
            if (World.DefaultGameObjectInjectionWorld == null) {
                return;
            }
            var editingPrefab = s_currentPrefabStage ?
                AssetDatabase.LoadAssetAtPath<GameObject>(s_currentPrefabStage.assetPath) :
                null;

            foreach (var drakeLodGroup in drakeLodGroups) {
                var gameObject = drakeLodGroup.drakeLodGroup.gameObject;
                if (CanBeSpawnInCurrentContext(s_currentPrefabStage, s_isPrefabStageContextHidden, gameObject,
                        editingPrefab)) {
                    drakeLodGroup.drakeLodGroup.Spawn();
                }
            }
        }

        static void OnAddedDrakeMeshRenderers(HashSet<DrakeMeshRendererTransformPair> drakeMeshRenderers) {
            // If the world is empty then EditorAwakenEcsBootstrap wasn't called yet
            if (World.DefaultGameObjectInjectionWorld == null) {
                return;
            }
            var editingPrefab = s_currentPrefabStage ?
                AssetDatabase.LoadAssetAtPath<GameObject>(s_currentPrefabStage.assetPath) :
                null;
            foreach (var drakeMeshRenderer in drakeMeshRenderers) {
                var gameObject = drakeMeshRenderer.drakeMeshRenderer.gameObject;
                if (CanBeSpawnInCurrentContext(s_currentPrefabStage, s_isPrefabStageContextHidden, gameObject,
                        editingPrefab)) {
                    drakeMeshRenderer.drakeMeshRenderer.Spawn();
                }
            }
        }

        static void OnRemovedDrakeLodGroups(HashSet<DrakeLodGroupTransformPair> drakeLodGroups) {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) {
                return;
            }

            DespawnMultiple(world, drakeLodGroups.Select(static dl => dl.transform.GetHashCode()).ToHashSet());
        }

        static void OnRemovedDrakeMeshRenderer(HashSet<DrakeMeshRendererTransformPair> drakeMeshRenderers) {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) {
                return;
            }

            DespawnMultiple(world, drakeMeshRenderers.Select(static dl => dl.transform.GetHashCode()).ToHashSet());
        }
        
        static void OnPrefabStageOpened(PrefabStage prefabStage, bool hiddenContext) {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) {
                return;
            }

            s_currentPrefabStage = prefabStage;
            s_isPrefabStageContextHidden = hiddenContext;
            
            var toDespawn = CollectToDespawn(prefabStage, hiddenContext);
            
            var entityManager = world.EntityManager;
            entityManager.CompleteAllTrackedJobs();
            using var query = entityManager.CreateEntityQuery(typeof(LinkedTransformComponent));
            using var entitiesToCheck = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entitiesToCheck) {
                var linkedTransform = entityManager.GetComponentData<LinkedTransformComponent>(entity);
                if (toDespawn.Contains(linkedTransform.transform.GetHashCode())) {
                    entityManager.DestroyEntity(entity);
                }
            }
        }
        
        static void OnPrefabStageChanged(PrefabStage prefabStage, bool hiddenContext) {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) {
                return;
            }
            
            s_currentPrefabStage = prefabStage;
            s_isPrefabStageContextHidden = hiddenContext;

            var toDespawn = CollectToDespawn(prefabStage, hiddenContext);
            var allTransforms = CollectAllTransforms();
            allTransforms.ExceptWith(toDespawn);

            var entityManager = world.EntityManager;
            entityManager.CompleteAllTrackedJobs();
            using var query = entityManager.CreateEntityQuery(typeof(LinkedTransformComponent));
            using var entitiesToCheck = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entitiesToCheck) {
                var linkedTransform = entityManager.GetComponentData<LinkedTransformComponent>(entity);
                if (toDespawn.Contains(linkedTransform.transform.GetHashCode())) {
                    entityManager.DestroyEntity(entity);
                }
                allTransforms.Remove(linkedTransform.transform.GetHashCode());
            }
            
            SpawnMissingDrakes(allTransforms);
        }
        
        static void OnPrefabStageClosed() {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) {
                return;
            }
            
            s_currentPrefabStage = null;
            
            var allTransforms = CollectAllTransforms();
            
            var entityManager = world.EntityManager;
            entityManager.CompleteAllTrackedJobs();
            using var query = entityManager.CreateEntityQuery(typeof(LinkedTransformComponent));
            using var entitiesToCheck = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entitiesToCheck) {
                var linkedTransform = entityManager.GetComponentData<LinkedTransformComponent>(entity);
                allTransforms.Remove(linkedTransform.transform.GetHashCode());
            }
            
            SpawnMissingDrakes(allTransforms);
        }

        static void OnPrefabStageContextChange(PrefabStage prefabStage, bool hiddenContext) {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) {
                return;
            }
            
            s_currentPrefabStage = prefabStage;
            s_isPrefabStageContextHidden = hiddenContext;

            var toDespawn = CollectToDespawn(prefabStage, hiddenContext);
            var allTransforms = CollectAllTransforms();
            allTransforms.ExceptWith(toDespawn);

            var entityManager = world.EntityManager;
            entityManager.CompleteAllTrackedJobs();
            using var query = entityManager.CreateEntityQuery(typeof(LinkedTransformComponent));
            using var entitiesToCheck = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entitiesToCheck) {
                var linkedTransform = entityManager.GetComponentData<LinkedTransformComponent>(entity);
                if (toDespawn.Contains(linkedTransform.transform.GetHashCode())) {
                    entityManager.DestroyEntity(entity);
                }
                allTransforms.Remove(linkedTransform.transform.GetHashCode());
            }
            
            SpawnMissingDrakes(allTransforms);
        }

        static void OnEditorUpdate() {
            if (ToSpawnLater.Count == 0) {
                return;
            }
            var toSpawn = new HashSet<int>();
            foreach (var gameObject in ToSpawnLater) {
                if (!gameObject) {
                    continue;
                }
                var editingPrefab = s_currentPrefabStage ?
                    AssetDatabase.LoadAssetAtPath<GameObject>(s_currentPrefabStage.assetPath) :
                    null;
                if (CanBeSpawnInCurrentContext(s_currentPrefabStage, s_isPrefabStageContextHidden, gameObject, editingPrefab)) {
                    toSpawn.Add(gameObject.transform.GetHashCode());
                }
            }
            ToSpawnLater.Clear();
            SpawnMissingDrakes(toSpawn);
        }

        static HashSet<int> CollectToDespawn(PrefabStage prefabStage, bool hiddenContext) {
            var toDespawn = new HashSet<int>();
            var editingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabStage.assetPath);
            foreach (var drakeMeshRenderer in DrakeRendererManagerEditor.DrakeMeshRenderers) {
                if (!drakeMeshRenderer) {
                    continue;
                }
                var gameObject = drakeMeshRenderer.gameObject;
                if (!CanBeSpawnInCurrentContext(prefabStage, hiddenContext, gameObject, editingPrefab)) {
                    toDespawn.Add(gameObject.transform.GetHashCode());
                }
            }
            foreach (var drakeLodGroup in DrakeRendererManagerEditor.DrakeLodGroups) {
                if (!drakeLodGroup) {
                    continue;
                }
                var gameObject = drakeLodGroup.gameObject;
                if (!CanBeSpawnInCurrentContext(prefabStage, hiddenContext, gameObject, editingPrefab)) {
                    toDespawn.Add(gameObject.transform.GetHashCode());
                }
            }
            return toDespawn;
        }

        static HashSet<int> CollectAllTransforms() {
            var allTransforms = new HashSet<int>(DrakeRendererManagerEditor.DrakeMeshRenderers.Count + DrakeRendererManagerEditor.DrakeLodGroups.Count);
            foreach (var drakeMeshRenderer in DrakeRendererManagerEditor.DrakeMeshRenderers) {
                if (!drakeMeshRenderer.Parent) {
                    allTransforms.Add(drakeMeshRenderer.transform.GetHashCode());
                }
            }
            foreach (var drakeLodGroup in DrakeRendererManagerEditor.DrakeLodGroups) {
                allTransforms.Add(drakeLodGroup.transform.GetHashCode());
            }
            return allTransforms;
        }
        
        static void SpawnMissingDrakes(HashSet<int> missingTransforms) {
            foreach (var transform in missingTransforms) {
                var lodGroup = ((Transform)Resources.InstanceIDToObject(transform)).GetComponent<DrakeLodGroup>();
                if (lodGroup) {
                    lodGroup.Spawn();
                } else {
                    ((Transform)Resources.InstanceIDToObject(transform)).GetComponent<DrakeMeshRenderer>().Spawn();
                }
            }
        }

        static bool CanBeSpawnInCurrentContext(PrefabStage prefabStage, bool hiddenContext, GameObject gameObject, GameObject editingPrefab) {
            if (prefabStage == null) {
                return true;
            }
            if (((bool)PrefabStageIsValidProperty.GetValue(prefabStage)) == false) {
                ToSpawnLater.Add(gameObject);
                return false;
            }
            if (prefabStage.scene == gameObject.scene) {
                return true;
            }
            return !hiddenContext && !DrakeRendererManagerEditor.IsPartOfEditingPrefab(gameObject, editingPrefab);
        }

        static void DespawnMultiple(World world, HashSet<int> drakeTransforms) {
            var entityManager = world.EntityManager;
            entityManager.CompleteAllTrackedJobs();
            using var query = entityManager.CreateEntityQuery(typeof(LinkedTransformComponent));
            using var entitiesToCheck = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entitiesToCheck) {
                var linkedTransform = entityManager.GetComponentData<LinkedTransformComponent>(entity);
                if (drakeTransforms.Contains(linkedTransform.transform.GetHashCode())) {
                    entityManager.DestroyEntity(entity);
                }
            }
        }
    }
}
