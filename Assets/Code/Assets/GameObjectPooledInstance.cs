using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Assets {
    public struct GameObjectPooledInstance : IPooledInstance {
        public GameObject Instance { get; private set; }
        public bool InstanceLoaded => true;
        public GameObject Prefab { get; private set; }

        public GameObjectPooledInstance(GameObject prefab, GameObject instance) {
            Instance = instance;
            Prefab = prefab;
        }

        public void Return() {
            if (Instance != null) {
                PrefabPool prefabPool = World.Services.TryGet<PrefabPool>();
                if (prefabPool != null) {
                    prefabPool.Return(Prefab, Instance);
                } else {
                    Object.Destroy(Instance);
                }
            }

            Prefab = null;
            Instance = null;
        }
        
        public void Release() {
            if (Instance != null) {
                Object.Destroy(Instance);
            }
            
            Prefab = null;
            Instance = null;
        }

        public void Invalidate() {
            if (Instance != null) {
                PrefabPool prefabPool = World.Services.TryGet<PrefabPool>();
                if (prefabPool != null) {
                    prefabPool.Return(Prefab, Instance);
                } else {
                    Object.Destroy(Instance);
                }
            }

            Prefab = null;
            Instance = null;
        }
    }
}