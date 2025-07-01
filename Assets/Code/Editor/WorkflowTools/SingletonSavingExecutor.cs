using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Awaken.TG.EditorOnly;
using Awaken.TG.EditorOnly.WorkflowTools;
using Awaken.Utility.Collections;
using Awaken.Utility.Editor.Scenes;
using Awaken.Utility.GameObjects;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.WorkflowTools {
    [Preserve]
    public class SceneSingletonGameObjectRemoval : SceneProcessor {
        public override int callbackOrder => 100;
        public override bool canProcessSceneInIsolation => true;
        protected override void OnProcessScene(Scene scene, bool processingInPlaymode) {
            var singletons = GameObjects.FindComponentsByTypeInScene<SingletonGameObject>(scene, true);
            foreach (SingletonGameObject singleton in singletons) {
                Object.DestroyImmediate(singleton);
            }
        }
    }

    [InitializeOnLoad]
    public static class SingletonSavingExecutor {
        static readonly List<Transform> Singletons = new();
        static ProfilerMarker s_totalMarker = new("SingletonSavingExecutor");

        static SingletonSavingExecutor() {
            EditorSceneManager.sceneSaving += BeforeSceneSave;
            PrefabStage.prefabSaving += BeforePrefabSave;
        }

        static void BeforePrefabSave(GameObject obj) {
            EnvironmentSetup();
            try {
                Singletons.Clear();
                Singletons.AddRange(obj.GetComponentsInChildren<SingletonGameObject>(true)
                                       .Select(s => s.transform)
                                       .Where(s => PrefabUtility.IsOutermostPrefabInstanceRoot(s.gameObject)));
                ApplyInstancesWithProgressBar();
                
                Singletons.Clear();
            } finally {
                CleanupEnvironmentSetup();
            }
        }

        static void BeforeSceneSave(Scene scene, string path) {
            EnvironmentSetup();
            try {
                Singletons.Clear();
                Singletons.AddRange(GameObjects.FindComponentsByTypeInScene<SingletonGameObject>(scene, true)
                                          .Select(s => s.transform)
                                          .Where(s => PrefabUtility.IsOutermostPrefabInstanceRoot(s.gameObject)));
                ApplyInstancesWithProgressBar();

                Singletons.Clear();
            } finally {
                CleanupEnvironmentSetup();
            }
        }

        
        // === Sub methods for callbacks
        static void EnvironmentSetup() {
            Profiler.enabled = true;
            s_totalMarker.Begin();
            AssetDatabase.StartAssetEditing();
            AssetDatabase.DisallowAutoRefresh();
        }
        
        static void ApplyInstancesWithProgressBar() {
            for (var index = 0; index < Singletons.Count; index++) {
                Transform transform = Singletons[index];
                EditorUtility.DisplayProgressBar("Applying all changes to top level Singletons", $"Applying changes for {transform.name}", (float) index / Singletons.Count);
                if (PrefabUtility.HasPrefabInstanceAnyOverrides(transform.gameObject, false)) {
                    PrefabUtility.ApplyPrefabInstance(transform.gameObject, InteractionMode.AutomatedAction);
                    EditorUtility.SetDirty(transform.gameObject);
                }
            }
        }
        
        static void CleanupEnvironmentSetup() {
            s_totalMarker.End();
            EditorUtility.ClearProgressBar();
            AssetDatabase.StopAssetEditing();
            AssetDatabase.AllowAutoRefresh();
            DelayedProfilerDisable();
        }
        
        static async void DelayedProfilerDisable() {
            await Task.Delay(10);
            Profiler.enabled = false;
        }

        /// <summary>
        /// Expensive
        /// </summary>
        [MenuItem("TG/Scene Tools/Apply All Singletons")]
        public static void Singletons_ApplyAll() {
            s_totalMarker.Begin();
            Singletons.Clear();
            Singletons.AddRange(Object.FindObjectsByType<SingletonGameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                                      .Select(s => s.transform));
            Singletons.Sort((a, b) => Depth(a, true).CompareTo(Depth(b, true)));
            
            List<Transform> outermost = new List<Transform>(Singletons.Where(s => PrefabUtility.IsOutermostPrefabInstanceRoot(s.gameObject)));
            foreach (Transform transform in outermost) {
                PrefabUtility.ApplyPrefabInstance(transform.gameObject, InteractionMode.AutomatedAction);
                EditorUtility.SetDirty(transform.gameObject);
            }

            for (var index = 0; index < Singletons.Count; index++) {
                Transform transform = Singletons[index];

                var singletonGameObject = transform.GetComponent<SingletonGameObject>();
                EditorPrefabHelpers.DoForComponentInAllDepthInPrefabScope(singletonGameObject, contentsScope => {
                    List<Transform> internalSingletons = new();
                    internalSingletons.AddRange(contentsScope.prefabContentsRoot.GetComponentsInChildren<SingletonGameObject>(true)
                                                             .Select(s => s.transform).WhereNotNull().Where(PrefabUtility.IsPartOfPrefabInstance));
                    internalSingletons.Sort((a, b) => Depth(a, true).CompareTo(Depth(b, true)));
                    foreach (Transform foundSingleton in internalSingletons) {
                        if (EditorUtility.DisplayCancelableProgressBar("Fully Applying Overrides to Singletons", $"Applying changes for {transform.name}", (float) index / Singletons.Count)) {
                            return true;
                        }
                        PrefabUtility.ApplyPrefabInstance(foundSingleton.gameObject, InteractionMode.AutomatedAction);
                        EditorUtility.SetDirty(foundSingleton.gameObject);
                    }

                    return false;
                });
            }

            Singletons.Clear();
            s_totalMarker.End();
        }
        
        // === Helpers

        public static int Depth(Transform root, bool inverse = false) {
            int depth = 0;
            while (root.parent != null) {
                root = root.parent;
                depth += 1;
            }
            return inverse ? -depth : depth;
        }
    }
}