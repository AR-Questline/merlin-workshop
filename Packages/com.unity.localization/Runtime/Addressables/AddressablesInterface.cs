using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.Localization.Operations;
using UnityEngine.Pool;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace UnityEngine.Localization
{
    /// <summary>
    /// Allows for intercepting calls to Addressables.
    /// Used for testing and so we can provide Edit mode support without going through Addressables.
    /// </summary>
    internal class AddressablesInterface
    {
        static AddressablesInterface s_Instance;

        public static AddressablesInterface Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    #if UNITY_EDITOR
                    // We have several tests that will change this value and then reset to null at the end. This was causing them to become unstable as we would then initialize with the wrong type. This is the safest approach to guarantee that we use the Editor version.
                    s_Instance = (AddressablesInterface)Activator.CreateInstance(Type.GetType("UnityEditor.Localization.EditorAddressablesInterface, Unity.Localization.Editor", true), true);
                    #else
                    s_Instance = new AddressablesInterface();
                    #endif
                }
                return s_Instance;
            }
            set => s_Instance = value;
        }

        public static ResourceManager ResourceManager => Addressables.ResourceManager;
        public static void Acquire(AsyncOperationHandle handle) => Instance.AcquireInternal(handle);
        public static void Release(AsyncOperationHandle handle) => Instance.ReleaseInternal(handle);

        public static void SafeRelease(AsyncOperationHandle handle)
        {
            if (handle.IsValid())
                Instance.ReleaseInternal(handle);
        }

        public static void ReleaseAndReset<TObject>(ref AsyncOperationHandle<TObject> handle)
        {
            if (handle.IsValid())
            {
                Instance.ReleaseInternal(handle);
                handle = default;
            }
        }

        public static AsyncOperationHandle<IList<AsyncOperationHandle>> CreateGroupOperation(List<AsyncOperationHandle> asyncOperations)
        {
            // The Group operation will call Release on all the handles when it is finished however it never calls acquire on them.
            // We want to have control over when the operation is released so we aquire here in order to prevent an overall change in reference count.
            foreach(var op in asyncOperations)
            {
                Acquire(op);
            }

            var go = LocalizationGroupOperation.Pool.Get();
            go.Init(asyncOperations);
            return ResourceManager.StartOperation(go, default);
        }

        public static AsyncOperationHandle<IList<IResourceLocation>> LoadResourceLocationsWithLabelsAsync(IEnumerable labels, Addressables.MergeMode mode, Type type = null) => Instance.LoadResourceLocationsWithLabelsAsyncInternal(labels, mode, type);
        public static AsyncOperationHandle<IList<IResourceLocation>> LoadTableLocationsAsync(string tableName, LocaleIdentifier id, Type type) => Instance.LoadTableLocationsAsyncInternal(tableName, id, type);
        public static AsyncOperationHandle<IList<TObject>> LoadAssetsFromLocations<TObject>(IList<IResourceLocation> locations, Action<TObject> callback) => Instance.LoadAssetsFromLocationsInternal(locations, callback);
        public static AsyncOperationHandle<TObject> LoadAssetFromGUID<TObject>(string guid) where TObject : class => Instance.LoadAssetFromGUIDInternal<TObject>(guid);
        public static AsyncOperationHandle<TObject> LoadAssetFromName<TObject>(string name) where TObject : class => Instance.LoadAssetFromNameInternal<TObject>(name);
        public static AsyncOperationHandle<TObject> LoadTableFromLocation<TObject>(IResourceLocation location) where TObject : class => Instance.LoadTableFromLocationInternal<TObject>(location);
        public static AsyncOperationHandle<IList<TObject>> LoadAssetsWithLabel<TObject>(string label, Action<TObject> callback) where TObject : class => Instance.LoadAssetsWithLabelInternal(label, callback);

        internal virtual void AcquireInternal(AsyncOperationHandle handle) => ResourceManager.Acquire(handle);
        internal virtual void ReleaseInternal(AsyncOperationHandle handle) => Addressables.Release(handle);
        internal virtual AsyncOperationHandle<IList<IResourceLocation>> LoadResourceLocationsWithLabelsAsyncInternal(IEnumerable labels, Addressables.MergeMode mode, Type type = null) => Addressables.LoadResourceLocationsAsync(labels, mode, type);
        internal virtual AsyncOperationHandle<IList<IResourceLocation>> LoadTableLocationsAsyncInternal(string tableName, LocaleIdentifier id, Type type) => Addressables.LoadResourceLocationsAsync(AddressHelper.GetTableAddress(tableName, id), type);
        internal virtual AsyncOperationHandle<IList<TObject>> LoadAssetsFromLocationsInternal<TObject>(IList<IResourceLocation> locations, Action<TObject> callback) => Addressables.LoadAssetsAsync(locations, callback);
        internal virtual AsyncOperationHandle<TObject> LoadAssetFromGUIDInternal<TObject>(string guid) where TObject : class => Addressables.LoadAssetAsync<TObject>(guid);
        internal virtual AsyncOperationHandle<TObject> LoadAssetFromNameInternal<TObject>(string name) where TObject : class => Addressables.LoadAssetAsync<TObject>(name);
        internal virtual AsyncOperationHandle<TObject> LoadTableFromLocationInternal<TObject>(IResourceLocation location) where TObject : class => Addressables.LoadAssetAsync<TObject>(location);
        internal virtual AsyncOperationHandle<IList<TObject>> LoadAssetsWithLabelInternal<TObject>(string label, Action<TObject> callback) where TObject : class => Addressables.LoadAssetsAsync(label, callback);

        internal virtual AsyncOperationHandle<IResourceLocator> InitializeAddressablesAsync() => Addressables.InitializeAsync();
    }
}
