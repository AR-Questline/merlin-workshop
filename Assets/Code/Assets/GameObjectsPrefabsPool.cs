using System.Collections.Generic;
using UnityEngine;

namespace Awaken.TG.Assets {
    public class GameObjectsPrefabsPool {
        readonly Dictionary<GameObject, List<GameObject>> _pool = new();
        
        public void RemoveOldReferences(bool forceClean = false) {
            if (forceClean) {
                foreach (List<GameObject> instances in _pool.Values) {
                    foreach (GameObject instance in instances) {
                        Object.Destroy(instance);
                    }
                }
                _pool.Clear();
            }
        }
        
        GameObjectPooledInstance InternalInstantiate(GameObject prefab) {
            if (!_pool.TryGetValue(prefab, out var list)) {
                list = new List<GameObject>();
                _pool.Add(prefab, list);
            }

            GameObject instance;
            if (list.Count == 0) {
                instance = Object.Instantiate(prefab);
            } else {
                instance = list[^1];
                list.RemoveAt(list.Count - 1);
            }

            return new GameObjectPooledInstance(prefab, instance);
        }
        
        public IPooledInstance Instantiate(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null, Vector3? overrideLocalScale = null) {
            var instance = InternalInstantiate(prefab);
            return PrefabPool.ConfigureInstance(instance, parent, position, rotation: rotation, overrideLocalScale: overrideLocalScale);
        }
        
        [UnityEngine.Scripting.Preserve]
        public IPooledInstance Instantiate(GameObject prefab, Vector3 position, Vector3 forward, Transform parent = null, Vector3? overrideLocalScale = null) {
            var instance = InternalInstantiate(prefab);
            return PrefabPool.ConfigureInstance(instance, parent, position, Quaternion.LookRotation(forward), overrideLocalScale: overrideLocalScale);
        }
        
        public void Return(GameObject prefab, GameObject instance, Transform root) {
            if (_pool.TryGetValue(prefab, out var list)) {
                list.Add(instance);
                instance.transform.SetParent(root.transform);
                instance.SetActive(false);
            } else {
                Object.Destroy(instance);
            }
        }
    }
}