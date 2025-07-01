using System;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Awaken.TG.Assets {
    public readonly struct ARAsyncOperationHandle : IDisposable, IEquatable<ARAsyncOperationHandle> {
        readonly AsyncOperationHandle _backingHandle;
        readonly CompletionTypeLess _completion;
        
        public AsyncOperationStatus Status => _backingHandle.Status;
        public bool IsDone => _backingHandle.IsDone;
        public bool IsValid() => _backingHandle.IsValid();
        public bool IsCancelled => _completion?.IsCancelled ?? false;

        internal ARAsyncOperationHandle(AsyncOperationHandle handle, CompletionTypeLess completionTypeLess) {
            _backingHandle = handle;
            _completion = completionTypeLess;
        }

        public ARAsyncOperationHandle<T> Convert<T>() {
            return new(_backingHandle.Convert<T>(), _completion);
        }
        
        public UniTask ToTypelessUniTask() {
            return Status switch {
                AsyncOperationStatus.Failed => UniTask.CompletedTask,
                AsyncOperationStatus.Succeeded => UniTask.CompletedTask,
                AsyncOperationStatus.None => Complete(this),
                _ => UniTask.CompletedTask
            };
            
            static UniTask Complete(ARAsyncOperationHandle handle) {
                return UniTask.WaitUntil(() => handle.IsDone || handle.IsCancelled);
            }
        }
        
        public void Release() {
            if (_backingHandle.IsValid()) {
                if (_backingHandle.IsDone) {
                    if (_completion == null) {
                        Addressables.Release(_backingHandle);
                    } else {
                        _completion?.OnReleaseWhenDone();
                    }
                } else {
                    _completion?.Cancel();
                }
            }
        }

        void IDisposable.Dispose() {
            Release();
        }

        // === Equality members
        public bool Equals(ARAsyncOperationHandle other) {
            return _backingHandle.Equals(other._backingHandle);
        }

        public override bool Equals(object obj) {
            return obj is ARAsyncOperationHandle other && Equals(other);
        }

        public override int GetHashCode() {
            return _backingHandle.GetHashCode();
        }

        internal class CompletionTypeLess {
            protected bool _cancelled;
            public bool IsCancelled => _cancelled;
            
            public void Cancel() {
                _cancelled = true;
            }
            
            public virtual void OnReleaseWhenDone() { }
        }
    }

    /// <summary>
    /// AsyncOperationHandle has multiple issues with Complete and Task so this wrapper aims to fix them
    /// </summary>
    /// <typeparam name="T">Asset type</typeparam>
    public readonly struct ARAsyncOperationHandle<T> : IDisposable, IEquatable<ARAsyncOperationHandle<T>> {
        readonly AsyncOperationHandle<T> _backingHandle;
        readonly Completion _completion;

        public ARAsyncOperationHandle(AsyncOperationHandle<T> handle) : this(handle, true) { }
        internal ARAsyncOperationHandle(AsyncOperationHandle<T> handle, ARAsyncOperationHandle.CompletionTypeLess completionTypeLess) {
            _backingHandle = handle;
            _completion = (Completion)completionTypeLess;
        }
        ARAsyncOperationHandle(AsyncOperationHandle<T> handle, bool withCompletion) {
            _backingHandle = handle;
            if (withCompletion) {
                _completion = new(_backingHandle);
            } else {
                _completion = null;
            }
        }

        // === Queries
        public AsyncOperationStatus Status => _backingHandle.Status;
        public T Result => _backingHandle.Result;
        public bool IsDone => _backingHandle.IsDone;
        public bool IsCancelled => _completion?.IsCancelled ?? false;
        
        public UniTask<T> ToUniTask() {
            return Status switch {
                AsyncOperationStatus.Failed => UniTask.FromResult(default(T)),
                AsyncOperationStatus.Succeeded => UniTask.FromResult(Result),
                AsyncOperationStatus.None => Complete(this),
                _ => UniTask.FromResult(default(T))
            };
            
            static async UniTask<T> Complete(ARAsyncOperationHandle<T> handle) {
                await UniTask.WaitUntil(() => handle.IsDone || handle.IsCancelled);
                return !handle.IsCancelled && handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded 
                    ? handle.Result 
                    : default;
            }
        }
        public bool IsValid() => _backingHandle.IsValid();

        // === Operations
        public ARAsyncOperationHandle TypeLess() {
            return new(_backingHandle, _completion);
        }
        
        public void OnComplete(Action<ARAsyncOperationHandle<T>> callback, Action<ARAsyncOperationHandle<T>> onCancelled = null) {
            if (IsDone) {
                callback(this);
            } else {
                _completion?.AddCallback(callback, onCancelled);
            }
        }
        
        public void OnCompleteForceAsync(Action<ARAsyncOperationHandle<T>> callback, Action<ARAsyncOperationHandle<T>> onCancelled = null) {
            // From Addressables AsyncOperationHandle.Completed
            // If this is assigned on a completed operation,// the callback is deferred until the LateUpdate of the current frame.
            _completion?.AddAsyncCallback(callback, onCancelled);
        }

        public T WaitForCompletion() {
            return _backingHandle.WaitForCompletion();
        }
        
        [UnityEngine.Scripting.Preserve]
        public void BindTo(Model model) {
            model.BindDisposable(this);
        }

        public void Release() {
            if (_backingHandle.IsValid()) {
                if (_backingHandle.IsDone) {
                    Addressables.Release(_backingHandle);
                } else {
                    _completion?.Cancel();
                }
            }
        }

        void IDisposable.Dispose() {
            Release();
        }

        // === Equality members
        public bool Equals(ARAsyncOperationHandle<T> other) {
            return _backingHandle.Equals(other._backingHandle);
        }

        public override bool Equals(object obj) {
            return obj is ARAsyncOperationHandle<T> other && Equals(other);
        }

        public override int GetHashCode() {
            return _backingHandle.GetHashCode();
        }
        
        public static implicit operator ARAsyncOperationHandle(ARAsyncOperationHandle<T> handle) {
            return handle.TypeLess();
        }
        
        public static explicit operator ARAsyncOperationHandle<T>(ARAsyncOperationHandle handle) {
            return handle.Convert<T>();
        }

        class Completion : ARAsyncOperationHandle.CompletionTypeLess {
            readonly AsyncOperationHandle<T> _backingHandle;
            Action<ARAsyncOperationHandle<T>> _onComplete;
            Action<ARAsyncOperationHandle<T>> _onCancelled;
            public bool IsCancelled => _cancelled;

            public Completion(AsyncOperationHandle<T> backingHandle) {
                _backingHandle = backingHandle;
                _backingHandle.Completed += this.Complete;
            }
            
            public override void OnReleaseWhenDone() {
                _cancelled = true;
                if (_backingHandle.IsValid()) {
                    _backingHandle.Completed -= this.Complete;
                    this.Complete(_backingHandle);
                }
                _onComplete = null;
                _onCancelled = null;
            }

            public void AddCallback(Action<ARAsyncOperationHandle<T>> callback, Action<ARAsyncOperationHandle<T>> onCancelled) {
                if (_cancelled) {
                    onCancelled?.Invoke(new(_backingHandle, false));
                    return;
                }

                _onComplete += callback;
                _onCancelled += onCancelled;
            }

            public void AddAsyncCallback(Action<ARAsyncOperationHandle<T>> callback, Action<ARAsyncOperationHandle<T>> onCancelled) {
                if (_cancelled) {
                    onCancelled?.Invoke(new(_backingHandle, false));
                    return;
                }

                _backingHandle.Completed -= this.Complete;
                _backingHandle.Completed += this.Complete;

                _onComplete += callback;
                _onCancelled += onCancelled;
            }

            void Complete(AsyncOperationHandle<T> handle) {
                var arHandle = new ARAsyncOperationHandle<T>(handle, false);
                if (_cancelled) {
                    arHandle.Release();
                    _onCancelled?.Invoke(arHandle);
                } else {
                    _onComplete?.Invoke(arHandle);
                }

                _onComplete = null;
                _onCancelled = null;
            }
        }
    }

    public static class ARAsyncOperationUtils {
        [UnityEngine.Scripting.Preserve]
        public static UniTask<T>.Awaiter GetAwaiter<T>(this in ARAsyncOperationHandle<T> handle) {
            return handle.ToUniTask().GetAwaiter();
        }
        
        [UnityEngine.Scripting.Preserve]
        public static UniTask.Awaiter GetAwaiter(this in ARAsyncOperationHandle handle) {
            return handle.ToTypelessUniTask().GetAwaiter();
        }
    }
}
