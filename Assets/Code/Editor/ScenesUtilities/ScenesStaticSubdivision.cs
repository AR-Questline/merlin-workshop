using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.CommonInterfaces;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.ECS.MedusaRenderer;
using Awaken.TG.Assets;
using Awaken.TG.Editor.Assets;
using Awaken.TG.Editor.Graphics;
using Awaken.TG.Graphics.Culling;
using Awaken.TG.Graphics.Scene;
using Awaken.TG.Graphics.ScriptedEvents;
using Awaken.TG.LeshyRenderer;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.AudioSystem.Biomes;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Scenes.SceneConstructors.AdditiveScenes;
using Awaken.TG.Main.Scenes.SceneConstructors.SubdividedScenes;
using Awaken.TG.Main.Templates.Specs;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Debugging;
using Awaken.Utility.Editor;
using Awaken.Utility.GameObjects;
using Pathfinding;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;
using Object = UnityEngine.Object;
using Path = System.IO.Path;

using static Awaken.Utility.Editor.EditorScenesUtility;
using Awaken.TG.Graphics.DayNightSystem;
using Awaken.TG.Graphics.Statues;
using Awaken.TG.Main.Heroes.FootSteps;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Settings.Controllers;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Enums;
using FMODUnity;

namespace Awaken.TG.Editor.Utility {
    public static class ScenesStaticSubdivision {
        [MenuItem("TG/Build/Static Subdivision/Simulate Static Subdivision in for all scenes", false, 4000)]
        static void SimulateStaticSubdivisionForAllScenes() {
            using var buildBaking = new BuildSceneBaking();
            ExecuteStaticSubdivisionForBuild(BuildTools.GetAllScenes());
        }
        
        [MenuItem("TG/Build/Static Subdivision/Simulate Static Subdivision for selected scenes", false, 4000)]
        static void SimulateStaticSubdivisionForSelectedScenes() {
            using var buildBaking = new BuildSceneBaking();
            ExecuteStaticSubdivisionForBuild(EditorScenesUtility.GetsSelectedScenesPaths());
        }

        [MenuItem("TG/Build/Static Subdivision/Simulate Static Subdivision for current scenes", false, 4000)]
        public static void SimulateStaticSubdivisionForCurrentScenes() {
            using var buildBaking = new BuildSceneBaking();
            ExecuteStaticSubdivisionForBuild(GetCurrentlyOpenScenesPath());
        }
        
        [MenuItem("TG/Build/Static Subdivision/Subdivide selected scenes", false, 4000)]
        static void SubdivideSelected() {
            SubdivideScenes(GetsSelectedScenesPaths());
        }

        [MenuItem("TG/Build/Static Subdivision/Subdivide current scenes", false, 4000)]
        static void SubdivideCurrentScenes() {
            SubdivideScenes(GetCurrentlyOpenScenesPath());
        }

        [MenuItem("TG/Build/Static Subdivision/Subdivide all static scenes", false, 4000)]
        static void SubdivideAll() {
            SubdivideScenes(BuildTools.GetAllScenes());
        }

        [MenuItem("TG/Build/Static Subdivision/Mark common static objects in selected scenes", false, 4000)]
        static void MarkCommonStaticObjectInSelectedScenes() {
            MarkCommonStaticObjectsInScenes(GetsSelectedScenesPaths());
        }

        [MenuItem("TG/Build/Static Subdivision/Mark common static objects in current scenes", false, 4000)]
        static void MarkCommonStaticObjectInCurrentScenes() {
            MarkCommonStaticObjectsInScenes(GetCurrentlyOpenScenesPath());
        }

        [MenuItem("TG/Build/Static Subdivision/Mark common static objects in all scenes", false, 4000)]
        static void MarkCommonStaticObjectInAllSubscenes() {
            MarkCommonStaticObjectsInScenes(BuildTools.GetAllScenes());
        }
        
