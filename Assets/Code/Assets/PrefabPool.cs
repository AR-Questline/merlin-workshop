using System.Threading;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Assets {
    public class PrefabPool : IDomainBoundService {
        public const int RemovingOldReferencesInterval = 30;
        public const float DefaultVFXLifeTime = 5f;
        const int RemovingOldReferencesIntervalMilliseconds = RemovingOldReferencesInterval * 1000;
        public Domain Domain => Domain.Gameplay;
        public bool RemoveOnDomainChange() {
            _poolCancellationSource?.Cancel();
            _poolCancellationSource = null;
            RemoveOldReferences(true);
            _addressablesPrefabsPool = null;
            _gameObjectsPrefabsPool = null;
            return true;
        }

        readonly GameObject _root;
        AddressablesPrefabsPool _addressablesPrefabsPool;
        GameObjectsPrefabsPool _gameObjectsPrefabsPool;
        CancellationTokenSource _poolCancellationSource;
        bool HasBeenDestroyed => _root == null || (_poolCancellationSource?.IsCancellationRequested ?? true);

        public PrefabPool() {
            _root = new GameObject("EffectsPool");
            SceneManager.MoveGameObjectToScene(_root, World.Services.Get<ViewHosting>().DefaultHost().gameObject.scene);
            _addressablesPrefabsPool = new AddressablesPrefabsPool();
            _gameObjectsPrefabsPool = new GameObjectsPrefabsPool();
            _poolCancellationSource = new CancellationTokenSource();
        }
        
        public void Init() {
            CheckForRemovingOldReferences().Forget();
            WaitForDestroy().Forget();
        }

        async UniTaskVoid CheckForRemovingOldReferences() {
            while (!HasBeenDestroyed) {
                RemoveOldReferences();
                await UniTask.Delay(RemovingOldReferencesIntervalMilliseconds, true);
            }
        }

        // --- On Exiting PlayMode (and possibly other situations) we are in state where root is being destroyed but comparing it to null returns false.
        // --- So we need this await to know that root is being destroyed.
        async UniTaskVoid WaitForDestroy() {
            await _root.OnDestroyAsync();
            _poolCancellationSource?.Cancel();
        }

        public void RemoveOldReferences(bool forceClean = false) {
            _addressablesPrefabsPool?.RemoveOldReferences(forceClean);
            _gameObjectsPrefabsPool?.RemoveOldReferences(forceClean);
        }

        public static IPooledInstance Instantiate(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null, Vector3? overrideLocalScale = null) {
            return World.Services.TryGet<PrefabPool>()?._gameObjectsPrefabsPool.Instantiate(prefab, position, rotation, parent, overrideLocalScale);
        }

        public static async UniTask<IPooledInstance> Instantiate(ShareableARAssetReference arAssetReference, Vector3 position, Quaternion rotation,
            Transform parent = null, Vector3? overrideLocalScale = null, CancellationToken cancellationToken = default, bool automaticallyActivate = true) {
            PrefabPool prefabPool = World.Services.TryGet<PrefabPool>();
            if (prefabPool == null) {
                return null;
            }

            if (cancellationToken != default) {
                CancellationTokenSource linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, prefabPool._poolCancellationSource.Token);
                cancellationToken = linkedTokenSource.Token;
            } else {
                cancellationToken = prefabPool._poolCancellationSource.Token;
            }
            
            IPooledInstance result = await prefabPool._addressablesPrefabsPool.Instantiate(arAssetReference, position, rotation, parent, overrideLocalScale, cancellationToken, automaticallyActivate);
            return result;
        }
        
        public static async UniTask<IPooledInstance> InstantiateAndReturn(ShareableARAssetReference arAssetReference, Vector3 position, Quaternion rotation,
            float returnAfter = DefaultVFXLifeTime, Transform parent = null, Vector3? overrideLocalScale = null, CancellationToken cancellationToken = default, bool automaticallyActivate = true) {
            PrefabPool prefabPool = World.Services.TryGet<PrefabPool>();
            if (prefabPool == null) {
                return null;
            }
            IPooledInstance result = await prefabPool._addressablesPrefabsPool.Instantiate(arAssetReference, position, rotation, parent, overrideLocalScale, cancellationToken, automaticallyActivate);
            result?.Return(returnAfter).Forget();
            return result;
        }

        public static IPooledInstance ConfigureInstance(IPooledInstance instance, Transform parent, Vector3 position, Quaternion rotation, Vector3? overrideLocalScale = null, bool automaticallyActivate = true) {
            Transform t = instance.Instance.transform;
            t.SetParent(parent);
            if (parent == null) {
                var sceneRef = World.Services.Get<SceneService>().ActiveSceneRef;
                var loadedScene = sceneRef.LoadedScene;
                if (!loadedScene.isLoaded) {
                    Log.Important?.Error($"PrefabPool: trying to move instantiated object ({instance.Instance.name}) to unloaded scene: {sceneRef.Name}");
                    instance.Invalidate();
                    return instance;
                }
                SceneManager.MoveGameObjectToScene(t.gameObject, loadedScene);
            }
            t.localPosition = position;
            t.localRotation = rotation;
            if (overrideLocalScale != null) {
                t.localScale = overrideLocalScale.Value;
            }

            if (automaticallyActivate) {
                instance.Instance.SetActive(true);
            }
            return instance;
        }

        public void Return(GameObject prefab, GameObject instance) {
            if (HasBeenDestroyed) {
                if (instance != null) {
                    Object.Destroy(instance);
                }
                return;
            }
            
            _gameObjectsPrefabsPool.Return(prefab, instance, _root.transform);
        }
        
        public void Return(AddressablesPooledInstance pooledInstance) {
            if (HasBeenDestroyed) {
                pooledInstance?.Release();
                return;
            }
            
            _addressablesPrefabsPool.Return(pooledInstance, _root.transform);
        }
    }
}