using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Awaken.TG.Editor.Prefabs {
    public class PrefabReplacer : OdinEditorWindow {
        [SerializeField] GameObject prefabToReplace;
        [SerializeField] GameObject newPrefab;
        [SerializeField, BoxGroup("Transform")] Vector3 positionOffset;
        [SerializeField, BoxGroup("Transform")] Vector3 rotationOffset;
        [SerializeField, BoxGroup("Transform")] Vector3 scaleOffset;
        [SerializeField] List<SceneAsset> scenes = new();

        [MenuItem("TG/Assets/Prefabs/Prefab Replacer")]
        public static void ShowWindow() {
            GetWindow<PrefabReplacer>("Prefab Replacer").Show();
        }

        [Button]
        void ReplacePrefabs() {
            if (prefabToReplace == null || newPrefab == null || !scenes.Any()) {
                EditorUtility.DisplayDialog("Error", "Please assign all fields and add at least one scene.", "OK");
                return;
            }
            foreach (var sceneAsset in scenes) {
                if (sceneAsset == null) {
                    continue;
                }
                var scenePath = AssetDatabase.GetAssetPath(sceneAsset);
                var scene = EditorSceneManager.OpenScene(scenePath);
                
                AssetDatabase.StartAssetEditing();
                try {
                    bool sceneNeedSaving = false;
                    foreach (var rootObject in scene.GetRootGameObjects()) {
                        ProcessGameObject(rootObject.transform, out var dirty);
                        sceneNeedSaving |= dirty;
                    }
                    if (sceneNeedSaving) {
                        EditorSceneManager.SaveScene(scene);
                    }
                } finally {
                    AssetDatabase.StopAssetEditing();
                }
            }
        }

        void ProcessGameObject(Transform root, out bool dirty) {
            var transformsToReplace = new List<Transform>();
            var transformsToFindNested = new List<Transform>();
            CollectPrefabInstances(root.transform, transformsToReplace, transformsToFindNested);

            dirty = transformsToReplace.Count > 0;
            foreach (var transform in transformsToReplace) {
                ReplacePrefabInstance(transform);
            }

            foreach (var transform in transformsToFindNested) {
                var outerPrefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(transform);
                var outerPrefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(outerPrefabPath);
                if (outerPrefabAsset != null) {
                    var editablePrefab = PrefabUtility.LoadPrefabContents(outerPrefabPath);
                    ProcessGameObject(editablePrefab.transform, out bool prefabDirty);
                    if (prefabDirty) {
                        PrefabUtility.SaveAsPrefabAsset(editablePrefab, outerPrefabPath);
                    }

                    PrefabUtility.UnloadPrefabContents(editablePrefab);
                }
            }
        }

        void CollectPrefabInstances(Transform transform, List<Transform> toReplace, List<Transform> toFindNested) {
            var prefabType = PrefabUtility.GetPrefabAssetType(transform.gameObject);
            if (prefabType is PrefabAssetType.Regular or PrefabAssetType.Variant) {
                var prefab = PrefabUtility.GetCorrespondingObjectFromSource(transform.gameObject);
                if (prefab == prefabToReplace) {
                    toReplace.Add(transform);
                } else {
                    toFindNested.Add(transform);
                }
            } else {
                foreach (Transform child in transform) {
                    CollectPrefabInstances(child, toReplace, toFindNested);
                }
            }
        }

        void ReplacePrefabInstance(Transform oldTransform) {
            var newPrefabInstance = (GameObject) PrefabUtility.InstantiatePrefab(newPrefab, oldTransform.parent);

            var newTransform = newPrefabInstance.transform;
            var newPosition = oldTransform.localToWorldMatrix.MultiplyPoint(positionOffset);
            var newRotation = oldTransform.rotation * Quaternion.Euler(rotationOffset);
            newTransform.SetPositionAndRotation(newPosition, newRotation);
            newTransform.localScale = Vector3.Scale(oldTransform.localScale, scaleOffset);

            Object.DestroyImmediate(oldTransform.gameObject);
        }
    }
}