        public static void ExecuteStaticSubdivisionForBuild(string[] scenesPaths, bool checkIfScenesValidToProcess = true) {
            var scenesToRefresh = new List<string>(3);
            
            foreach (var scenePath in scenesPaths) {
                if ((checkIfScenesValidToProcess && IsValidSceneToProcess(scenePath) == false) || 
                    TryOpenSceneSingle(scenePath, out var scene) == false) {
                    continue;
                }
                ExecuteStaticSubdivisionForBuild(scene, out var hasSubdividedSceneComponent, out var staticScene, checkIfScenesValidToProcess);
                if (hasSubdividedSceneComponent) {
                    scenesToRefresh.Add(scenePath);
                }
            }
            UpdateStaticScenesConfigs();
            AssetDatabase.SaveAssets();
            var scenesToRefreshCount = scenesToRefresh.Count;
            for (int i = 0; i < scenesToRefreshCount; i++) {
                if (TryOpenSceneSingle(scenesToRefresh[i], out var scene)) {
                    try {
                        RefreshSubdividedSceneStaticScenes(scene);
                    } catch (Exception e) {
                        Debug.LogException(e);
                    }
                }
            }
        }
        
        /// <summary>
        /// Executes static subdivision on scene without saving scene. After calling this it is needed to call <see cref="UpdateStaticScenesConfigs"/>,
        /// <see cref="AssetDatabase.SaveAssets"/> and <see cref="RefreshSubdividedSceneStaticScenes"/>
        /// </summary>
        public static void ExecuteStaticSubdivisionForBuild(Scene scene, out SubdividedScene subdividedScene, out Scene staticScene, 
            bool checkIfSceneValidToProcess = true, bool save = true) {
            try {
                var scenePath = scene.path;
                if (checkIfSceneValidToProcess && IsValidSceneToProcess(scene) == false) {
                    Log.Minor?.Error($"Processing unnecessary scene {scenePath}");
                    subdividedScene = null;
                    staticScene = default;
                    return;
                }
                FlattenHierarchyAndMarkStaticObjects(scene, false);
                MarkStaticObjectsInSubdividedScene(scene, false);
                MoveStaticObjectsIntoStaticScene(scene, scenePath, out subdividedScene, out staticScene, save);
            } catch (Exception e) {
                Debug.LogException(e);
                subdividedScene = null;
                staticScene = default;
            }
        }

        public static bool IsValidSceneToProcess(Scene scene) {
            var subdividedScene = GameObjects.FindComponentByTypeInScene<SubdividedScene>(scene, false);
            if (subdividedScene != null) {
                return true;
            }

            var subScene = GameObjects.FindComponentByTypeInScene<SubdividedSceneChild>(scene, false);
            return subScene != null;
        }
        
