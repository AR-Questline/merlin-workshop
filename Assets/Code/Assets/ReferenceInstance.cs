using System;
using Awaken.Utility.Debugging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Assets {
    [Serializable]
    public abstract class ReferenceInstance {
        /// <summary>
        /// Reference to asset
        /// </summary>
        public ARAssetReference Reference { get; }
        /// <summary>
        /// Reference to loaded asset
        /// </summary>
        public Object GenericInstance { get; set; }

        protected T As<T>() where T : Object => (T) GenericInstance;

        protected ReferenceInstance(ARAssetReference reference) {
            Reference = reference;
        }
    }
    
    /// <summary>
    /// Asset reference state container
    /// </summary>
    [Serializable]
    public class ReferenceInstance<T> : ReferenceInstance where T : Object {
        /// <summary>
        /// GenericInstance but casted to type T
        /// </summary>
        public T Instance => As<T>();

        public ReferenceInstance(ARAssetReference reference) : base(reference) {}
    }
    
    public static class ReferenceInstanceExtensions{
        public static void ReleaseInstance(this ReferenceInstance<GameObject> referenceInstance) {
            if (referenceInstance.Instance != null) {
                Object.Destroy(referenceInstance.Instance);
            }
            referenceInstance.Reference.ReleaseAsset();
        }

        public static ARAsyncOperationHandle<GameObject> Instantiate(this ReferenceInstance<GameObject> instance, Transform parent, Action<GameObject> onLoaded = null, Action onCancelled = null) {
            return instance.Instantiate(parent, null, onLoaded, onCancelled);
        }
        
        public static ARAsyncOperationHandle<GameObject> Instantiate(this ReferenceInstance<GameObject> instance, Transform parent, Vector3 position, Quaternion rotation, 
            Action<GameObject> onLoaded = null, Action onCancelled = null) {
            return instance.Instantiate(parent, (position, rotation), onLoaded, onCancelled);
        }
        
        static ARAsyncOperationHandle<GameObject> Instantiate(this ReferenceInstance<GameObject> instance, Transform parent, (Vector3, Quaternion)? positionAndRotationOverride = null, 
            Action<GameObject> onLoaded = null, Action onCancelled = null) {
            var handle = instance.Reference.LoadAsset<GameObject>();
            handle.OnComplete(h => instance.InstanceLoaded(h, parent, onLoaded, positionAndRotationOverride), _ => onCancelled?.Invoke());
            return handle;
        }

        static void InstanceLoaded(this ReferenceInstance<GameObject> instance, ARAsyncOperationHandle<GameObject> handle, Transform parent = null, Action<GameObject> onLoaded = null, (Vector3, Quaternion)? positionAndRotationOverride = null) {
            if (handle.Result == null) {
                Log.Important?.Error("Result of instantiation was null. Asset reference: " + instance.Reference.Address + " Cancelled: " + 
                                     handle.IsCancelled + " Status: " + handle.Status + " Result: " + handle.Result);
            } else {
                instance.GenericInstance = positionAndRotationOverride.HasValue
                    ? Object.Instantiate(handle.Result, positionAndRotationOverride.Value.Item1, positionAndRotationOverride.Value.Item2, parent)
                    : Object.Instantiate(handle.Result, parent);
            }

            onLoaded?.Invoke(instance.Instance);
        }
    }
}