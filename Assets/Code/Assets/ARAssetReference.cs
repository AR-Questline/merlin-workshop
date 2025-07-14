using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Awaken.TG.Assets.Modding;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Assets;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UnityEngine.ResourceManagement.ResourceLocations;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.Util;
#endif

namespace Awaken.TG.Assets {
    /// <summary>
    /// AssetReference working with mod system.
    /// </summary>
    [Serializable]
    public sealed partial class ARAssetReference : IEquatable<ARAssetReference> {
        public ushort TypeForSerialization => SavedTypes.ARAssetReference;

        // === State

        [Saved, SerializeField] string address;
        [Saved, SerializeField] string subObjectName;
        ARAsyncOperationHandle _handle;

        // === Properties

        /// <summary>
        /// Address of main asset.
        /// If you refers to sprite in sprite sheet, you get sprite sheet address
        /// </summary>
        public string Address {
            get => address;
            private set => address = value;
        }

        /// <summary>
        /// Name of sub-object in main asset.
        /// For example sprite in sprite sheet.
        /// </summary>
        public string SubObjectName {
            get => subObjectName;
            private set => subObjectName = value;
        }
        
        /// <summary>
        /// Checks if there is any value set to asset reference
        /// </summary>
        public bool IsSet => !string.IsNullOrWhiteSpace(Address);

        public string RuntimeKey {
            get {
                if (string.IsNullOrEmpty(_runtimeKey)) {
                    _runtimeKey = string.IsNullOrEmpty(SubObjectName) ? Address ?? string.Empty : $"{Address}[{SubObjectName}]";
                }
                return _runtimeKey;
            }
        }
        string _runtimeKey;

        // === Constructors

        public ARAssetReference() { }
        
        public ARAssetReference(string address) {
            Address = address;
        }

        public ARAssetReference(string address, string subObjectName) : this(address) {
            SubObjectName = subObjectName;
        }
        
        // === Copy

        public ARAssetReference DeepCopy() {
            return new(Address, SubObjectName);
        }
        
        public ShareableARAssetReference AsShareable() {
            return new ShareableARAssetReference(this);
        }
        
        // === Assets

        /// <summary>
        /// Asynchronously load asset.
        /// You can yield, await or register callback to do required action when asset will be available
        /// </summary>
        /// <typeparam name="T">Type of asset, you can also use array or IList type</typeparam>
        /// <returns>Handle to load operation</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ARAsyncOperationHandle<T> LoadAsset<T>() where T : class {
#if UNITY_EDITOR
            if (Application.isPlaying) {
                return LoadFromAddressables<T>();
            } else {
                return new ARAsyncOperationHandle<T>(ResourceManager.StartOperation(new AsyncEditorLoad<T>(Address, SubObjectName), new AsyncOperationHandle()));
            }
#else
            return LoadFromAddressables<T>();
#endif
        }