        static void MarkCommonStaticObjectsInScenes(string[] scenesPaths) {
            var initiallyLoadedScenesLoader = new InitiallyLoadedScenesLoader();
            initiallyLoadedScenesLoader.SaveCurrentScenesAsInitiallyLoaded();
            for (int i = 0; i < scenesPaths.Length; i++) {
                var scenePath = scenesPaths[i];
                if (IsValidSceneToProcess(scenePath) && TryOpenSceneSingle(scenePath, out var scene)) {
                    if (!IsValidSceneToProcess(scene)) {
                        Log.Minor?.Error($"Processing unnecessary scene {scenePath}");
                        continue;
                    }
                    try {
                        FlattenHierarchyAndMarkStaticObjects(scene);
                        MarkStaticObjectsInSubdividedScene(scene);
                    } catch (Exception e) {
                        Debug.LogException(e);
                    }
                }
            }
            initiallyLoadedScenesLoader.RestoreInitiallyLoadedScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void SubdivideScenes(string[] scenesPaths) {
            var initiallyLoadedScenesLoader = new InitiallyLoadedScenesLoader();
            initiallyLoadedScenesLoader.SaveCurrentScenesAsInitiallyLoaded();
            
            using var buildBaking = new BuildSceneBaking();

            // Per scene actions
            var scenesToRefresh = new List<string>(3);
            for (int i = 0; i < scenesPaths.Length; i++) {
                var scenePath = scenesPaths[i];
                if (IsValidSceneToProcess(scenePath) && TryOpenSceneSingle(scenePath, out var scene)) {
                    try {
                        if (!IsValidSceneToProcess(scene)) {
                            Log.Minor?.Error($"Processing unnecessary scene {scenePath}");
                            continue;
                        }
                        ScenesStaticSubdivision.MoveStaticObjectsIntoStaticScene(scene, scenePath,
                            out SubdividedScene subdividedScene, out var staticScene);
                        if (subdividedScene) {
                            scenesToRefresh.Add(scenePath);
                        }
                    } catch (Exception e) {
                        Debug.LogException(e);
                    }
                }
            }

            var scenesToRefreshCount = scenesToRefresh.Count;
            for (int i = 0; i < scenesToRefreshCount; i++) {
                if (TryOpenSceneSingle(scenesToRefresh[i], out var scene)) {
                    try {
                        RefreshSubdividedSceneStaticScenes(scene);
                    } catch (Exception e) {
                        Debug.LogException(e);
                    }
                }
            }

            initiallyLoadedScenesLoader.RestoreInitiallyLoadedScenes();

            UpdateStaticScenesConfigs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void UpdateStaticScenesConfigs() {
            var sceneConfigs = AssetDatabase.LoadAssetAtPath<SceneConfigs>(SceneConfigsWindow.SceneConfigAssetPath);
            sceneConfigs.UpdateSceneList();
            const string staticSceneSuffix = SceneService.StaticSceneSuffix;
            var allScenes = sceneConfigs.AllScenes;
            var allStaticScenes = allScenes.Where(x => x.sceneName.EndsWith(staticSceneSuffix)).ToArray();
            foreach (var staticScene in allStaticScenes) {
                var sceneName =
                    staticScene.sceneName.Substring(0, staticScene.sceneName.Length - staticSceneSuffix.Length);
                if (!sceneConfigs.TryGetSceneConfigData(sceneName, out var sceneConfig)) {
                    Log.Minor?.Error(
                        $"No matching normal scene {nameof(SceneConfig)} for static scene {staticScene.sceneName}");
                    continue;
                }

                sceneConfigs.SetSceneConfigData(staticScene.sceneName, sceneConfig.bake, sceneConfig.APV, sceneConfig.additive);
            }
        }
        
        static void RefreshSubdividedSceneStaticScenes(Scene scene) {
            var subdividedScene = GameObjects.FindComponentByTypeInScene<SubdividedScene>(scene, false);
            subdividedScene.RefreshStaticScenesList();
            EditorSceneManager.SaveScene(scene);
        }

        static void MoveStaticObjectsIntoStaticScene(Scene scene, string scenePath, out SubdividedScene subdividedScene,
            out Scene staticScene, bool save = true) {
            staticScene = default;
            subdividedScene = null;
            try {
                subdividedScene = GameObjects.FindComponentByTypeInScene<SubdividedScene>(scene, false);
                var hasSubdividedSceneComponent = subdividedScene!= null;
                List<GameObject> staticRootGameObjects = GetStaticRootGameObjects(scene);
                if (staticRootGameObjects.Count == 0 && hasSubdividedSceneComponent == false) {
                    Log.Minor?.Info($"Scene {scenePath} static objects count = 0");
                    return;
                }
                MakeCopyOfStaticRootGameObjectIfNeeded(staticRootGameObjects);
                staticRootGameObjects.ForEach(go => go.transform.SetParent(null));
                var sceneDirectoryPath = Path.GetDirectoryName(scenePath)!;
                var staticScenePath = Path.Combine(sceneDirectoryPath, $"{scene.name}{SceneService.StaticSceneSuffix}.unity");

                staticScene = AssetDatabase.AssetPathExists(staticScenePath)
                    ? EditorSceneManager.OpenScene(staticScenePath, OpenSceneMode.Additive)
                    : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                if (!EditorSceneManager.SaveScene(staticScene, staticScenePath)) {
                    Log.Minor?.Error($"Static scene {scene.name} cannot be saved");
                    return;
                }

                if (hasSubdividedSceneComponent && TryGetComponentInChildren(staticRootGameObjects, out LeshyManager leshyManager)) {
                    MoveLeshyBinaryData(leshyManager);
                }

                if (staticRootGameObjects.Count != 0) {
                    var staticObjectsInstanceIds = ConvertGameObjectsListToInstanceIdsNativeArray(staticRootGameObjects, Allocator.Temp);
                    SceneManager.MoveGameObjectsToScene(staticObjectsInstanceIds, staticScene);
                    staticObjectsInstanceIds.Dispose();
                }

                var staticSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(staticScenePath);
                AddressableHelper.AddEntry(new AddressableEntryDraft.Builder(staticSceneAsset)
                    .InGroup(AddressableGroup.Scenes).WithLabels(SceneService.ScenesLabel)
                    .WithAddressProvider(static (obj, _) => obj.name).Build());


                if (!TryGetOrAddSceneRootComponent(scene, staticScene, out _)) {
                    Log.Minor?.Error($"Cannot get or add spec registry to static scene {staticScene.name}");
                }

            } catch (Exception e) {
                Debug.LogException(e);
            } finally {
                if (save) {
                    if (staticScene.IsValid()) {
                        EditorSceneManager.SaveScene(staticScene);
                    }

                    if (scene.IsValid()) {
                        EditorSceneManager.SaveScene(scene);
                    }
                }
            }
        }

        static void MakeCopyOfStaticRootGameObjectIfNeeded(List<GameObject> staticRootGameObjects) {
            int count = staticRootGameObjects.Count;
            for (int i = 0; i < count; i++) {
                if (HasStaticRootTypeToCopyNotMove(staticRootGameObjects[i])) {
                    var copyGO = Object.Instantiate(staticRootGameObjects[i]);
                    staticRootGameObjects[i] = copyGO;
                }
            }
        }

        static void FlattenHierarchyAndMarkStaticObjects(Scene scene, bool save = true) {
            var rootObjects = scene.GetRootGameObjects();
            int rootObjectsCount = rootObjects.Length;
            for (int i = 0; i < rootObjectsCount; i++) {
                var rootObject = rootObjects[i];
                FlattenObjectsHierarchy(rootObject.transform);
            }

            var newRootObjects = scene.GetRootGameObjects();
            var staticParent = new GameObject("StaticObjects");
            staticParent.AddComponent<StaticRootTag>();
            staticParent.AddComponent<DrakeMergedRenderersRoot>();
            staticParent.isStatic = true;
            SceneManager.MoveGameObjectToScene(staticParent, scene);

            ProcessFlatHierarchy(scene, newRootObjects, staticParent.transform);
            if (save) {
                EditorSceneManager.SaveScene(scene);
            }
        }
        
        static void MarkStaticObjectsInSubdividedScene(Scene scene, bool save = true) {
            var isSubdividedScene = GameObjects.FindComponentByTypeInScene<SubdividedScene>(scene, false) != null;
            if (!isSubdividedScene) {
                return;
            }
            var rootObjects = scene.GetRootGameObjects();
            var staticRootGameObjects = new List<GameObject>(3);
            var staticRootComponents = GetSubdividedScenesStaticRootComponentsTypes();
            for (int i = 0; i < staticRootComponents.Length; i++) {
                if (TryGetFirstTopChildWithComponent(rootObjects, staticRootComponents[i], out var staticRootGO)){
                    staticRootGameObjects.Add(staticRootGO);
                }
            }
            for (int i = 0; i < staticRootGameObjects.Count; i++) {
                staticRootGameObjects[i].AddComponent<StaticRootTag>();
            }

            if (save) {
                EditorSceneManager.SaveScene(scene);
            }
        }
        
        static bool IsValidSceneToProcess(string scenePath) {
            return !IsStaticScenePath(scenePath) && !scenePath.Contains("Dev_Scenes") &&
                   !scenePath.Contains("CampaignDungeons") && !scenePath.Contains("CampaignInteriors") && !scenePath.Contains("Prologue");
        }
        
        static void ProcessFlatHierarchy(Scene scene, GameObject[] gameObjects, Transform staticParent) {
            var surfaceTypeParents = new Dictionary<Awaken.TG.Main.Utility.Animations.SurfaceType, Transform>();
            foreach (var surfaceType in RichEnum.AllValuesOfType<Awaken.TG.Main.Utility.Animations.SurfaceType>()) {
                if (surfaceType.InspectorCategory != MeshSurfaceType.AllowedSurfaceType) {
                    continue;
                }
                var surfaceParent = new GameObject("SurfaceType_" + surfaceType.EnumName);
                surfaceParent.AddComponent<MeshSurfaceType>().EDITOR_Init(surfaceType);
                surfaceParent.transform.SetParent(staticParent);
                surfaceParent.isStatic = true;
                surfaceTypeParents.TryAdd(surfaceType, surfaceParent.transform);
            }

            int count = gameObjects.Length;
            for (int i = 0; i < count; i++) {
                var gameObject = gameObjects[i];
                if (IsByDesignRootGameObject(gameObject)) {
                    gameObject.transform.SetParent(null);
                    SceneManager.MoveGameObjectToScene(gameObject, scene);
                } else if (IsStaticObject(gameObject)) {
                    if (IsInvalidStaticObject(gameObject)) {
                        Object.DestroyImmediate(gameObject);
                    } else {
                        MarkObjectStatic(gameObject, withChildren: true);
                        gameObject.transform.SetParent(staticParent);
                        if (gameObject.TryGetComponent<MeshSurfaceType>(out var meshSurfaceType)) {
                            ProcessWithSurfaceType(gameObject, meshSurfaceType, surfaceTypeParents);
                        }
                    }
                } else {
                    // If only transform component
                    if (gameObject.GetComponentCount() == 1) {
                        Object.DestroyImmediate(gameObject);
                    } else {
                        gameObject.transform.SetParent(null);
                    }
                }
            }

            foreach (var surfaceParent in surfaceTypeParents.Values) {
                if (surfaceParent.transform.childCount == 0) {
                    Object.DestroyImmediate(surfaceParent.gameObject);
                }
            }
        }

        public static bool IsInvalidStaticObject(GameObject gameObject) {
            if (!gameObject.activeSelf) {
                return true;
            }

            if (gameObject.CompareTag(InteractionObject.InteractionTag)) {
                return true;
            }

            return false;
        }

        static void MarkObjectStatic(GameObject gameObject, bool withChildren) {
            gameObject.isStatic = true;
            if (!withChildren) {
                return;
            }
            var childCount = gameObject.transform.childCount;
            for (int i = 0; i < childCount; i++) {
                MarkObjectStatic(gameObject.transform.GetChild(i).gameObject, true);
            }
        }

        static readonly List<Collider> CollidersBuffer = new List<Collider>(32);
        static readonly List<Collider> LocalCollidersBuffer = new List<Collider>(6);

        static void ProcessWithSurfaceType(GameObject gameObject, MeshSurfaceType meshSurfaceType, Dictionary<Awaken.TG.Main.Utility.Animations.SurfaceType, Transform> surfaceTypeParents) {
            gameObject.GetComponentsInChildren(CollidersBuffer);
            while (CollidersBuffer.Count > 0) {
                var colliderGO = CollidersBuffer[0].gameObject;
                colliderGO.GetComponents(LocalCollidersBuffer);
                var hasMeshSurfaceType = colliderGO.TryGetComponent<MeshSurfaceType>(out var localMeshSurfaceType);
                localMeshSurfaceType ??= colliderGO.GetComponentInParent<MeshSurfaceType>();

                var surfaceType = localMeshSurfaceType.SurfaceType ?? SurfaceType.TerrainGround;

                var parent = surfaceTypeParents[surfaceType];

                var canMoveWholeObject = colliderGO.GetComponentCount() == LocalCollidersBuffer.Count + (hasMeshSurfaceType ? 2 : 1);
                if (canMoveWholeObject) {
                    colliderGO.transform.SetParent(parent, true);
                    colliderGO.name = "Cl";
                } else {
                    var collidersParent = new GameObject("Cl");
                    collidersParent.transform.SetParent(colliderGO.transform, false);

                    foreach (var collider in LocalCollidersBuffer) {
                        UnityEditorInternal.ComponentUtility.CopyComponent(collider);
                        UnityEditorInternal.ComponentUtility.PasteComponentAsNew(collidersParent);
                        Object.DestroyImmediate(collider);
                    }

                    collidersParent.transform.SetParent(parent, true);
                    collidersParent.isStatic = true;
                }

                if (hasMeshSurfaceType && localMeshSurfaceType != meshSurfaceType) {
                    Object.DestroyImmediate(localMeshSurfaceType);
                }

                foreach (var localCollider in LocalCollidersBuffer) {
                    CollidersBuffer.Remove(localCollider);
                }
                LocalCollidersBuffer.Clear();
            }
            CollidersBuffer.Clear();

            Object.DestroyImmediate(meshSurfaceType);
            while (gameObject.TryGetComponent<MeshSurfaceType>(out var additionalMeshSurfaceType)) {
                Object.DestroyImmediate(additionalMeshSurfaceType);
            }

            RemoveLeaves(gameObject.transform);
        }

        static void FlattenObjectsHierarchy(Transform transform) {
            if (PrefabUtility.IsPartOfPrefabInstance(transform.gameObject)) {
                PrefabUtility.UnpackPrefabInstance(transform.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }

            if (transform.gameObject.TryGetComponent<DistanceCullerGroup>(out var distanceCullerGroup)) {
                distanceCullerGroup.EDITOR_BakeData();
            }

#if UNITY_GAMECORE
            if (transform.TryGetComponent<XboxDelete>(out _)) {
                Object.DestroyImmediate(transform.gameObject);
                return;
            }

            if (transform.TryGetComponent<XboxReplace>(out var xboxReplace)) {
                transform = xboxReplace.Replace();
                if (transform == null) {
                    return;
                }
            }
#endif

            if (IsObjectHierarchyImportant(transform.gameObject)) {
                return;
            }

            var childCount = transform.childCount;
            for (int i = childCount - 1; i >= 0; i--) {
                var child = transform.GetChild(i);
                FlattenObjectsHierarchy(child);
                var wasActive = child.gameObject.activeInHierarchy;
                child.SetParent(null);
                child.gameObject.SetActive(wasActive);
            }
        }

        static void RemoveLeaves(Transform transform) {
            for (int i = transform.childCount - 1; i >= 0; i--) {
                RemoveLeaves(transform.GetChild(i));
            }

            if (transform.IsEmptyLeaf()) {
                Object.DestroyImmediate(transform.gameObject);
            }
        }

        public static bool IsObjectHierarchyImportant(GameObject go) {
            return IsNonStaticObject(go)
                   // || go.HasComponent<VegetationStudioManager>()
                   || go.HasComponent<VSPBiomeManager>()
                   || go.HasComponent<PlayableDirector>()
                   || go.HasComponent<SkinnedMeshRenderer>()
                   || go.HasComponent<IWyrdnightRepellerSource>()
                   || go.HasComponent<StatueRoot>()
                   || go.HasComponent<MeshSurfaceType>()
                   || go.HasComponent<DayNightSystem>()
                   || go.HasComponent<QualityController>()
                   || go.HasComponent<EditorOnlyTransform>()
                   || go.HasComponent<MutableGameObject>()
                   ;
        }

        public static bool IsNonStaticObject(GameObject go) {
            return go.HasComponent<SceneSpec>()
                   || go.HasComponent<IInteractableWithHeroProviderComplex>()
                   || go.HasComponent<View>()
                   || go.HasComponent<ViewComponent>()
                   || go.HasComponent<NpcInteractionBase>()
                   || go.HasComponent<GroupInteraction>()
                   //Should have initial hierarchy and could have non-static objects under it
                   || go.HasComponent<DistanceCullerGroup>()
                   || go.HasComponent<ScriptedEvent>()
                   || go.HasComponent<VolumeQualityController>()
                   || go.HasComponent<IListenerOwner>()
                   ;
        }

        public static bool IsStaticObject(GameObject go) {
            return !IsNonStaticObject(go) && (
                go.HasComponent<DrakeLodGroup>() 
                || go.HasComponent<DrakeMeshRenderer>() 
                // || go.HasComponent<HLODSceneManager>() 
                // || go.HasComponent<AddressableHLODController>() 
                || go.HasComponent<Collider>() 
                || go.HasComponent<MeshRenderer>() 
                || go.HasComponent<LODGroup>() 
                || go.HasComponent<DecalProjector>() 
                || go.HasComponent<NavmeshCut>() 
                || go.HasComponent<VisualEffect>() 
                || go.HasComponent<Light>() 
                || go.HasComponent<DistanceCullerGroup>() 
                || go.HasComponent<MedusaRendererManager>() 
                // || go.HasComponent<VegetationStudioManager>() 
                || go.HasComponent<VSPBiomeManager>() 
                || go.HasComponent<LeshyManager>() 
                || go.HasComponent<AstarPath>()
                || go.HasComponent<StudioEventEmitter>()
                );
        }
        
        public static bool IsByDesignRootGameObject(GameObject go) {
            return go.HasComponent<IScene>() || go.HasComponent<ISubscene>();
        }
        
        static Type[] GetSubdividedScenesStaticRootComponentsTypes() {
            return new[] {
                typeof(LeshyManager),
                // typeof(VegetationStudioManager),
                typeof(MedusaRendererManager),
                typeof(GroundBounds)
            };
        }
        
        static bool HasStaticRootTypeToCopyNotMove(GameObject go) {
            return go.HasComponent<GroundBounds>();
        }

        static bool IsStaticScenePath(string scenePath) {
            int staticSuffixLength = SceneService.StaticSceneSuffix.Length;
            return scenePath.Substring(scenePath.Length - (staticSuffixLength + 6), staticSuffixLength) == SceneService.StaticSceneSuffix;
        }

        static bool TryGetComponentInChildren<T>(List<GameObject> objects, out T component) where T : MonoBehaviour {
            for (int i = 0; i < objects.Count; i++) {
                var rootGO = objects[i];
                component = rootGO.GetComponentInChildren<T>();
                if (component != null) {
                    return true;
                }
            }

            component = null;
            return false;
        }

        static void MoveLeshyBinaryData(LeshyManager leshyManager) {
            var projectPath = Application.dataPath.Remove(Application.dataPath.Length - 6);

            var catalogPrevPathFull = leshyManager.CatalogPath;
            var catalogPrevDirectoryFull = Path.GetDirectoryName(catalogPrevPathFull);
            var catalogNewDirectoryFull =
                catalogPrevDirectoryFull + SceneService.StaticSceneSuffix;
            bool createdNewDirectory = false;
            if (!Directory.Exists(catalogNewDirectoryFull)) {
                Directory.CreateDirectory(catalogNewDirectoryFull);
                createdNewDirectory = true;
            }

            var matricesPrevPathFull = leshyManager.MatricesPath;
            var matricesPrevDirectoryFull = Path.GetDirectoryName(matricesPrevPathFull);
            var matricesNewDirectoryFull = matricesPrevDirectoryFull + SceneService.StaticSceneSuffix;
            if (!Directory.Exists(matricesNewDirectoryFull)) {
                Directory.CreateDirectory(matricesNewDirectoryFull);
                createdNewDirectory = true;
            }

            if (createdNewDirectory) {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            var catalogNewPathFull =
                Path.Combine(catalogNewDirectoryFull, Path.GetFileName(catalogPrevPathFull));

            var catalogPrevPath = catalogPrevPathFull.Remove(0, projectPath.Length);
            var catalogNewPath = catalogNewPathFull.Remove(0, projectPath.Length);
            AssetDatabase.MoveAsset(catalogPrevPath, catalogNewPath);

            var matricesNewPathFull =
                Path.Combine(matricesNewDirectoryFull, Path.GetFileName(matricesPrevPathFull));
            var matricesPrevPath = matricesPrevPathFull.Remove(0, projectPath.Length);
            var matricesNewPath = matricesNewPathFull.Remove(0, projectPath.Length);

            AssetDatabase.MoveAsset(matricesPrevPath, matricesNewPath);

            if (Directory.Exists(catalogPrevDirectoryFull) &&
                !Directory.EnumerateFileSystemEntries(catalogPrevDirectoryFull).Any()) {
                var catalogPrevDirectory = catalogPrevDirectoryFull.Remove(0, projectPath.Length);
                AssetDatabase.DeleteAsset(catalogPrevDirectory);
            }

            if (Directory.Exists(matricesPrevDirectoryFull) &&
                !Directory.EnumerateFileSystemEntries(matricesPrevDirectoryFull).Any()) {
                var matricesPrevDirectory = matricesPrevDirectoryFull.Remove(0, projectPath.Length);
                AssetDatabase.DeleteAsset(matricesPrevDirectory);
            }
        }

        static List<GameObject> GetStaticRootGameObjects(Scene scene) {
            var rootGameObjects = scene.GetRootGameObjects();
            var staticRootGameObjects = new List<GameObject>(20);
            for (int i = 0; i < rootGameObjects.Length; i++) {
                var rootObject = rootGameObjects[i];
                GetFirstTopChildrenWithComponent<StaticRootTag>(rootObject.transform, staticRootGameObjects);
            }

            return staticRootGameObjects;
        }

        static bool TryGetOrAddSceneRootComponent(Scene scene, Scene staticScene, out MonoBehaviour staticSceneRoot) {
            if (!FindSceneRoot(scene, out var sceneWithSpecRegistry)) {
                Log.Minor?.Error($"Scene {scene.name} does not have {nameof(IScene)} nor {nameof(SubdividedSceneChild)} component");
                staticSceneRoot = default;
                return false;
            }

            if (!FindSceneRoot(staticScene, out staticSceneRoot)) {
                var rootGO = new GameObject("Root");
                if (!TryAddSceneRoot(sceneWithSpecRegistry, rootGO, out staticSceneRoot)) {
                    Log.Minor?.Error($"Failed to add scene root to static scene {staticScene.name}");
                    Object.DestroyImmediate(rootGO);
                    return false;
                }

                SceneManager.MoveGameObjectToScene(rootGO, staticScene);
            }

            return true;
        }

        static bool TryAddSceneRoot(MonoBehaviour component, GameObject gameObject, out MonoBehaviour sceneRoot) {
            if (component is SubdividedSceneChild) {
                sceneRoot = gameObject.AddComponent<SubdividedSceneChild>();
                gameObject.AddComponent<DistanceCuller>();
                var decalsCuller = gameObject.AddComponent<StaticDecalsCuller>();
                decalsCuller.enabled = false;
            } else if (component is SubdividedScene) {
                sceneRoot = gameObject.AddComponent<SubdividedSceneChild>();
                gameObject.AddComponent<DistanceCuller>();
            } else if (component is AdditiveScene) {
                sceneRoot = gameObject.AddComponent<AdditiveScene>();
            } else if (component is MapScene) {
                Log.Minor?.Error($"{nameof(MapScene)} should not be processed here. That is an error");
                sceneRoot = default;
                return false;
            } else {
                Log.Minor?.Error($"Type {component.GetType().FullName} is not handled");
                sceneRoot = default;
                return false;
            }

            return true;
        }

        static bool FindSceneRoot(Scene scene, out MonoBehaviour sceneRoot) {
            var rootGameObjects = scene.GetRootGameObjects();
            for (int i = 0; i < rootGameObjects.Length; i++) {
                var rootGO = rootGameObjects[i];
                sceneRoot = rootGO.GetComponent<SubdividedSceneChild>();
                if (sceneRoot != null) {
                    return true;
                }
                sceneRoot = rootGO.GetComponent<IScene>() as MonoBehaviour;
                if (sceneRoot != null) {
                    return true;
                }
            }

            sceneRoot = default;
            return false;

        }

        static NativeArray<int> ConvertGameObjectsListToInstanceIdsNativeArray(List<GameObject> gameObjects, Allocator allocator) {
            int count = gameObjects.Count;
            var instanceIds = new NativeArray<int>(count, allocator);
            for (int i = 0; i < count; i++) {
                instanceIds[i] = gameObjects[i].GetInstanceID();
            }

            return instanceIds;
        }
        
        static void GetFirstTopChildrenWithComponent<T>(Transform root, List<GameObject> gameObjectsWithComponent)
            where T : Component {
            if (root.gameObject.TryGetComponent(out T _)) {
                gameObjectsWithComponent.Add(root.gameObject);
                return;
            }
            var childCount = root.childCount;
            for (int i = 0; i < childCount; i++) {
                GetFirstTopChildrenWithComponent<T>(root.GetChild(i), gameObjectsWithComponent);
            }
        }

        static bool TryGetFirstTopChildWithComponent(GameObject[] rootGameObjects, Type componentType, out GameObject childWithComponent) {
            int count = rootGameObjects.Length;
            for (int i = 0; i < count; i++) {
                if (TryGetFirstTopChildWithComponent(rootGameObjects[i].transform, componentType, out childWithComponent)) {
                    return true;
                }
            }
            childWithComponent = null;
            return false;
        }
        
        static bool TryGetFirstTopChildWithComponent(Transform root, Type componentType, out GameObject childWithComponent) {
            if (root.gameObject.TryGetComponent(componentType, out Component _)) {
                childWithComponent = root.gameObject;
                return true;
            }
            var childCount = root.childCount;
            for (int i = 0; i < childCount; i++) {
                if (TryGetFirstTopChildWithComponent(root.GetChild(i), componentType, out childWithComponent)) {
                    return true;
                }
            }
            childWithComponent = null;
            return false;
        }
    }
}