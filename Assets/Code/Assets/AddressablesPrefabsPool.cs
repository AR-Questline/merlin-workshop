using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Assets {
    public class AddressablesPrefabsPool {
        Dictionary<string, float> _lastTimeUsed = new();
        Dictionary<string, List<AddressablesPooledInstance>> _addressablesPool = new();
        
        public void RemoveOldReferences(bool forceClean = false) {
            for (int i = _lastTimeUsed.Count - 1; i >= 0; i--) {
                (string assetRuntimeKey, float lastTimeUsed) = _lastTimeUsed.ElementAt(i);
                if (lastTimeUsed + PrefabPool.RemovingOldReferencesInterval < Time.realtimeSinceStartup || forceClean) {
                    _lastTimeUsed.Remove(assetRuntimeKey);
                    foreach (var instance in _addressablesPool[assetRuntimeKey]) {
                        instance.Release();
                    }
                    _addressablesPool.Remove(assetRuntimeKey);
                }
            }

            if (forceClean) {
                _lastTimeUsed.Clear();
                _addressablesPool.Clear();
            }
        }
        
        AddressablesPooledInstance InternalInstantiate(ShareableARAssetReference shareableARAssetReference, CancellationToken cancellationToken) {
            string assetRuntimeKey = shareableARAssetReference.RuntimeKey;
            _lastTimeUsed[assetRuntimeKey] = Time.realtimeSinceStartup;
            if(!_addressablesPool.TryGetValue(assetRuntimeKey, out var list)) {
                list = new List<AddressablesPooledInstance>();
                _addressablesPool.Add(assetRuntimeKey, list);
            }

            AddressablesPooledInstance instance;
            if (list.Count == 0) {
                instance = new AddressablesPooledInstance(shareableARAssetReference, cancellationToken);
            } else {
                instance = list[^1];
                list.RemoveAt(list.Count - 1);
            }
            _lastTimeUsed[assetRuntimeKey] = Time.realtimeSinceStartup;

            return instance;
        }
        
        public async UniTask<IPooledInstance> Instantiate(ShareableARAssetReference arAssetReference, Vector3 position, Quaternion rotation, Transform parent, Vector3? overrideLocalScale, CancellationToken cancellationToken, bool automaticallyActivate) {
            // TODO: Manage cancelled instantiation better
            var instance = InternalInstantiate(arAssetReference, cancellationToken);
            await UniTask.WaitWhile(() => !instance.InstanceLoaded, cancellationToken: cancellationToken).SuppressCancellationThrow();
            if (instance.Instance == null || cancellationToken.IsCancellationRequested) {
                instance.Return();
                return null;
            }
            return PrefabPool.ConfigureInstance(instance, parent, position, rotation, overrideLocalScale: overrideLocalScale, automaticallyActivate: automaticallyActivate);
        }
        
        public void Return(AddressablesPooledInstance pooledInstance, Transform root) {
            if (_addressablesPool.TryGetValue(pooledInstance.ShareAbleAssetReference.RuntimeKey, out var list)) {
                list.Add(pooledInstance);
                if (pooledInstance.Instance != null) {
                    pooledInstance.Instance.transform.SetParent(root.transform);
                    pooledInstance.Instance.SetActive(false);
                }
            } else {
                pooledInstance.Release();
            }
        }
    }
}