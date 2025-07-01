using System;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Operations;
using UnityEngine.Localization.Settings;
using UnityEngine.Pool;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine.Localization.Tables
{
    /// <summary>
    /// Run time representation of an entry in a <see cref="AssetTable"/>.
    /// </summary>
    public class AssetTableEntry : TableEntry
    {
        internal AsyncOperationHandle<Object[]> PreloadAsyncOperation { get; set; }
        internal AsyncOperationHandle AsyncOperation { get; set; }

        string m_GuidCache;
        string m_SubAssetNameCache;

        /// <summary>
        /// The address used to load the asset from Addressables. Contains the <see cref="Guid"/> and the optional <see cref="SubAssetName"/> in the form <c>Guid[SubAssetName]</c>.
        /// </summary>
        public string Address
        {
            get => Data.Localized;
            set
            {
                Data.Localized = value;
                m_GuidCache = null;
                m_SubAssetNameCache = null;
            }
        }

        /// <summary>
        /// The asset Guid.
        /// </summary>
        public string Guid
        {
            get
            {
                if (m_GuidCache == null)
                    m_GuidCache = AssetAddress.GetGuid(Address);
                return m_GuidCache;
            }
            set => Address = value;
        }

        /// <summary>
        /// The name of the sub-asset if one is used; otherwise <see langword="null"/>.
        /// </summary>
        public string SubAssetName
        {
            get
            {
                if (m_SubAssetNameCache == null)
                    m_SubAssetNameCache = AssetAddress.GetSubAssetName(Address);
                return m_SubAssetNameCache;
            }
        }

        /// <summary>
        /// Does this entry contain any data? Checks if <see cref="Address"/> is empty.
        /// </summary>
        public bool IsEmpty => string.IsNullOrEmpty(Address);

        /// <summary>
        /// Does the <see cref="Address"/> reference a sub-asset?
        /// </summary>
        public bool IsSubAsset => AssetAddress.IsSubAsset(Address);

        internal AssetTableEntry()
        {
        }

        /// <summary>
        /// Attempts to remove the entry from the <see cref="AssetTable"/> that it belongs to.
        /// If <see cref="TableEntry.Table"/> is null then a warning will be produced.
        /// </summary>
        public void RemoveFromTable()
        {
            var assetTable = Table as AssetTable;
            if (assetTable == null)
            {
                Debug.LogWarning($"Failed to remove {nameof(AssetTableEntry)} with id {KeyId} and address `{Address}` as it does not belong to a table.");
            }
            else
            {
                assetTable.Remove(KeyId);
            }
        }

        internal Type GetExpectedType()
        {
            var sharedTableData = Table.SharedData;
            foreach (var md in sharedTableData.Metadata.MetadataEntries)
            {
                if (md is AssetTypeMetadata at)
                {
                    if (at.Contains(KeyId))
                    {
                        return at.Type;
                    }
                }
            }
            return typeof(Object);
        }

        /// <summary>
        /// Provides support for overriding the localized asset for the Entry. Note this is only temporary and will not persist in the Editor or if the table is reloaded.
        /// This allows for a table to be updated in the player.
        /// To remove the override, call <see cref="AssetTable.ReleaseAsset(AssetTableEntry)"/>.
        /// </summary>
        /// <typeparam name="T">The type to store the asset as locally.</typeparam>
        /// <param name="asset">The asset reference to use instead of <see cref="Address"/>.</param>
        /// <example>
        /// This example shows how you can update the AssetTable entry values when the Locale is changed.
        /// <code source="../../DocCodeSamples.Tests/LocalizedAssetSamples.cs" region="override-asset-entry-1"/>
        /// </example>
        /// <example>
        /// This example shows how you can update all AssetTable entries at the start and ensure that the tables are never unloaded so that the changes are persistent throughtout the lifetime of the player.
        /// <code source="../../DocCodeSamples.Tests/LocalizedAssetSamples.cs" region="override-asset-entry-2"/>
        /// </example>
        public void SetAssetOverride<T>(T asset) where T : Object
        {
            AddressablesInterface.SafeRelease(AsyncOperation);
            AsyncOperation = AddressablesInterface.ResourceManager.CreateCompletedOperation(asset, null);
        }
    }

    /// <summary>
    /// An AssetTable
    /// </summary>
    public class AssetTable : DetailedLocalizationTable<AssetTableEntry>, IPreloadRequired
    {
        AsyncOperationHandle m_PreloadOperationHandle;

        ResourceManager ResourceManager => AddressablesInterface.ResourceManager;

        /// <summary>
        /// Handle to the preload operation for this table.
        /// Calling this will start the asset tables preloading operation, it will preload all assets referenced
        /// unless the Metadata <see cref="PreloadAssetTableMetadata"/> is found and has the
        /// <see cref="PreloadAssetTableMetadata.PreloadBehaviour.NoPreload"/> behavior.
        /// </summary>
        public virtual AsyncOperationHandle PreloadOperation
        {
            get
            {
                if (!m_PreloadOperationHandle.IsValid())
                {
                    m_PreloadOperationHandle = PreloadAssets();
                }
                return m_PreloadOperationHandle;
            }
        }

        AsyncOperationHandle PreloadAssets()
        {
            // Check the metadata to see if we should preload. First we check the table preload data, if one does not exist then check the key database(global).
            var preload = GetMetadata<PreloadAssetTableMetadata>() ?? SharedData.Metadata.GetMetadata<PreloadAssetTableMetadata>();

            // If no preload metadata was found then we will preload all assets by default.
            if (preload?.Behaviour != PreloadAssetTableMetadata.PreloadBehaviour.NoPreload)
            {
                var handleList = ListPool<AsyncOperationHandle>.Get();

                // Preload all
                foreach (var entry in Values)
                {
                    if (!entry.IsEmpty && !entry.PreloadAsyncOperation.IsValid())
                    {
                        // We have to preload the asset as an array so that we get all sub objects. The reason for this is that some assets,
                        // such as Sprite can contain multiple sub objects and if we load it as Object then we may get the wrong one.
                        // For example, if we load a Sprite as an Object and it has the same name as its Texture asset then it will load as a Texture,
                        // not Sprite. So if we load as an array we get both, we can then pick the one we need later based on the type passed into GetAssetAsync. (LOC-143)
                        // We can not rely on the sub-asset name when loading as Object as its common to have sub-assets with the same name
                        // but different types, e.g GameObject and Mesh.
                        entry.PreloadAsyncOperation = AddressablesInterface.LoadAssetFromGUID<Object[]>(entry.Guid);
                        handleList.Add(entry.PreloadAsyncOperation);
                    }
                }
                if (handleList.Count > 0)
                {
                    return AddressablesInterface.CreateGroupOperation(handleList);
                }
                else
                {
                    ListPool<AsyncOperationHandle>.Release(handleList);
                }
            }

            // Nothing to preload, we are done
            return ResourceManager.CreateCompletedOperation(this, null);
        }

        /// <summary>
        /// Returns the loading operation for the asset.
        /// Check isDone to see if the asset is available for immediate use, if not you can yield on the operation or add a callback subscriber.
        /// </summary>
        /// <param name="entryReference"></param>
        /// <typeparam name="TObject">The type of object to load.</typeparam>
        /// <returns></returns>
        public AsyncOperationHandle<TObject> GetAssetAsync<TObject>(TableEntryReference entryReference) where TObject : Object
        {
            var entry = GetEntryFromReference(entryReference);
            if (entry == null)
            {
                var keyName = entryReference.ResolveKeyName(SharedData);
                return ResourceManager.CreateCompletedOperation<TObject>(null, $"Could not find asset with key \"{keyName}\"");
            }
            return GetAssetAsync<TObject>(entry);
        }

        internal AsyncOperationHandle<TObject> GetAssetAsync<TObject>(AssetTableEntry entry) where TObject : Object
        {
            if (entry.AsyncOperation.IsValid())
            {
                try
                {
                    return entry.AsyncOperation.Convert<TObject>();
                }
                catch (InvalidCastException)
                {
                    // Release the old operation
                    AddressablesInterface.Release(entry.AsyncOperation);
                    entry.AsyncOperation = default;
                }
            }

            if (entry.IsEmpty)
            {
                // Empty entries are treated as null.
                var emptyOperation = ResourceManager.CreateCompletedOperation<TObject>(null, null);
                entry.AsyncOperation = emptyOperation;
                return emptyOperation;
            }

            var subAssetOperation = LoadSubAssetOperation<TObject>.Pool.Get();
            subAssetOperation.Init(entry.PreloadAsyncOperation, entry.Address, entry.IsSubAsset, entry.SubAssetName);
            var operation = ResourceManager.StartOperation(subAssetOperation, entry.PreloadAsyncOperation);
            entry.AsyncOperation = operation;
            return operation;
        }

        /// <summary>
        /// Releases all assets that have been preloaded or cached and resets the preload state so it can be performed again.
        /// Note: This is called automatically by <see cref="LocalizedAssetDatabase"/> when the <see cref="LocalizationSettings.SelectedLocale"/> is changed.
        /// </summary>
        public void ReleaseAssets()
        {
            if (m_PreloadOperationHandle.IsValid())
            {
                AddressablesInterface.Release(m_PreloadOperationHandle);
                m_PreloadOperationHandle = default;
            }

            foreach (var entry in Values)
            {
                ReleaseAsset(entry);
            }
        }

        /// <summary>
        /// Release an asset for a single entry that has been preloaded or cached.
        /// </summary>
        /// <param name="entry">A reference to the entry in the table.</param>
        public void ReleaseAsset(AssetTableEntry entry)
        {
            if (entry == null)
                return;

            if (entry.PreloadAsyncOperation.IsValid())
            {
                AddressablesInterface.Release(entry.PreloadAsyncOperation);
                entry.PreloadAsyncOperation = default;
            }

            if (entry.AsyncOperation.IsValid())
            {
                AddressablesInterface.Release(entry.AsyncOperation);
                entry.AsyncOperation = default;
            }
        }

        /// <summary>
        /// Release an asset for a single entry that have been preloaded  or cached
        /// </summary>
        /// <param name="entry">A reference to the entry in the table.</param>
        /// <example>
        /// In this example the Audio Clip is only used by this script and can be unloaded after the clip has finished playing.
        /// By using ReleaseAsset we can tell the localization system to release its handle to the asset and allow it to be unloaded from memory.
        /// <code source="../../DocCodeSamples.Tests/ReleaseAssetExample.cs" region="release-asset-example"/>
        /// </example>
        public void ReleaseAsset(TableEntryReference entry)
        {
            ReleaseAsset(GetEntryFromReference(entry));
        }

        /// <summary>
        /// Creates a new, empty AssetTableEntry.
        /// </summary>
        /// <returns></returns>
        public override AssetTableEntry CreateTableEntry()
        {
            return new AssetTableEntry() { Table = this, Data = new TableEntryData() };
        }

        #if UNITY_EDITOR
        /// <summary>
        // ScriptableObject properties may persist during runs in the editor, so we reset them here to keep each play consistent.
        /// </summary>
        void OnEnable()
        {
            m_PreloadOperationHandle = default;
        }

        #endif
    }
}
