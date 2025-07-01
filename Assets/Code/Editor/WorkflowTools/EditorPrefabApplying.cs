using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Awaken.TG.EditorOnly.WorkflowTools;
using Awaken.TG.Graphics.LODSystem;
using Sirenix.Utilities;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Awaken.TG.Editor.WorkflowTools {
    /// <summary>
    /// Currently unused as the operation takes too much time. the methods here might be useful in the future for other applications.
    /// </summary>
    public static class EditorPrefabApplying {
        static readonly List<Transform> Singletons = new();
        static ProfilerMarker s_addedGo = new("Applying: GameObject Added");
        static ProfilerMarker s_removedGo = new("Applying: GameObject Removed");
        static ProfilerMarker s_addedComponent = new("Applying: Component Added");
        static ProfilerMarker s_removedComponent = new("Applying: Component Removed");
        static ProfilerMarker s_otherModifications = new("Applying: Other Modifications");
        static ProfilerMarker s_otherModification = new("Applying: Other Modification");

        class OverrideSingletonData {
            public int depth;
            public string prefabAssetPath;
            public List<ObjectOverride> overrides;
        }

        public static void ApplyAllRecursively(GameObject obj) {
            // get all top level prefab instances below obj
            var prefabInstances = obj.GetComponentsInChildren<Transform>().Select(t => t.gameObject).Where(PrefabUtility.IsOutermostPrefabInstanceRoot).ToList();

            Dictionary<GameObject, OverrideSingletonData> singletonData = new Dictionary<GameObject, OverrideSingletonData>();

            for (var index = 0; index < prefabInstances.Count; index++) {
                GameObject prefabInstance = prefabInstances[index];
                var overrides = PrefabUtility.GetObjectOverrides(prefabInstance);

                foreach (ObjectOverride oOverride in overrides) {
                    if (EditorUtility.DisplayCancelableProgressBar("Gathering Singleton Changes", $"Gathering changes for {prefabInstance.name}", (float) index / prefabInstances.Count)) {
                        goto Apply;
                    }

                    GameObject singletonGameObject = null;

                    if (oOverride.instanceObject is GameObject go) {
                        singletonGameObject = go.GetComponentInParent<SingletonGameObject>()?.gameObject;
                    } else if (oOverride.instanceObject is Component c) {
                        singletonGameObject = c.GetComponentInParent<SingletonGameObject>()?.gameObject;
                    }

                    if (singletonGameObject == null) {
                        continue;
                    }

                    string prefabAssetPathOfNearestInstanceRoot = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(singletonGameObject);
                    if (prefabAssetPathOfNearestInstanceRoot.IsNullOrWhitespace()) {
                        continue;
                    }

                    if (singletonData.TryGetValue(singletonGameObject, out var data)) {
                        data.overrides.Add(oOverride);
                    } else {
                        singletonData.Add(singletonGameObject, new() {
                            depth = SingletonSavingExecutor.Depth(singletonGameObject.transform, true),
                            overrides = new List<ObjectOverride> {
                                oOverride
                            },
                            prefabAssetPath = prefabAssetPathOfNearestInstanceRoot
                        });
                    }
                }
            }

            Apply:
            EditorUtility.ClearProgressBar();

            var ordered = singletonData.Keys.OrderBy(go => singletonData[go].depth).ToList();

            for (var index = 0; index < ordered.Count; index++) {
                GameObject singleton = ordered[index];
                OverrideSingletonData overrideSingletonData = singletonData[singleton];

                for (var i = 0; i < overrideSingletonData.overrides.Count; i++) {
                    if (EditorUtility.DisplayCancelableProgressBar("Applying Singleton Changes " + index + "/" + ordered.Count, $"Applying changes to {singleton.name}", (float) i / overrideSingletonData.overrides.Count)) {
                        return;
                    }

                    overrideSingletonData.overrides[i].Apply(overrideSingletonData.prefabAssetPath);
                }
            }
        }

        /// <summary>
        /// For being able to cancel invocation of select operations on progress bar cancel
        /// </summary>
        static bool s_canceled = false;

        static void ApplyChangesToSingletons(List<Transform> singletons = null) {
            HashSet<SerializedObject> changedObjects = new HashSet<SerializedObject>(100);
            HashSet<SerializedObject> sharedHashSet = new HashSet<SerializedObject>(100);

            foreach (Transform singleton in singletons ?? Singletons) {
                GameObject singletonGameObject = singleton.gameObject;
                Transform originalSingleton = PrefabUtility.GetCorrespondingObjectFromOriginalSource(singleton);
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(singletonGameObject)) continue;

                string prefabAssetPathOfNearestInstanceRoot = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(singletonGameObject);

                HandleAddedGameObject(singletonGameObject, singleton, prefabAssetPathOfNearestInstanceRoot);
                HandleRemovedGameObject(singletonGameObject, originalSingleton);

                HandleAddedComponent(singletonGameObject, singleton, prefabAssetPathOfNearestInstanceRoot);
                HandleRemovedComponent(singletonGameObject, originalSingleton);

                HandleOtherProperties(singletonGameObject, prefabAssetPathOfNearestInstanceRoot, sharedHashSet, changedObjects);

                if (s_canceled) break;
            }

            ApplyModifiedProperties(changedObjects);
        }

        /// <summary>
        /// Applies modified properties of provided serialized objects
        /// </summary>
        static void ApplyModifiedProperties(HashSet<SerializedObject> changedObjects) {
            SerializedObject[] changedObjectsArray = changedObjects.ToArray();

            for (var index = 0; index < changedObjectsArray.Length; index++) {
                SerializedObject serializedObject = changedObjectsArray[index];
                if (EditorUtility.DisplayCancelableProgressBar("Applying Modified Properties", $"Applying changes to {serializedObject.targetObject.name}", (float) index / changedObjectsArray.Length)) {
                    s_canceled = true;
                    break;
                }

                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// Gathers and groups all property overrides
        /// </summary>
        static void HandleOtherProperties(GameObject singletonGameObject, string prefabAssetPathOfNearestInstanceRoot, HashSet<SerializedObject> sharedHashSet, HashSet<SerializedObject> changedObjects) {
            s_otherModifications.Begin();
            // --- Handle Property Modifications
            Transform[] children = singletonGameObject.GetComponentsInChildren<Transform>(true);

            for (var index = 0; index < children.Length; index++) {
                Transform child = children[index];

                if ((child.hideFlags & HideFlags.DontSaveInEditor) == HideFlags.DontSaveInEditor) continue;
                if (!PrefabUtility.IsPartOfPrefabInstance(child)) continue;

                if (EditorUtility.DisplayCancelableProgressBar("Applying Singleton Changes", $"Applying changes to {child.name}", (float) index / children.Length)) {
                    s_canceled = true;
                    break;
                }

                s_otherModification.Begin(child);

                ApplyPropertyOverrides(child.gameObject, prefabAssetPathOfNearestInstanceRoot, false, sharedHashSet);
                changedObjects.UnionWith(sharedHashSet);
                sharedHashSet.Clear();

                child.GetComponents(s_childComponentsSharedList);
                foreach (Component component in s_childComponentsSharedList) {
                    ApplyPropertyOverrides(component, prefabAssetPathOfNearestInstanceRoot, false, sharedHashSet);
                    changedObjects.UnionWith(sharedHashSet);
                    sharedHashSet.Clear();
                }

                s_otherModification.End();
            }

            EditorUtility.ClearProgressBar();
            s_otherModifications.End();
        }

        static void HandleRemovedComponent(GameObject singletonGameObject, Transform originalSingleton) {
            s_removedComponent.Begin();
            // --- Handle Removed components
            foreach (RemovedComponent removedComponent in PrefabUtility.GetRemovedComponents(singletonGameObject)) {
                if (removedComponent.assetComponent.transform.GetComponentInParent<SingletonGameObject>(true)?.transform == originalSingleton) {
                    PrefabUtility.ApplyRemovedComponent(removedComponent.containingInstanceGameObject, removedComponent.assetComponent, InteractionMode.AutomatedAction);
                }
            }

            s_removedComponent.End();
        }

        static void HandleAddedComponent(GameObject singletonGameObject, Transform singleton, string prefabAssetPathOfNearestInstanceRoot) {
            s_addedComponent.Begin();
            // --- Handle Added components
            foreach (AddedComponent addedComponent in PrefabUtility.GetAddedComponents(singletonGameObject)) {
                if (addedComponent.instanceComponent.transform.GetComponentInParent<SingletonGameObject>(true)?.transform == singleton) {
                    PrefabUtility.ApplyAddedComponent(addedComponent.instanceComponent, prefabAssetPathOfNearestInstanceRoot, InteractionMode.AutomatedAction);
                }
            }

            s_addedComponent.End();
        }

        static void HandleRemovedGameObject(GameObject singletonGameObject, Transform originalSingleton) {
            s_removedGo.Begin();
            foreach (RemovedGameObject removedGameObject in PrefabUtility.GetRemovedGameObjects(singletonGameObject)) {
                if (removedGameObject.assetGameObject.transform.GetComponentInParent<SingletonGameObject>(true).transform == originalSingleton) {
                    PrefabUtility.ApplyRemovedGameObject(removedGameObject.parentOfRemovedGameObjectInInstance, removedGameObject.assetGameObject, InteractionMode.AutomatedAction);
                }
            }

            s_removedGo.End();
        }

        static void HandleAddedGameObject(GameObject singletonGameObject, Transform singleton, string prefabAssetPathOfNearestInstanceRoot) {
            s_addedGo.Begin();
            foreach (AddedGameObject addedGameObject in PrefabUtility.GetAddedGameObjects(singletonGameObject)) {
                if (addedGameObject.instanceGameObject.transform.GetComponentInParent<SingletonGameObject>(true).transform == singleton) {
                    PrefabUtility.ApplyAddedGameObject(addedGameObject.instanceGameObject, prefabAssetPathOfNearestInstanceRoot, InteractionMode.AutomatedAction);
                }
            }

            s_addedGo.End();
        }

        static MethodInfo handleApplySingleProperties = typeof(PrefabUtility).GetMethod("HandleApplySingleProperties", BindingFlags.Static | BindingFlags.NonPublic);
        static MethodInfo saveChangesToPrefabFileIfPersistent = typeof(PrefabUtility).GetMethod("SaveChangesToPrefabFileIfPersistent", BindingFlags.Static | BindingFlags.NonPublic);
        static List<Component> s_childComponentsSharedList = new(16);

        /// <summary>
        /// Taken from PrefabUtility.cs and changed to leave saving till later
        /// </summary>
        static void ApplyPropertyOverrides(
            Object prefabInstanceObject,
            string assetPath,
            bool allowApplyDefaultOverride,
            HashSet<SerializedObject> changedObjects,
            InteractionMode action = InteractionMode.AutomatedAction) {
            Object fromSourceAtPath = PrefabUtility.GetCorrespondingObjectFromSourceAtPath(prefabInstanceObject, assetPath);
            if (fromSourceAtPath == null)
                return;
            SerializedObject prefabSourceSerializedObject = new SerializedObject(fromSourceAtPath);
            List<SerializedObject> serializedObjects = new List<SerializedObject>();

            handleApplySingleProperties.Invoke(null, new object[] {prefabInstanceObject, null, assetPath, fromSourceAtPath, prefabSourceSerializedObject, serializedObjects, changedObjects, allowApplyDefaultOverride, action});
        }
    }
}