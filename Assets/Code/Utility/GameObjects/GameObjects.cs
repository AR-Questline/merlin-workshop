using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Awaken.Utility.Collections;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Awaken.Utility.GameObjects {
    public static class GameObjects {
        static readonly List<GameObject> RootsBuffer = new List<GameObject>();

        /// <summary>
        /// Creates a new empty GameObject housing a single MonoBehaviour.
        /// </summary>
        /// <typeparam name="T">the behaviour to add</typeparam>
        /// <returns>the behaviour component, already bound to a game object</returns>
        public static T WithSingleBehavior<T>(Transform parent = null, string name = null) where T : MonoBehaviour {
            GameObject go = new GameObject();
            go.name = name ?? typeof(T).Name;
            go.AddComponent<T>();
            if (parent != null) {
                go.transform.SetParent(parent, false);
            }

            return go.GetComponent<T>();
        }

        public static GameObject Empty(string name, Transform parent = null) {
            GameObject go = new GameObject();
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, worldPositionStays: false);
            return go;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameObject Empty(string name, Scene scene) {
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            return go;
        }

        public static GameObject FromResource(string resourcePath, Transform parent = null) {
            GameObject prefab = Resources.Load<GameObject>(resourcePath);
            GameObject instance = UnityEngine.Object.Instantiate(prefab, parent: parent);
            return instance;
        }

        public static T FromResource<T>(string resourcePath, Transform parent = null) where T : Component {
            GameObject prefab = Resources.Load<GameObject>(resourcePath);
            GameObject instance = UnityEngine.Object.Instantiate(prefab, parent: parent);
            return instance.GetComponent<T>();
        }

        public static T FromPrefab<T>(GameObject prefab, Transform parent = null) where T : Component {
            GameObject go = UnityEngine.Object.Instantiate(prefab, parent);
            return go.GetComponent<T>();
        }

        public static T GrabChild<T>(GameObject gob, params string[] path) where T : Component {
            Transform current = gob.transform;
            foreach (string element in path) {
                Transform child = current.Find(element);
                if (child == null) {
                    throw new ArgumentException($"Game object {gob.name} has no child called '{element}'.");
                }

                current = child;
            }

            T component = current.GetComponent<T>();
            if (component == null) {
                throw new ArgumentException($"'{current.name} does not have a '{typeof(T).Name}' component.");
            }

            return component;
        }

        /// <summary>
        /// Grabs a child at the specified path and gets a component from it.
        /// Returns null if any game object in the path is missing or if there is no
        /// component of specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="gob">game object to look in</param>
        /// <param name="path">path to look in, game object names</param>
        /// <returns>the component, or null</returns>
        public static T TryGrabChild<T>(GameObject gob, params string[] path) where T : Component {
            Transform current = gob.transform;
            foreach (string element in path) {
                Transform child = current.Find(element);
                if (child == null) {
                    return null;
                }

                current = child;
            }

            T component = current.GetComponent<T>();
            return component;
        }

        /// <summary>
        /// Finds a component placed in GameObject with given name.
        /// </summary>
        public static T FindRecursively<T>(this GameObject tree, string name) where T : Component {
            var components = tree.transform.GetComponentsInChildren<T>();
            foreach (T component in components) {
                if (component.gameObject.name == name) {
                    return component;
                }
            }

            return null;
        }

        public static Transform FindChildRecursively(this GameObject tree, string name, bool includeDisabled = false) {
            if (!includeDisabled) {
                return FindRecursively<Transform>(tree, name);
            }

            if (tree.name == name) {
                return tree.transform;
            }

            foreach (Transform t in tree.transform) {
                var child = FindChildRecursively(t.gameObject, name, true);
                if (child != null) {
                    return child.transform;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds a GameObject found by predicate.
        /// </summary>
        public static GameObject FindRecursively(this GameObject tree, Func<GameObject, bool> predicate) {
            if (predicate(tree)) {
                return tree;
            } else {
                GameObject result = null;
                foreach (Transform child in tree.transform) {
                    result = FindRecursively(child.gameObject, predicate);
                    if (result != null) break;
                }

                return result;
            }
        }

        /// <summary>
        /// Search for a component in parents
        /// </summary>
        /// <param name="searchStart">the Transform to start searching the component from</param>
        /// <param name="depth">should the search stop after X parents searched (defaults to full upward hierarchy search)</param>
        /// <typeparam name="T">The component to search for</typeparam>
        /// <returns>Found component or null</returns>
        public static T FindInAnyParent<T>(Transform searchStart, int depth = -1) where T : Component {
            int loops = 0;

            while (depth < 0 || loops <= depth) {
                loops++;
                if (searchStart == null) return null;
                if (searchStart.TryGetComponent(out T component)) return component;
                searchStart = searchStart.parent;
            }

            return null;
        }

        public static T FindInAnyParent<T>(GameObject searchStart, int depth = -1) where T : Component => FindInAnyParent<T>(searchStart.transform, depth);

        /// <summary>
        /// Destroys the object using the right method regardless of whether
        /// we are in play mode or edit mode.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DestroySafely(UnityEngine.Object objToDestroy, bool allowDestroyingAssets = false) {
#if UNITY_EDITOR
            if (Application.isPlaying) {
                UnityEngine.Object.Destroy(objToDestroy);
            } else {
                UnityEngine.Object.DestroyImmediate(objToDestroy, allowDestroyingAssets);
            }
#else
            UnityEngine.Object.Destroy(objToDestroy);
#endif
        }

        public static void DestroyAllChildrenSafely(Transform parent, params GameObject[] omitted) {
            List<GameObject> toDestroy = new List<GameObject>(parent.childCount);
            foreach (Transform child in parent) {
                if (!omitted.Contains(child.gameObject)) {
                    toDestroy.Add(child.gameObject);
                }
            }

            foreach (GameObject go in toDestroy) {
                DestroySafely(go);
            }
        }

        public static void SetStaticRecursively(GameObject go, bool isStatic) {
#if UNITY_EDITOR
            go.isStatic = isStatic;

            foreach (Transform child in go.transform) {
                SetStaticRecursively(child.gameObject, isStatic);
            }
#endif
        }

        public static List<T> FindComponentsByTypeInScene<T>(Scene scene, bool findInactive, int size = 10) {
            var results = new List<T>(size);
            FindComponentsByTypeInScene(scene, findInactive, ref results);
            return results;
        }

        public static List<T> FindComponentsByTypeInScenes<T>(IReadOnlyList<Scene> scenes, bool findInactive, int size = 10) {
            ListPool<T>.Get(out List<T> results);
            results.EnsureCapacity(size);
            FindComponentsByTypeInScenes(scenes, findInactive, ref results);
            return results;
        }

        public static void FindComponentsByTypeInScenes<T>(IReadOnlyList<Scene> scenes, bool findInactive, ref List<T> results) {
            foreach (var scene in scenes) {
                FindComponentsByTypeInScene<T>(scene, findInactive, ref results);
            }
        }

        public static void FindComponentsByTypeInScene<T>(Scene scene, bool findInactive, ref List<T> results) {
#if DEBUG
            var isComponent = typeof(T).IsSubclassOf(typeof(Component));
            var isInterface = typeof(T).IsInterface;
            if (!isComponent && !isInterface) {
                throw new ArgumentException($"Type {typeof(T).Name} is neither a component nor an interface.");
            }
#endif
            RootsBuffer.EnsureCapacity(scene.rootCount);
            using var buffer = new ReusableListBuffer<GameObject>(RootsBuffer);
            scene.GetRootGameObjects(buffer);
            var temp = new List<T>();

            foreach (GameObject root in buffer) {
                if (findInactive || root.activeSelf) {
                    root.GetComponentsInChildren(findInactive, temp);
                    results.AddRange(temp);
                    temp.Clear();
                }
            }
        }

        public static T FindComponentByTypeInScene<T>(Scene scene, bool findInactive) {
#if DEBUG
            var isComponent = typeof(T).IsSubclassOf(typeof(Component));
            var isInterface = typeof(T).IsInterface;
            if (!isComponent && !isInterface) {
                throw new ArgumentException($"Type {typeof(T).Name} is neither a component nor an interface.");
            }
#endif
            RootsBuffer.EnsureCapacity(scene.rootCount);
            using var buffer = new ReusableListBuffer<GameObject>(RootsBuffer);
            scene.GetRootGameObjects(buffer);

            foreach (GameObject root in buffer) {
                if (findInactive || root.activeSelf) {
                    var child = root.GetComponentInChildren<T>(findInactive);
                    if (child != null) {
                        return child;
                    }
                }
            }

            return default;
        }

        static readonly Regex PathRegex = new(@"(?>\/)?(?>[^$]+)\$(\d+)\$", RegexOptions.Compiled);

        public static GameObject GetGameObject(string pathWithSiblingIndices, Scene? scene = null) {
            if (string.IsNullOrWhiteSpace(pathWithSiblingIndices)) {
                return null;
            }

            GameObject result = null;
            scene ??= SceneManager.GetActiveScene();

            var pathParts = PathRegex.Matches(pathWithSiblingIndices);

            if (pathParts.Count == 0) {
                return null;
            }

            if (!int.TryParse(pathParts[0].Groups[1].Value, out var rootIndex)) {
                return null;
            }

            var roots = scene.Value.GetRootGameObjects();

            if (rootIndex >= roots.Length) {
                return null;
            }

            var root = roots[rootIndex];
            Transform current = root.transform;

            for (var i = 1; i < pathParts.Count; i++) {
                string childIndexString = pathParts[i].Groups[1].Value;
                current = TryGetChild(current, childIndexString);
            }

            result = current != null ? current.gameObject : null;

            return result;
        }

        static Transform TryGetChild(Transform transform, string indexString) {
            if (transform == null || !int.TryParse(indexString, out int index)) {
                return null;
            }

            return transform.childCount > index ? transform.GetChild(index) : null;
        }

        public static List<Transform> BreadthFirst(Transform root) {
            var queue = new Queue<Transform>();
            var results = new List<Transform>();

            queue.Enqueue(root);
            while (queue.Count > 0) {
                var current = queue.Dequeue();
                results.Add(current);
                foreach (Transform child in current) {
                    queue.Enqueue(child);
                }
            }

            return results;
        }
    }

    public static class GameObjectExtensions {
        /// <summary>
        /// Sets the active state of the game object if it is not already in that state.
        /// </summary>
        public static void SetActiveOptimized(this GameObject go, bool active) {
            if (go.activeSelf != active) {
                go.SetActive(active);
            }
        }
        
        /// <summary>
        /// Sets the active state of the game object if it is not null.
        /// </summary>
        public static void TrySetActiveOptimized(this GameObject go, bool active) {
            if (go == null) return;
            go.SetActiveOptimized(active);
        }

        /// <summary>
        /// Sets the active state of the component's game object if it is not null.
        /// </summary>
        public static void TrySetActiveOptimized(this Component mb, bool active) {
            if (mb == null || mb.gameObject == null) return;
            mb.gameObject.SetActiveOptimized(active);
        }

        public static T GrabChild<T>(this Component mb, params string[] path) where T : Component {
            return GameObjects.GrabChild<T>(mb.gameObject, path);
        }

        public static T TryGrabChild<T>(this Component mb, params string[] path) where T : Component {
            return GameObjects.TryGrabChild<T>(mb.gameObject, path);
        }

        public static bool TryGetComponentInParent<T>(this Component mb, out T component) where T : Component {
            component = mb.GetComponentInParent<T>();
            return component is not null;
        }

        public static bool HasAnyComponentInParent(this GameObject go, IEnumerable<Type> componentsToCheck) {
            if (componentsToCheck == null) {
                return false;
            }

            foreach (var componentType in componentsToCheck) {
                var component = go.GetComponentInParent(componentType, true);
                if (component is not null) {
                    return true;
                }
            }

            return false;
        }

        public static T GetComponentInParentOnlyAbove<T>(this Component mb) where T : Component {
            var parent = mb.transform.parent;
            return parent == null ? default : parent.GetComponentInParent<T>();
        }

        public static GameObject LastChild(this Component mb) {
            Transform transform = mb.transform;
            var childCount = transform.childCount;
            return childCount > 0 ? transform.GetChild(childCount - 1).gameObject : null;
        }

        public static string PathInSceneHierarchy(this GameObject obj, bool withSiblingIndex = false) {
            StringBuilder builder = new();
            builder.Append(ObjectName(obj, withSiblingIndex));
            while (obj.transform.parent != null) {
                obj = obj.transform.parent.gameObject;
                string objName = ObjectName(obj, withSiblingIndex);
                builder.Insert(0, "/");
                builder.Insert(0, objName);
            }

            return builder.ToString();
        }
        
        /// <summary>
        /// <see cref="PathInSceneHierarchy"/> with scene name
        /// </summary>
        public static string HierarchyPath(this GameObject obj, bool withSiblingIndex = false) {
            StringBuilder builder = new();
            builder.Append(ObjectName(obj, withSiblingIndex));
            while (obj.transform.parent != null) {
                obj = obj.transform.parent.gameObject;
                string objName = ObjectName(obj, withSiblingIndex);
                builder.Insert(0, "/");
                builder.Insert(0, objName);
            }
            builder.Insert(0, ":");
            builder.Insert(0, obj.scene.name);
            return builder.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static string ObjectName(GameObject obj, bool withSiblingIndex = false) {
            if (withSiblingIndex) {
                if (obj.transform.parent != null) {
                    return $"{obj.name}${obj.transform.GetSiblingIndex()}$";
                } else {
                    var scene = obj.scene;
                    if (scene.IsValid()) {
                        var rootObjects = scene.GetRootGameObjects();
                        var index = rootObjects.IndexOf(obj);
                        return $"{obj.name}${index}$";
                    } else {
                        return obj.name;
                    }
                }
            } else {
                return obj.name;
            }
        }

        public static void SetLayerRecursively(this Component mb, int layer, params int[] exceptLayers) {
            GameObject obj = mb.gameObject;
            if (!(exceptLayers?.Contains(obj.layer) ?? false)) {
                obj.layer = layer;
            }

            foreach (Transform child in obj.transform) {
                SetLayerRecursively(child, layer, exceptLayers);
            }
        }

        public static Transform FindChildWithTagRecursively(this GameObject tree, string tag, bool includeDisabled = false) {
            if (!includeDisabled) {
                return FindRecursivelyByTag<Transform>(tree, tag);
            }

            if (tree.CompareTag(tag)) {
                return tree.transform;
            }

            foreach (Transform t in tree.transform) {
                var child = FindChildWithTagRecursively(t.gameObject, tag, true);
                if (child != null) {
                    return child.transform;
                }
            }

            return null;
        }

        static T FindRecursivelyByTag<T>(this GameObject tree, string tag) where T : Component {
            var components = tree.transform.GetComponentsInChildren<T>();
            foreach (T component in components) {
                if (component.CompareTag(tag)) {
                    return component;
                }
            }

            return null;
        }
        
        public static Transform FindChildWithTagOrNameRecursively(this GameObject tree, string name, string tag, bool includeDisabled = false) {
            if (!includeDisabled) {
                return FindRecursivelyByTagOrName<Transform>(tree, name, tag);
            }

            if (tree.name == name || tree.CompareTag(tag)) {
                return tree.transform;
            }

            foreach (Transform t in tree.transform) {
                var child = FindChildWithTagOrNameRecursively(t.gameObject, name, tag, true);
                if (child != null) {
                    return child.transform;
                }
            }

            return null;
        }
        
        static T FindRecursivelyByTagOrName<T>(this GameObject tree, string name, string tag) where T : Component {
            foreach (Transform t in tree.transform) {
                if (t.TryGetComponent(out T component)) {
                    if (component.CompareTag(tag) || component.name == name) {
                        return component;
                    }
                }
            }
            
            return null;
        }

        public static TComponent AddChildWith<TComponent>(this Transform transform, string name) where TComponent : Component {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
#if UNITY_EDITOR
            GameObjectUtility.EnsureUniqueNameForSibling(go);
#endif
            return go.AddComponent<TComponent>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetComponentInChildren<T>(this Component c, out T component) where T : Component {
            component = c.GetComponentInChildren<T>();
            return component != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetComponentInChildren<T>(this GameObject g, out T component) where T : Component {
            component = g.GetComponentInChildren<T>();
            return component != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetComponentInChildren<T>(this GameObject g, bool includeInactive, out T component) where T : Component {
            component = g.GetComponentInChildren<T>(includeInactive);
            return component != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameObject WithComponent<T>(this GameObject go, out T component) where T : Component {
            component = go.AddComponent<T>();
            return go;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameObject WithComponent<T>(this GameObject go) where T : Component => go.WithComponent<T>(out _);

        public static bool HasComponent<T>(this GameObject go) => go.TryGetComponent(out T _);

        public static T GetOrAddComponent<T>(this GameObject go) where T : Component {
            T component = go.GetComponent<T>();
            if (component == null) {
                // For some reason unity can return a non null component that is invalid
                component = go.AddComponent<T>();
            }

            return component;
        }

        public static void CopyLocalPositionAndRotationFrom(this Transform transform, Transform source) {
            source.GetLocalPositionAndRotation(out var position, out var rotation);
            transform.SetLocalPositionAndRotation(position, rotation);
        }

        public static void CopyPositionAndRotationFrom(this Transform transform, Transform source) {
            source.GetPositionAndRotation(out var position, out var rotation);
            transform.SetPositionAndRotation(position, rotation);
        }

        public static int GetDepthInHierarchy(this Transform transform) {
            int depth = 0;
            Transform current = transform;
            while (current.parent != null) {
                depth++;
                current = current.parent;
            }

            return depth;
        }

        public static bool IsEmptyLeaf(this Transform transform) {
            return transform.childCount == 0 && transform.gameObject.GetComponentCount() == 1;
        }

        public static bool IsLeafSingleComponent(this Transform transform) {
            return transform.childCount == 0 && transform.gameObject.GetComponentCount() == 2;
        }
    }
}