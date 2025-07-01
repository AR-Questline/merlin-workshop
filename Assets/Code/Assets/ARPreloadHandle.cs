using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Awaken.TG.Assets {
    public class ARPreloadHandle<T> where T : class {
        CancellationTokenSource _cancellationTokenSource;
        ARAssetReference _assetReference;
        ARAsyncOperationHandle<T> _handle;
        
        public bool IsValid => _handle.IsValid();
        public UniTask<T> PreloadTask => CreateTask(_handle);

        // === Constructors
        
        public ARPreloadHandle(ARAssetReference assetReference) {
            _assetReference = assetReference;
        }

        public void Init() {
            _handle = _assetReference.LoadAsset<T>();
        }

        public void Init(float timeout, Func<bool> shouldExtendTimeout) {
            Init();
            ProceedTimeout(timeout, shouldExtendTimeout);
        }

        /// <summary>
        /// The same as create instance with <see cref="Init()"/> and immediate obtain <see cref="PreloadTask"/>
        /// without storing preload handle
        /// </summary>
        /// <param name="assetReference"></param>
        /// <returns></returns>
        public static UniTask<T> JustTask(ARAssetReference assetReference) {
            var handle = assetReference.LoadAsset<T>();
            return CreateTask(handle);
        }

        // === Release
        
        public void Release() {
            if (IsValid) {
                _handle.Release();
                _handle = default;
            }
            _cancellationTokenSource?.Cancel();
        }

        // === Timeout
        
        void ProceedTimeout(float timeout, Func<bool> shouldExtendTimeout = null) {
            shouldExtendTimeout ??= () => false;
            
            _cancellationTokenSource = new CancellationTokenSource();

            WaitAndReturn(timeout, shouldExtendTimeout, _cancellationTokenSource.Token).Forget();
        }

        async UniTaskVoid WaitAndReturn(float timeout, Func<bool> shouldExtendTimeout, CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested && IsValid && shouldExtendTimeout()) {
                await UniTask.Delay( TimeSpan.FromSeconds(timeout), true, cancellationToken: cancellationToken).SuppressCancellationThrow();
            }
            Release();
        }

        // === Task
        
        static UniTask<T> CreateTask(ARAsyncOperationHandle<T> handle) {
            if (!handle.IsValid()) {
                return UniTask.FromResult<T>(null);
            }

            if (handle.IsDone) {
                return UniTask.FromResult(handle.Result);
            }

            return UniTask.WaitUntil(() => handle.IsValid() && handle.IsDone).ContinueWith(() => UniTask.FromResult(handle.IsValid() ? null : handle.Result));
        }
    }
}