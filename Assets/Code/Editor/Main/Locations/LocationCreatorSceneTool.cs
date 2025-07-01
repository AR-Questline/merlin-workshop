using Awaken.TG.Editor.Assets;
using Awaken.TG.Editor.Utility.Paths;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Locations.Setup;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.GameObjects;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Locations {
    [InitializeOnLoad]
    public static class LocationCreatorSceneTool {

        static LocationCreatorSceneTool() {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        static void OnSceneGUI(SceneView sceneView) {
            Event e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.N) {
                if (PrefabStageUtility.GetCurrentPrefabStage() != null) {
                    EditorUtility.DisplayDialog("Problem", "You need to quit Prefab Mode to create new locations on scene", "OK");
                    return;
                }
                Vector3 projectedPoint = ProjectedPoint(e.mousePosition);
                Transform parent = Object.FindObjectsByType<MapGridGizmos>(FindObjectsSortMode.None).MinBy(g => Vector3.SqrMagnitude(projectedPoint - g.transform.position), true)?.transform;
                if (parent == null) {
                    EditorUtility.DisplayDialog("Problem", "This tool only works on scene with MapGridGizmos", "OK");
                    return;
                }
                DisplayParentDialogue(parent, projectedPoint);
                e.Use();
            }
        }

        static void SpawnNewLocation(Transform parent, Vector3 position) {
            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(parent.gameObject);
            if (string.IsNullOrWhiteSpace(prefabPath)) {
                EditorUtility.DisplayDialog("Problem", "Couldn't find a valid prefab to attach LocationSpec to", "OK");
                return;
            }

            // Create or choose Location Spec prefab
            GameObject prefab;
            if (EditorUtility.DisplayDialog("Choose Prefab Mode", "Do you want to use existing LocationSpec prefab or create new one?", "Create new", "Use existing")) {
                prefab = TemplateCreation.CreatePrefab(n => GameObjects.WithSingleBehavior<LocationSpec>(name: n), select: false, defaultDirectory: "Assets/Data/LocationSpecs");
            } else {
                string path = EditorUtility.OpenFilePanel("Choose Location Spec", "Assets/Data/LocationSpecs", "prefab");
                if (string.IsNullOrEmpty(path)) {
                    return;
                }
                path = PathUtils.FilesystemToAssetPath(path);
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }

            if (prefab == null) return;

            // Instantiate the LocationSpec prefab in correct on-scene prefab
            GameObject instance = (GameObject) PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = prefab.name;
            instance.transform.position = position;
            PrefabUtility.ApplyPrefabInstance(PrefabUtility.GetNearestPrefabInstanceRoot(parent.gameObject), InteractionMode.AutomatedAction);

            GameObject spawnedOnScene = parent.Find(prefab.name)?.gameObject;
            Selection.activeObject = spawnedOnScene;
        }

        static void DisplayParentDialogue(Transform parent, Vector3 position) {
            GenericMenu menu = new GenericMenu();
            string prefix = parent.parent.gameObject.PathInSceneHierarchy() + "/";

            foreach (var transform in parent.GetComponentsInChildren<Transform>()) {
                if (transform.GetComponentInParent<LocationSpec>() == null) {
                    string name = transform.gameObject.PathInSceneHierarchy().Replace(prefix, "");
                    menu.AddItem(new GUIContent(name), false, () => {
                        SpawnNewLocation(transform, position);
                    });
                }
            }

            if (menu.GetItemCount() == 1) {
                SpawnNewLocation(parent, position);
            } else {
                // show the menu
                menu.ShowAsContext();
            }
        }

        static Vector3 ProjectedPoint(Vector2 mousePosition) {
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            // align to ground if possible
            bool hit = Ground.Raycast(ray, out Vector3 pos, out _);
            if (hit) return pos;

            // no ground or no ground hit, place at y=0 plane
            Plane zeroPlane = new Plane(Vector3.up, Vector3.zero);
            zeroPlane.Raycast(ray, out float distance);
            return ray.GetPoint(distance);
        }
    }
}