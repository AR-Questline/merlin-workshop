using System;
using Awaken.TG.Utility;
using Awaken.Utility.GameObjects;
using AwesomeTechnologies;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Helpers {

    public static class ChecklistItemFunctions {
        // === Helpers

        public static Func<GameObject, T> FindChildWith<T>() where T : Component {
            return (root) => root.GetComponentInChildren<T>();
        }

        public static Func<GameObject, T> FindChildWithName<T>(string name) where T : Component {
            return (root) => root.transform.TryGrabChild<T>(name);
        }

        public static Func<GameObject, T> CreateByAddingPrefab<T>(string prefabAssetPath) where T : Component {
            return (root) => {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabAssetPath);
                if (prefab == null) {
                    throw new InvalidOperationException($"No prefab at path: {prefabAssetPath}.");
                }
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
                instance.transform.localPosition = Vector3.zero;
                T component = instance.GetComponent<T>();
                if (component == null) {
                    throw new InvalidOperationException($"The prefab {prefabAssetPath} does not have a {typeof(T)} component.");
                }
                return component;
            };
        }

        public static Func<GameObject, Transform> CreateEmptyObject(string objectName) {
            return root => {
                GameObject host = new GameObject(objectName);
                host.transform.parent = root.transform;
                host.transform.localPosition = Vector3.zero;
                return host.transform;
            };
        }

        public static Func<GameObject, T> CreateObjectWithBehaviour<T>(string objectName)
            where T : Component {
            return root => {
                GameObject host = CreateEmptyObject(objectName)(root).gameObject;
                T component = host.AddComponent<T>();
                return component;
            };
        }

        public static Func<GameObject, T> CreateColliderBasedOnPrefab<T>(string objectName = "Collider") where T : Collider {
            return root => {
                T collider = CreateObjectWithBehaviour<T>(objectName)(root);
                // Bounds bounds = MeshUtils.CalculateBounds(root);
                // TransformBoundsUtil.AdjustColliderByBounds<T>(collider, bounds, 0.8f);
                return collider;
            };
        }
    }
}