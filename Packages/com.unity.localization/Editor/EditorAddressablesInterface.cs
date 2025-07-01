using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using static UnityEngine.AddressableAssets.Addressables;
using Object = UnityEngine.Object;

namespace UnityEditor.Localization
{
    class EditorAddressablesInterface : AddressablesInterface
    {
        class InMemoryResourceLocation : ResourceLocationBase
        {
            public Object Asset { get; set; }

            public InMemoryResourceLocation(string name, string id, string providerId, Type t, params IResourceLocation[] dependencies) :
                base(name, id, providerId, t, dependencies)
            {
            }
        }

        readonly ResourceManager m_ResourceManager = new ResourceManager();

        // Addressables is only safe to use in playmode, any other time we use the asset database. (LOC-722)
        internal bool UseAddressables => (LocalizationSettings.Instance.IsPlayingOverride.HasValue && LocalizationSettings.Instance.IsPlayingOverride.Value) || 
                                        (LocalizationSettings.Instance.IsPlayingOrWillChangePlaymode && PlayModeState == PlayModeStateChange.EnteredPlayMode);

        internal static PlayModeStateChange PlayModeState => EditorApplication.isPlaying ?
            EditorApplication.isPlayingOrWillChangePlaymode ? PlayModeStateChange.EnteredPlayMode : PlayModeStateChange.ExitingPlayMode :
            EditorApplication.isPlayingOrWillChangePlaymode ? PlayModeStateChange.ExitingEditMode : PlayModeStateChange.EnteredEditMode;

        static TObject LoadAsset<TObject>(string address) where TObject : class
        {
            var asset = AssetUtility.LoadAssetFromAddress(address, typeof(TObject)) as TObject;
            if (asset != null)
                return asset;

            var path = AssetUtility.GetPathFromAddress(address);
            if (string.IsNullOrEmpty(path))
                Debug.LogWarning($"Could not find an asset of type {typeof(TObject)} with the guid `{address}` from `{path}`.");
            return null;
        }

        internal override AsyncOperationHandle<IList<IResourceLocation>> LoadResourceLocationsWithLabelsAsyncInternal(IEnumerable labels, MergeMode mode, Type type = null)
        {
            if (LocalizationSettings.Instance.IsPlayingOrWillChangePlaymode)
                return base.LoadResourceLocationsWithLabelsAsyncInternal(labels, mode, type);

            throw new NotImplementedException("Should not be called outside of play mode");
        }

        internal override AsyncOperationHandle<IList<IResourceLocation>> LoadTableLocationsAsyncInternal(string tableName, LocaleIdentifier id, Type type)
        {
            if (UseAddressables)
                return base.LoadTableLocationsAsyncInternal(tableName, id, type);

            LocalizationTable table = null;
            var locations = new List<IResourceLocation>();

            if (type == typeof(AssetTable))
            {
                table = LocalizationEditorSettings.GetAssetTableCollection(tableName)?.GetTable(id);
            }
            else if (type == typeof(StringTable))
            {
                table = LocalizationEditorSettings.GetStringTableCollection(tableName)?.GetTable(id);
            }
            else
            {
                Debug.LogError($"Unknown table type {type}");
            }

            if (table != null)
            {
                var path = AssetDatabase.GetAssetPath(table);

                // When running tests an asset may not have a path so we track it as in memory.
                locations.Add(string.IsNullOrEmpty(path) ?
                    new InMemoryResourceLocation(tableName, nameof(InMemoryResourceLocation), nameof(EditorAddressablesInterface), type) {Asset = table} :
                    new ResourceLocationBase(tableName, path, nameof(EditorAddressablesInterface), type));
            }

            return m_ResourceManager.CreateCompletedOperation<IList<IResourceLocation>>(locations, null);
        }

        internal override AsyncOperationHandle<IList<TObject>> LoadAssetsFromLocationsInternal<TObject>(IList<IResourceLocation> locations, Action<TObject> callback)
        {
            if (LocalizationSettings.Instance.IsPlayingOrWillChangePlaymode)
                return base.LoadAssetsFromLocationsInternal(locations, callback);

            throw new NotImplementedException("Should not be called outside of play mode");
        }

        internal override AsyncOperationHandle<TObject> LoadAssetFromGUIDInternal<TObject>(string guid)
        {
            if (UseAddressables)
                return base.LoadAssetFromGUIDInternal<TObject>(guid);
            return m_ResourceManager.CreateCompletedOperation(LoadAsset<TObject>(guid), null);
        }

        internal override AsyncOperationHandle<TObject> LoadAssetFromNameInternal<TObject>(string name)
        {
            if (UseAddressables)
                return base.LoadAssetFromNameInternal<TObject>(name);

            var settings = LocalizationEditorSettings.Instance.GetAddressableAssetSettings(false);
            if (settings == null)
                return m_ResourceManager.CreateCompletedOperation(default(TObject), null);

            foreach (var group in settings.groups)
            {
                if (group == null)
                    continue;

                foreach (var entry in group.entries)
                {
                    if (entry.address == name && !string.IsNullOrEmpty(entry.guid))
                    {
                        var asset = LoadAsset<TObject>(entry.guid);
                        if (asset != null)
                            return m_ResourceManager.CreateCompletedOperation(asset, null);
                    }
                }
            }

            return m_ResourceManager.CreateCompletedOperation(default(TObject), "Failed to find asset with name: " + name);
        }

        internal override AsyncOperationHandle<TObject> LoadTableFromLocationInternal<TObject>(IResourceLocation location)
        {
            if (UseAddressables)
                return base.LoadTableFromLocationInternal<TObject>(location);

            TObject table = null;
            if (location is InMemoryResourceLocation memoryResourceLocation)
            {
                table = memoryResourceLocation.Asset as TObject;
            }
            else if (!string.IsNullOrEmpty(location.InternalId))
            {
                table = AssetDatabase.LoadAssetAtPath(location.InternalId, typeof(TObject)) as TObject;
            }

            return m_ResourceManager.CreateCompletedOperation(table, table != null ? null : "Failed to load table: " + location);
        }

        internal override AsyncOperationHandle<IList<TObject>> LoadAssetsWithLabelInternal<TObject>(string label, Action<TObject> callback)
        {
            if (UseAddressables)
                return base.LoadAssetsWithLabelInternal(label, callback);

            IList<TObject> list = new List<TObject>();

            var settings = LocalizationEditorSettings.Instance.GetAddressableAssetSettings(false);
            if (settings != null)
            {
                foreach (var group in settings.groups)
                {
                    if (group == null)
                        continue;

                    foreach (var entry in group.entries)
                    {
                        if (entry.labels.Contains(label) && !string.IsNullOrEmpty(entry.guid))
                        {
                            var asset = LoadAsset<TObject>(entry.guid);
                            if (asset != null)
                            {
                                list.Add(asset);
                                callback?.Invoke(asset);
                            }
                        }
                    }
                }
            }
            return m_ResourceManager.CreateCompletedOperation(list, null);
        }

        internal override AsyncOperationHandle<IResourceLocator> InitializeAddressablesAsync()
        {
            if (UseAddressables)
                return base.InitializeAddressablesAsync();
            return default;
        }
    }
}
