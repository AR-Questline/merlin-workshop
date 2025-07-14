using Awaken.Utility;
using System;
using Awaken.TG.Main.Saving;
using Awaken.TG.Utility.Attributes;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Awaken.TG.Assets {
    /// <summary>
    /// Producer of <see cref="ARAssetReference"/>, used to passing asset reference to multiple places.
    /// </summary>
    /// <remarks>
    /// Only final consumer should use <see cref="Get()"/>, other users should pass reference via this producer.
    /// </remarks>
    [Serializable]
    public sealed partial class ShareableARAssetReference {
        public ushort TypeForSerialization => SavedTypes.ShareableARAssetReference;

        // === Properties
        /// <summary>
        /// Check if is valid sprite reference
        /// </summary>
        public bool IsSet => !string.IsNullOrWhiteSpace(AssetGUID);
        
        public string AssetGUID => arReference?.Address;
        public string SubObject => arReference?.SubObjectName;
        public string RuntimeKey => arReference?.RuntimeKey;
        
        // === Editor references
        [SerializeField][Saved] ARAssetReference arReference;

        // === Constructors
        public ShareableARAssetReference() { }
        public ShareableARAssetReference(ARAssetReference arReference) {
            this.arReference = arReference;
        }

        public ShareableARAssetReference(string guid) {
            arReference = new ARAssetReference(guid);
        }

        // === Operation
        /// <summary>
        /// Returns new handle to asset reference
        /// </summary>
        /// <remarks>
        /// Every load or preload operation on handle must be eventually completed with release ON THE SAME HANDLE.
        /// </remarks>
        public ARAssetReference Get() {
            if (!IsSet) return null;
            return arReference.DeepCopy();
        }
        /// <summary>
        /// Returns new handle to asset reference and loads asset (calls onCompleted when completed).
        /// </summary>
        public ARAssetReference GetAndLoad<T>(Action<ARAsyncOperationHandle<T>> onCompleted) where T : class {
            var reference = Get();
            reference?.LoadAsset<T>().OnComplete(onCompleted);
            return reference;
        }
        
        // === Preloading
        public AsyncOperationHandle<T> PreloadLight<T>() where T : class {
            return Addressables.LoadAssetAsync<T>(RuntimeKey);
        }
        
        public void ReleasePreloadLight<T>(AsyncOperationHandle<T> preloadHandle) where T : class {
            preloadHandle.Release();
        }

        bool Equals(ShareableARAssetReference other) {
            return Equals(arReference, other.arReference);
        }

        public override bool Equals(object obj) {
            return ReferenceEquals(this, obj) || (obj is ShareableARAssetReference other && Equals(other));
        }

        public override int GetHashCode() {
            return (arReference != null ? arReference.GetHashCode() : 0);
        }

        public static SerializationAccessor Serialization(ShareableARAssetReference instance) => new(instance);
        public struct SerializationAccessor {
            readonly ShareableARAssetReference _instance;
            
            public SerializationAccessor(ShareableARAssetReference instance) {
                _instance = instance;
            }

            public ref ARAssetReference ARReference => ref _instance.arReference;
        }
        
        #if UNITY_EDITOR
        public struct EDITOR_Accessor {
            readonly ShareableARAssetReference _instance;
            
            public EDITOR_Accessor(ShareableARAssetReference instance) {
                _instance = instance;
            }

            public ref ARAssetReference ARReference => ref _instance.arReference;
        } 
        #endif
    }
}