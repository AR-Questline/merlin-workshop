using System;
using System.Collections.Generic;
using Awaken.Utility.Collections;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Awaken.TG.MVC.Domains {
    /// <summary>
    /// Wrapper for AsyncOperationHandle to encapsulate all scene-related differences in addressable loading 
    /// </summary>
    public class SceneLoadOperation : ISceneLoadOperation {
        public readonly AsyncOperationHandle<SceneInstance> handle;
        Action _callbacks;
        string _sceneName;
        bool _callbackCalled;
        bool _isUnload;

        public SceneLoadOperation(string sceneName, AsyncOperationHandle<SceneInstance> h, bool isUnload) {
            this._sceneName = sceneName;
            _isUnload = isUnload;
            handle = h;
        }

        public string Name => handle.DebugName;
        public bool IsDone => handle.IsDone && (_isUnload || IsInitialized);
        public float Progress => IsDone ? 1f : handle.PercentComplete;
        public bool IsInitialized { get; private set; }
        public IEnumerable<string> MainScenesNames => _sceneName.Yield();
        public void OnComplete(Action callback) {
            if (IsDone || _callbackCalled) {
                callback();
            } else {
                _callbacks += callback;
            }
        }

        public void Complete() {
            _callbackCalled = true;
            _callbacks?.Invoke();
        }

        public void Initialize() {
            IsInitialized = true;
        }
    }
}