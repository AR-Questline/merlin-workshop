using System;
using System.Linq;
using Awaken.Utility.GameObjects;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Utility {
    public static class PrefabUtil {
        public static void SavePrefabInstanceChangesAndDestroy(GameObject prefabInstance, GameObject target) {
            PrefabUtility.ApplyPrefabInstance(prefabInstance, InteractionMode.UserAction);
            EditorUtility.SetDirty(prefabInstance);
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            Object.DestroyImmediate(prefabInstance);
        }

        public static GameObject GetPrefabInstance(GameObject target) {
            GameObject prefab;
            var assetPath = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (assetPath == null) {
                prefab = target.gameObject;
            } else {
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath.assetPath);
            }

            return (GameObject) PrefabUtility.InstantiatePrefab(prefab);
        }
        
        public static bool IsInPrefabStage(GameObject target, UnityEditor.SceneManagement.PrefabStage currentPrefabStage = null) {
            if (currentPrefabStage == null) {
                currentPrefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            }
            return currentPrefabStage?.prefabContentsRoot == target;
        }
        
        public static void EditInPrefab(GameObject gob, Action<GameObject> action) {
            EditInPrefab(gob, go => {
                action(go);
                return true;
            });
        }
        
        public static void EditInPrefab(GameObject gob, Func<GameObject, bool> action) {
            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gob);
            GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);

            GameObject nearestPrefabInstance = PrefabUtility.GetNearestPrefabInstanceRoot(gob);
            GameObject targetObject;
            if (nearestPrefabInstance == gob) {
                targetObject = prefab;
            } else {
                string rootPath = nearestPrefabInstance.PathInSceneHierarchy();
                rootPath = rootPath.Replace(nearestPrefabInstance.name, "");
                string path = gob.PathInSceneHierarchy().Replace(rootPath, "");

                int siblingsIndex = gob.transform.GetSiblingIndex();

                targetObject = prefab.GetComponentsInChildren<Transform>()
                    .FirstOrDefault(t => path == t.gameObject.PathInSceneHierarchy() && siblingsIndex == t.GetSiblingIndex())?
                    .gameObject;
            }

            if (targetObject != null) {
                if (action(targetObject)) {
                    PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
                }
            }
        }

        public static void VariantToRegularPrefab(GameObject prefabAsset) {
            string prefabPath = AssetDatabase.GetAssetPath(prefabAsset);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (!PrefabUtility.IsPartOfPrefabAsset(prefab) || PrefabUtility.GetPrefabAssetType(prefab) != PrefabAssetType.Variant) {
                return;
            }
            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.OutermostRoot, InteractionMode.UserAction);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.OutermostRoot, InteractionMode.UserAction);
            AssetDatabase.DeleteAsset(prefabPath);
            PrefabUtility.SaveAsPrefabAssetAndConnect(instance, prefabPath, InteractionMode.UserAction);
            Object.DestroyImmediate(instance);
        }
    }
}
