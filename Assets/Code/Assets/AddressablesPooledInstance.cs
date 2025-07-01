using System.Threading;
using Awaken.CommonInterfaces;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Assets {
    public class AddressablesPooledInstance : IPooledInstance {
        public GameObject Instance { get; private set; }
        public bool InstanceLoaded => Instance != null || _completed;
        public ARAssetReference AssetReference { get; private set; }
        public ShareableARAssetReference ShareAbleAssetReference { get; }
        CancellationTokenSource _cancellationToken;
        bool _completed;

        public AddressablesPooledInstance(ShareableARAssetReference shareableARAssetReference, CancellationToken cancellationToken) {
            ShareAbleAssetReference = shareableARAssetReference;
            AssetReference = shareableARAssetReference.Get();
            Instantiate(cancellationToken).Forget();
        }
        
        async UniTaskVoid Instantiate(CancellationToken cancellationToken) {
            _cancellationToken = new CancellationTokenSource();
            _completed = false;
            GameObject loadedAsset = await AssetReference.LoadAsset<GameObject>().ToUniTask();
            if (cancellationToken.IsCancellationRequested || _cancellationToken.IsCancellationRequested || loadedAsset == null) {
                _completed = true;
                AssetReference.ReleaseAsset();
                return;
            }
            Instance = PrefabUtil.InstantiateDisabled(loadedAsset);
            Instance.SetUnityRepresentation(new IWithUnityRepresentation.Options() {
                linkedLifetime = true,
                movable = true,
            });
            await UniTask.DelayFrame(1);
            if (cancellationToken.IsCancellationRequested || _cancellationToken.IsCancellationRequested) {
                _completed = true;
                Object.Destroy(Instance);
                AssetReference.ReleaseAsset();
                return;
            }
            _completed = true;
        }
        
        public void Return() {
            PrefabPool prefabPool = World.Services.TryGet<PrefabPool>();
            if (prefabPool != null && Instance != null) {
                prefabPool.Return(this);
            } else {
                Release();
            }
        }

        public void Release() {
            _cancellationToken?.Cancel();
            _cancellationToken = null;
            
            if (Instance != null) {
                Object.Destroy(Instance);
                Instance = null;
            }
            
            AssetReference?.ReleaseAsset();
            AssetReference = null;
        }
        
        public void Invalidate() {
            if (Instance != null) {
                PrefabPool prefabPool = World.Services.TryGet<PrefabPool>();
                if (prefabPool != null) {
                    prefabPool.Return(this);
                } else {
                    Object.Destroy(Instance);
                }
            }
            
            Instance = null;
        }
    }
}