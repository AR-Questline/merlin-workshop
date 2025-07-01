using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.SimpleTools;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Locations.Pickables;
using Awaken.TG.Main.Locations.Regrowables;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Pathfinding;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Validation {
    public class InvalidFindersResultWindow : OdinEditorWindow {
        [Serializable]
        public struct Result {
            [DisplayAsString, TableColumnWidth(200)]
            public string scene;
            [ReadOnly]
            public GameObject gameObject;

            [Button, TableColumnWidth(50, false)]
            void GoTo() {
                Selection.activeObject = gameObject;
                SceneView.FrameLastActiveSceneView();
            }
        }

        [Button, HorizontalGroup]
        void InvalidPickables() {
            InvalidSetupFinders.FindInvalidPickableSpecs(results);
        }
        
        [Button, HorizontalGroup("Invokes")]
        void InteractionsOffNavMesh(float minDistance = 0.5f) {
            InvalidSetupFinders.FindFloatingInteractions(results, minDistance);
        }
        
        [Button, HorizontalGroup]
        void FloatingObjects() {
            InvalidSetupFinders.FindAndLogFloatingObjectsInScenes(results);
        }
        
        [SerializeField, TableList(IsReadOnly = true), PropertyOrder(1)]
        List<Result> results = new();
        
        [MenuItem("TG/Assets/Find Invalid Scene Interactables", priority = -100)]
        public static void Open() {
            var window = GetWindow<InvalidFindersResultWindow>("Floating Objects");
            window.Show();
        }
    }
    
    public static class InvalidSetupFinders {
        static List<SimpleInteractionBase> s_simpleInteractions = new();
        static RaycastHit[] s_rayHits;
        static List<Transform> s_transforms;
        static HashSet<Transform> s_visitedObjects;
        static Bounds s_boundsCache;
        
        public static void EDITOR_RuntimeReset() {
            s_simpleInteractions.Clear();
            s_rayHits = null;
            s_transforms = null;
            s_visitedObjects = null;
        }
        
        public static void FindInvalidPickableSpecs(List<InvalidFindersResultWindow.Result> results) {
            results.Clear();
            foreach (var pickable in Object.FindObjectsByType<PickableSpec>(FindObjectsSortMode.None)) {
                if (pickable.TryGetValidPrefab(out var handle)) {
                    if (handle.EditorLoad<GameObject>() == null) {
                        Log.Important?.Error(
                            $"PickableSpec <color=blue>{pickable.name}</color> has invalid prefab reference." + 
                            "\nDrop prefab is set to invalid asset! See error above", 
                            pickable
                        );
                    }
                }
            }
        }

        public static void FindFloatingInteractions(List<InvalidFindersResultWindow.Result> results, float minDistance) {
            results.Clear();
            Scene[] scenes = new Scene[SceneManager.sceneCount];
            for (int i = 0; i < SceneManager.sceneCount; i++) {
                scenes[i] = SceneManager.GetSceneAt(i);
            }

            GameObjects.FindComponentsByTypeInScenes(scenes, true, ref s_simpleInteractions);
            AstarData activeData = AstarPath.active.data;
            
            if (((RecastGraph)activeData.graphs[0]).tileXCount == 0) {
                activeData.LoadFromCache();
            }
            
            float minDistanceSqr = minDistance * minDistance;
            
            for (int i = 0; i < s_simpleInteractions.Count; i++) {
                var current = s_simpleInteractions[i];
                Vector3 interactionPosition = current.transform.position;
                var closestPoint = AstarPath.active.GetNearest(interactionPosition).position;
                if ((closestPoint - interactionPosition).sqrMagnitude > minDistanceSqr) {
                    GameObject currentGameObject = current.gameObject;
                    results.Add(new InvalidFindersResultWindow.Result {
                        scene = currentGameObject.scene.name,
                        gameObject = currentGameObject
                    });
                }
            }
            
            s_simpleInteractions.Clear();
        }
        
        static HashSet<string> s_gameObjectNamesToIgnore = new() {
            "Light_Ext_Lantern",
            "Prefab_Zombie_Hanging",
            "Light_Int_Lantern_01_Lit",
        };
        
        public static void FindAndLogFloatingObjectsInScenes(List<InvalidFindersResultWindow.Result> results) {
            s_rayHits = new RaycastHit[64];
            s_transforms = new List<Transform>(10000);
            s_visitedObjects = new HashSet<Transform>(10000);
            results.Clear();
            
            var progress = ProgressBar.Create("Finding floating objects", "Finding floating objects in scenes", true);
            var sceneProgress = progress.TakeRestsFor(SceneManager.sceneCount, "Scenes");
            
            for (int i = 0; i < SceneManager.sceneCount; i++) {
                var scene = SceneManager.GetSceneAt(i);
                var currentSceneProgress = sceneProgress.Next(scene.name);
                if (scene.name.StartsWith("CampaignMap")) continue;
                GameObject[] goes = scene.GetRootGameObjects();
                
                var rootProgress = currentSceneProgress.TakeRestsFor(goes.Length, "Root objects");
                for (var rootIndex = 0; rootIndex < goes.Length; rootIndex++) {
                    GameObject go = goes[rootIndex];
                    var currentRootProgress = rootProgress.Next(go.name);
                    if (go.name == "Vegetation") continue;

                    // find the deepest nested prefabs and then for each child collider that is not a trigger. do a box cast based on the bounds of the collider
                    // if the box cast returns nothing, log the object

                    s_transforms.Clear();
                    s_visitedObjects.Clear();
                    go.GetComponentsInChildren(true, s_transforms);

                    for (var childTransformIndex = 0; childTransformIndex < s_transforms.Count; childTransformIndex++) {
                        Transform transform = s_transforms[childTransformIndex];
                        if (currentRootProgress.DisplayCancellable((float) childTransformIndex / s_transforms.Count, scene.name + " > " + transform.gameObject.PathInSceneHierarchy())) {
                            CleanupSearching();
                            rootProgress.Dispose();
                            sceneProgress.Dispose();
                            throw new Exception("Process aborted");
                        }
                        
                        if (transform.childCount == 0 && transform.gameObject.activeInHierarchy && transform.hideFlags == HideFlags.None) {
                            // find first prefab in parents
                            Transform searchingTransform = transform;
                            do {
                                if (s_visitedObjects.Contains(searchingTransform)) {
                                    break;
                                }
                                
                                foreach (var ignoreName in s_gameObjectNamesToIgnore) {
                                    if (searchingTransform.gameObject.name.StartsWith(ignoreName, StringComparison.OrdinalIgnoreCase)) {
                                        s_visitedObjects.Add(searchingTransform);
                                        goto loopBreak;
                                    }
                                }

                                if (PrefabUtility.IsAnyPrefabInstanceRoot(searchingTransform.gameObject)) {
                                    s_visitedObjects.Add(searchingTransform);
                                    GatherBounds(searchingTransform, ref s_boundsCache);
                                    if (s_boundsCache.extents != Vector3.zero) {
                                        LogIfNoCollisionFound(s_boundsCache, searchingTransform, scene, results);
                                    }

                                    break;
                                }

                                searchingTransform = searchingTransform.parent;
                            } while (searchingTransform != null);
                            
                            loopBreak: ;
                        }
                    }
                }
            }
            CleanupSearching();
        }
        
        static void CleanupSearching() {
            EditorUtility.ClearProgressBar();
            s_rayHits = null;
            s_transforms = null;
            s_visitedObjects = null;
        }

        static void LogIfNoCollisionFound(Bounds bounds, Transform searchingTransform, Scene scene, List<InvalidFindersResultWindow.Result> results) {
            var size = Physics.BoxCastNonAlloc(bounds.center, bounds.extents, Vector3.down, s_rayHits, Quaternion.identity, 0.001f, ~0, QueryTriggerInteraction.Ignore);
            for (int j = 0; j < size; j++) {
                if (s_rayHits[j].collider.transform.IsChildOf(searchingTransform)) {
                    continue;
                }
                // At least one collision found
                return;
            }
            
            GameObject colliderGameObject = searchingTransform.gameObject;
            Log.Important?.Error(
                $"Object <color=blue>{colliderGameObject.name}</color> is floating at <color=blue>{scene.name + "/" + colliderGameObject.PathInSceneHierarchy()}</color>.",
                colliderGameObject,
                LogOption.NoStacktrace
            );
            results.Add(new InvalidFindersResultWindow.Result {
                scene = scene.name,
                gameObject = colliderGameObject
            });
        }

        static void GatherBounds(Transform searchingTransform, ref Bounds bounds) {
            bounds.extents = Vector3.zero;
            Collider[] children = searchingTransform.GetComponentsInChildren<Collider>();
            for (var childIndex = 0; childIndex < children.Length; childIndex++) {
                Collider collider = children[childIndex];
                if (collider.enabled && collider.gameObject.activeInHierarchy && !collider.isTrigger && collider.gameObject.hideFlags == HideFlags.None) {
                    Bounds colliderBounds = collider.bounds;
                    if (bounds.extents == Vector3.zero) {
                        bounds.extents = colliderBounds.extents;
                        bounds.center = colliderBounds.center;
                        continue;
                    }
                    bounds.Encapsulate(colliderBounds);
                }
            }

            if (bounds.extents == Vector3.zero) {
                Component[] regrowables = searchingTransform.GetComponentsInChildren<ComplexRegrowableSpec>();
                Component[] vegetationRegrowables = searchingTransform.GetComponentsInChildren<VegetationRegrowableSpec>();
                Component[] pickables = searchingTransform.GetComponentsInChildren<PickableSpec>();

                foreach (var component in regrowables.Union(vegetationRegrowables).Union(pickables)) {
                    if (bounds.extents == Vector3.zero) {
                        bounds.size = 0.5f * Vector3.one;
                        bounds.center = component.transform.position;
                        continue;
                    }
                    bounds.Encapsulate(new Bounds(component.transform.position, 0.5f * Vector3.one));
                }
            }

            // expand bounds to avoid false positives
            if (bounds.extents != Vector3.zero) {
                bounds.Expand(0.3f);
            }
        }
    }
}