        ARAsyncOperationHandle<T> LoadFromAddressables<T>() {
            // TODO: FIND OUT WHY HANDLE IS NOT NULL YET IT IS INVALID, DID SOMEONE UNLOADED ASSET WITHOUT USING OUR RELEASE?
            // UP: It's either that ARAssetReference is shared which is invalid, or one entity calls a few times Load without Release
            bool isHandleValid = _handle.IsValid();
            if (!isHandleValid) {
                _handle = new ARAsyncOperationHandle<T>(LoadFromLocation<T>());
            } else {
#if UNITY_EDITOR
                Log.Debug?.Warning($"Trying to use already used ARReference: {RuntimeKey}");
#endif
            }
            return _handle.Convert<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        AsyncOperationHandle<T> LoadFromLocation<T>() {
            return Addressables.LoadAssetAsync<T>(RuntimeKey);
        }

        /// <summary>
        /// Decrease internal asset usage counter.
        /// If usage counter is zero, asset will be removed from memory
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleaseAsset() {
#if UNITY_EDITOR
            if (Application.isPlaying) {
                ReleaseFromAddressables();
            }
#else
            ReleaseFromAddressables();
#endif
        }

        void ReleaseFromAddressables() {
            if (_handle.IsValid()) {
                _handle.Release();
            }
            _handle = default;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IResourceLocation Location<T>() {
            var location = ModService.GetAddressableLocation(RuntimeKey, typeof(T));
            ValidateLocation<T>(location);
            return location;
        }
        
        // === Preloading
        public AsyncOperationHandle<T> PreloadLight<T>() where T : class {
            return LoadFromLocation<T>();
        }

        public async UniTaskVoid Preload<T>(Func<bool> shouldExtendTimeout) where T : class {
            var preload = PreloadLight<T>();
            if (!preload.IsValid()) {
                return;
            }
            
            while (shouldExtendTimeout()) {
                await UniTask.Delay( TimeSpan.FromSeconds(1), true);
            }
            
            ReleasePreloadLight(preload);
        }

        public void ReleasePreloadLight<T>(AsyncOperationHandle<T> preloadHandle) where T : class {
            preloadHandle.Release();
        }
        
#if UNITY_EDITOR
        // === Editor Operation
        
        public T EditorLoad<T>() where T : class {
            return AssetReferenceUtils.EditorLoadAt<T>(AssetDatabase.GUIDToAssetPath(Address), SubObjectName);
        }

        public T EditorInstantiate<T>() where T : Object => Object.Instantiate(EditorLoad<T>());
        public T EditorInstantiate<T>(Transform parent) where T: Object => Object.Instantiate(EditorLoad<T>(), parent);
        public T EditorInstantiate<T>(Vector3 position, Quaternion rotation) where T : Object => Object.Instantiate(EditorLoad<T>(), position, rotation);
        public T EditorInstantiate<T>(Transform parent, Vector3 position, Quaternion rotation) where T : Object => Object.Instantiate(EditorLoad<T>(), position, rotation, parent);

        public override string ToString() {
            if (!string.IsNullOrEmpty(subObjectName)) {
                return subObjectName;
            }

            if (!string.IsNullOrEmpty(address)) {
                return address;
            }
            return base.ToString();
        }
#endif

        [Conditional("DEBUG")]
        void ValidateLocation<T>(IResourceLocation location) {
            if (location != null) {
#if UNITY_EDITOR
                if (EditorIsUnused(Address)) {
                    Log.Important?.Error($"Loading asset marked as UNUSED: {Address}");
                }
#endif
                return;
            }
#if UNITY_EDITOR
            var path = AssetDatabase.GUIDToAssetPath(Address);
            if (string.IsNullOrWhiteSpace(path)) {
                Log.Important?.Error($"Cannot find path for asset with GUID {Address} of type {typeof(T).FullName}");
                return;
            }
            var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (string.IsNullOrWhiteSpace(SubObjectName)) {
                Log.Important?.Error($"Cannot find location for asset with GUID {Address} of type {typeof(T).FullName} at path {path}. Probably there is something wrong with addressables setup for this asset.", asset);
            } else {
                Log.Important?.Error($"Cannot find location for asset with GUID {Address}/{SubObjectName} of type {typeof(T).FullName} at path {path}. Probably there is something wrong with addressables setup for this asset.", asset);
            }
#else
            Log.Important?.Error($"Cannot find location for asset {RuntimeKey} of type {typeof(T).FullName}");
#endif
        }
        
#if UNITY_EDITOR
        static HashSet<string> s_editorUnusedGuids = new();
        
        public static bool EditorIsUnused(string guid) {
            return s_editorUnusedGuids.Contains(guid);
        }
        
        public static void EditorAssignUnusedGuids(HashSet<string> unusedGuids) {
            s_editorUnusedGuids = unusedGuids;
        }
#endif

#if UNITY_EDITOR
        public void EditorSetValues(string address, string subObject) {
            Address = address;
            SubObjectName = subObject;
            _runtimeKey = null;
        }
        
        #region Edit mode instant (asset database based) loading
        ResourceManager ResourceManager {
            get {
                if (s_resourceManager == null)
                    s_resourceManager = new ResourceManager(new DefaultAllocationStrategy());
                return s_resourceManager;
            }
        }
        static ResourceManager s_resourceManager;
        
        class AsyncEditorLoad<TObject> : AsyncOperationBase<TObject> where TObject : class {

            // === Properties
            /// <summary>
            /// <see cref="AsyncOperationBase{TObject}"/>
            /// </summary>
            protected override float Progress => _progress;
            
            // === Fields
            float _progress;
            string _address;
            string _subObject;

            // === Constructor
            public AsyncEditorLoad(string address, string subObject) {
                _address = address;
                _subObject = subObject;
            }

            // === Loading 
            /// <summary>
            /// <see cref="AsyncOperationBase{TObject}"/>
            /// </summary>
            protected override void Execute() {
                // Obtain main path
                var path = AssetDatabase.GUIDToAssetPath(_address);
                
                // Get defined type
                var assetType = typeof(TObject);

                // No matter of result we will complete in this frame
                _progress = 1f;
                
                if (assetType.IsArray) {
                    // Get asset type no just array
                    assetType = assetType.GetElementType();
                    // Load and setup result
                    Array destinationArray = LoadAsArray( path, assetType);
                    Result = destinationArray as TObject;
                } else if (assetType.IsGenericType && typeof(IList<>) == assetType.GetGenericTypeDefinition()) {
                    // Get asset type no just collection
                    assetType = assetType.GetGenericArguments()[0];
                    // Load and setup result
                    Array destinationArray = LoadAsArray( path, assetType);
                    Result = destinationArray as TObject;
                } else {
                    Result = AssetReferenceUtils.EditorLoadAt<TObject>(path, _subObject);
                }
                
                // Check if we successfully loaded Result
                if (Result != null) {
                    Complete(Result, true, "");
                } else {
                    Complete(Result, false, $"There is no asset of type {typeof(TObject)} with guid {_address} (obtained path [{path}])");
                }
            }

            static Array LoadAsArray(string path, Type assetType) {
                // Load all internal assets of given type 
                var allAssetsAtPath = AssetDatabase.LoadAllAssetsAtPath(path).Where(asset => asset.GetType() == assetType).ToArray();
                
                // Create array from asset type, we get assetType as Type so this is required to get proper cast
                Array destinationArray = Array.CreateInstance(assetType, allAssetsAtPath.Length);
                Array.Copy(allAssetsAtPath, destinationArray, allAssetsAtPath.Length);
                return destinationArray;
            }
        }
        
        class AsyncEditorInstantiate : AsyncOperationBase<GameObject> {

            // === Properties
            /// <summary>
            /// <see cref="AsyncOperationBase{TObject}"/>
            /// </summary>
            protected override float Progress => _progress;
            
            // === Fields 
            // - AsyncOperation and Instantiate setup
            float _progress;
            string _guid;
            Vector3 _position;
            Quaternion _rotation;
            Transform _parent;
            bool _instantiateInWorldSpace;
            // - Internal setup
            bool _withPosition;

            // === Constructors
            public AsyncEditorInstantiate(string guid, Vector3 position, Quaternion rotation, Transform parent) {
                _guid = guid;
                _position = position;
                _rotation = rotation;
                _parent = parent;
                _withPosition = true;
            }

            public AsyncEditorInstantiate(string guid, Transform parent, bool instantiateInWorldSpace) {
                _guid = guid;
                _parent = parent;
                _instantiateInWorldSpace = instantiateInWorldSpace;
                _withPosition = false;
            }

            // === Loading 
            /// <summary>
            /// <see cref="AsyncOperationBase{TObject}"/>
            /// </summary>
            protected override void Execute() {
                // Obtain path and load prefab
                var path = AssetDatabase.GUIDToAssetPath(_guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                // No matter of result we will complete in this frame
                _progress = 1f;
                
                // If prefab is loaded instantiate 
                if (prefab != null) {
                    if (_withPosition) {
                        Result = Object.Instantiate( prefab, _position, _rotation, _parent );
                    } else {
                        Result = Object.Instantiate( prefab, _parent, _instantiateInWorldSpace );
                    }
                    Complete(Result, true, "");
                } else {
                    Complete(Result, false, $"There is no prefab with guid {_guid} (obtained path [{path}])");
                }
            }
        }
        #endregion Edit mode instant (asset database based) loading
#endif

        public bool Equals(ARAssetReference other) {
            return other != null && address == other.address && subObjectName == other.subObjectName;
        }

        public override bool Equals(object obj) {
            return ReferenceEquals(this, obj) || (obj is ARAssetReference other && Equals(other));
        }

        public override int GetHashCode() {
            unchecked {
                return ((address != null ? address.GetHashCode() : 0) * 397) ^ (subObjectName != null ? subObjectName.GetHashCode() : 0);
            }
        }

        public static SerializationAccessor Serialization(ARAssetReference reference) => new(reference);
        public struct SerializationAccessor {
            readonly ARAssetReference _reference;

            public SerializationAccessor(ARAssetReference reference) {
                _reference = reference;
            }

            public ref string Address => ref _reference.address;
            public ref string SubObjectName => ref _reference.subObjectName;
        }
    }
}