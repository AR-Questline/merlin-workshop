using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Localization.Operations;
using UnityEngine.Localization.Tables;
using UnityEngine.Pool;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine.Localization.Settings
{
    /// <summary>
    /// Options for the different fallback behaviours that are available.
    /// </summary>
    public enum FallbackBehavior
    {
        /// <summary>
        /// Uses the <see cref="LocalizedDatabase{TTable, TEntry}.UseFallback"/> value in <see cref="LocalizationSettings.StringDatabase"/>
        /// when localizing a string or <see cref="LocalizationSettings.AssetDatabase"/> when localizing an asset.
        /// </summary>
        UseProjectSettings,

        /// <summary>
        /// Do not fallback.
        /// </summary>
        DontUseFallback,

        /// <summary>
        /// Attempts to use a fallback when a localized value is not found.
        /// </summary>
        UseFallback
    }

    /// <summary>
    /// Options for how to handle a missing translation.
    /// </summary>
    [Flags]
    public enum MissingTranslationBehavior
    {
        /// <summary>
        /// Includes the missing translation message in the translated string.
        /// </summary>
        ShowMissingTranslationMessage = 1,

        /// <summary>
        /// Prints the missing translation message using [Debug.LogWarning](https://docs.unity3d.com/ScriptReference/Debug.LogWarning.html).
        /// </summary>
        PrintWarning = 2
    }

    /// <summary>
    /// Options for how to handle asynchronous operations.
    /// </summary>
    public enum AsynchronousBehaviour
    {
        /// <summary>
        /// Use the default behavior specified in the <see cref="LocalizedReference"/>.
        /// </summary>
        Default,

        /// <summary>
        /// Forces the operation to complete synchronously.
        /// </summary>
        ForceSynchronous
    }

    /// <summary>
    /// Can be assigned to <see cref="LocalizedDatabase{TTable, TEntry}.TableProvider"/> to override the default table loading through Addressables in order to provide a custom table.
    /// </summary>
    /// <example>
    /// This example demonstrates how to use the <see cref="ITableProvider"/> to provide a custom String Table without using the Addressables system.
    /// This approach is particularly useful when you want to allow users to add third-party content, such as modding.
    /// The localization data could be loaded from an external file and then converted into a table at runtime.
    /// <code source="../../../DocCodeSamples.Tests/TableProviderSamples.cs" region="custom-table-provider"/>
    /// <code source="../../../DocCodeSamples.Tests/TableProviderSamples.cs" region="set-provider-editor"/>
    /// </example>
    public interface ITableProvider
    {
        /// <summary>
        /// Provides a way to return a custom table when when attempting to load from <see cref="LocalizedDatabase{TTable, TEntry}.GetTableAsync(TableReference, Locale)"/>.
        /// </summary>
        /// <param name="tableCollectionName"></param>
        /// <param name="locale"></param>
        /// <typeparam name="TTable"></typeparam>
        /// <returns>A valid table or <see langword="default"/>, which will trigger the default table loading.</returns>
        AsyncOperationHandle<TTable> ProvideTableAsync<TTable>(string tableCollectionName, Locale locale) where TTable : LocalizationTable;
    }

    /// <summary>
    /// Gets a notification when a <see cref="StringTable"/> or <see cref="AssetTable"/> completes loading.
    /// </summary>
    /// <example>
    /// This example demonstrates how to use the <see cref="ITablePostprocessor"/> to apply changes to a table after it has loaded but before it has been used.
    /// This can be beneficial when you wish to modify or add entries to a table, such as when supporting third-party content, for example modding.
    /// <code source="../../../DocCodeSamples.Tests/TablePatcherSamples.cs" region="custom-table-patcher"/>
    /// <code source="../../../DocCodeSamples.Tests/TablePatcherSamples.cs" region="set-patcher-editor"/>
    /// </example>
    public interface ITablePostprocessor
    {
        /// <summary>
        /// This could be used to patch a table with updated values.
        /// </summary>
        /// <param name="table">The loaded <see cref="StringTable"/> or <see cref="AssetTable"/>.</param>
        void PostprocessTable(LocalizationTable table);
    }

    /// <summary>
    /// Provides common functionality for both string and asset table fetching.
    /// </summary>
    /// <typeparam name="TTable"></typeparam>
    /// <typeparam name="TEntry"></typeparam>
    [Serializable]
    public abstract class LocalizedDatabase<TTable, TEntry> : IPreloadRequired, IReset, IDisposable
        where TTable : DetailedLocalizationTable<TEntry>
        where TEntry : TableEntry
    {
        /// <summary>
        /// Contains the results of a request. The found entry and the table the entry was found in,
        /// this may be different if a fall back occurred.
        /// </summary>
        public struct TableEntryResult
        {
            /// <summary>
            /// The entry that was resolved or <see langword="null"/> if one could not be found.
            /// </summary>
            public TEntry Entry { get; }

            /// <summary>
            /// The table the entry was extracted from. When <see cref="Entry"/> is <see langword="null"/>, this contains the last table that was tried.
            /// </summary>
            public TTable Table { get; }

            internal TableEntryResult(TEntry entry, TTable table)
            {
                Entry = entry;
                Table = table;
            }
        }

        /// <summary>
        /// Preload operation.
        /// Loads all tables and their contents(when applicable) marked with the preload label for the selected locale.
        /// </summary>
        public AsyncOperationHandle PreloadOperation
        {
            get
            {
                #if UNITY_EDITOR
                // Don't preload in Editor preview
                if (!LocalizationSettings.Instance.IsPlayingOrWillChangePlaymode)
                    return AddressablesInterface.ResourceManager.CreateCompletedOperation(this, null);
                #endif

                if (!m_PreloadOperationHandle.IsValid())
                {
                    var operation = PreloadDatabaseOperation<TTable, TEntry>.Pool.Get();
                    operation.Init(this);
                    m_PreloadOperationHandle = AddressablesInterface.ResourceManager.StartOperation(operation, default);
                }
                return m_PreloadOperationHandle;
            }
        }

        [SerializeField] TableReference m_DefaultTableReference;
        [SerializeReference] ITableProvider m_CustomTableProvider;
        [SerializeReference] ITablePostprocessor m_CustomTablePostprocessor;
        [SerializeField] AsynchronousBehaviour m_AsynchronousBehaviour;
        [SerializeField] bool m_UseFallback;

        internal AsyncOperationHandle m_PreloadOperationHandle;
        Action<AsyncOperationHandle> m_ReleaseNextFrame;

        readonly Action<AsyncOperationHandle<TTable>> m_PatchTableContentsAction;
        readonly Action<AsyncOperationHandle<TTable>> m_RegisterSharedTableAndGuidOperationAction;
        readonly Action<AsyncOperationHandle<TTable>> m_RegisterCompletedTableOperationAction;

        internal Action<AsyncOperationHandle> ReleaseNextFrame => m_ReleaseNextFrame;

        // Used in place of the actual selected locale when it is still being loaded.
        internal static readonly LocaleIdentifier k_SelectedLocaleId = new LocaleIdentifier("selected locale placeholder");

        internal Dictionary<(LocaleIdentifier localeIdentifier, string tableNameOrGuid), AsyncOperationHandle<TTable>> TableOperations
        {
            get;
        } = new Dictionary<(LocaleIdentifier localeIdentifier, string tableNameOrGuid), AsyncOperationHandle<TTable>>();

        internal Dictionary<Guid, AsyncOperationHandle<SharedTableData>> SharedTableDataOperations
        {
            get;
        } = new Dictionary<Guid, AsyncOperationHandle<SharedTableData>>();

        /// <summary>
        /// The default table to use when no table collection name is provided.
        /// </summary>
        public virtual TableReference DefaultTable
        {
            get => m_DefaultTableReference;
            set => m_DefaultTableReference = value;
        }

        /// <summary>
        /// Called when attempting to load a table, can be used to override the default table loading through Addressables in order to provide a custom table.
        /// </summary>
        /// <example>
        /// This example demonstrates how to use the <see cref="ITableProvider"/> to provide a custom String Table without using the Addressables system.
        /// This approach is particularly useful when you want to allow users to add third-party content, such as modding.
        /// The localization data could be loaded from an external file and then converted into a table at runtime.
        /// <code source="../../../DocCodeSamples.Tests/TableProviderSamples.cs" region="custom-table-provider"/>
        /// <code source="../../../DocCodeSamples.Tests/TableProviderSamples.cs" region="set-provider-editor"/>
        /// </example>
        public ITableProvider TableProvider
        {
            get => m_CustomTableProvider;
            set => m_CustomTableProvider = value;
        }

        /// <summary>
        /// Gets a notification when a table completes loading.
        /// This can be used to apply changes to a table at runtime, such as updating or creating new entries.
        /// </summary>
        /// <example>
        /// This example demonstrates how to use the <see cref="ITablePostprocessor"/> to apply changes to a table after it has loaded but before it has been used.
        /// This can be beneficial when you wish to modify or add entries to a table, such as when supporting third-party content, for example modding.
        /// <code source="../../../DocCodeSamples.Tests/TablePatcherSamples.cs" region="custom-table-patcher"/>
        /// <code source="../../../DocCodeSamples.Tests/TablePatcherSamples.cs" region="set-patcher-editor"/>
        /// </example>
        public ITablePostprocessor TablePostprocessor
        {
            get => m_CustomTablePostprocessor;
            set => m_CustomTablePostprocessor = value;
        }

        /// <summary>
        /// Should the fallback Locale be used when a translation could not be found?.
        /// </summary>
        public bool UseFallback
        {
            get => m_UseFallback;
            set => m_UseFallback = value;
        }

        /// <summary>
        /// Options for how to handle asyncronous operations.
        /// </summary>
        public AsynchronousBehaviour AsynchronousBehaviour
        {
            get => m_AsynchronousBehaviour;
            set => m_AsynchronousBehaviour = value;
        }

        /// <summary>
        /// Creates a new instance of the database.
        /// </summary>
        public LocalizedDatabase()
        {
            m_PatchTableContentsAction = PatchTableContents;
            m_RegisterSharedTableAndGuidOperationAction = RegisterSharedTableAndGuidOperation;
            m_RegisterCompletedTableOperationAction = RegisterCompletedTableOperation;
            m_ReleaseNextFrame = LocalizationBehaviour.ReleaseNextFrame;
        }

        internal TableReference GetDefaultTable()
        {
            if (m_DefaultTableReference.ReferenceType == TableReference.Type.Empty)
                throw new Exception($"Trying to get the DefaultTable however the {GetType().Name} DefaultTable value has not been set. This can be configured in the Localization Settings.");

            return m_DefaultTableReference;
        }

        internal void RegisterCompletedTableOperation(AsyncOperationHandle<TTable> tableOperation)
        {
            if (!tableOperation.IsDone)
            {
                tableOperation.Completed += m_RegisterCompletedTableOperationAction;
                return;
            }

            var table = tableOperation.Result;
            if (table == null)
                return;

            RegisterTableNameOperation(tableOperation, table.LocaleIdentifier, table.TableCollectionName);

            // If the table is already present then RegisterTableNameOperation will release the operation which may cause it to become invalid.
            if (tableOperation.IsValid())
                RegisterSharedTableAndGuidOperation(tableOperation);
        }

        void RegisterTableNameOperation(AsyncOperationHandle<TTable> tableOperation, LocaleIdentifier localeIdentifier, string tableName)
        {
            var key = (localeIdentifier, tableName);
            if (TableOperations.ContainsKey(key))
            {
                // Dont hold onto this.
                return;
            }

            TableOperations[key] = tableOperation;

            if (TablePostprocessor != null)
            {
                // Patch the table contents
                if (tableOperation.IsDone)
                    PatchTableContents(tableOperation);
                else
                    tableOperation.Completed += m_PatchTableContentsAction;
            }
        }

        void RegisterSharedTableAndGuidOperation(AsyncOperationHandle<TTable> tableOperation)
        {
            if (!tableOperation.IsDone)
            {
                tableOperation.Completed += m_RegisterSharedTableAndGuidOperationAction;
                return;
            }

            var table = tableOperation.Result;
            if (table == null)
                return;

            // Register the shared table data Guid.
            var tableNameGuid = table.SharedData.TableCollectionNameGuid;
            if (!SharedTableDataOperations.ContainsKey(tableNameGuid))
                SharedTableDataOperations[tableNameGuid] = AddressablesInterface.ResourceManager.CreateCompletedOperation(table.SharedData, null);

            // Register the table via the locale identifier and guid.
            var localeAndGuid = (table.LocaleIdentifier, TableReference.StringFromGuid(tableNameGuid));
            if (!TableOperations.ContainsKey(localeAndGuid))
            {
                // We acquire when using the guid.
                AddressablesInterface.Acquire(tableOperation);
                TableOperations[localeAndGuid] = tableOperation;
            }
        }

        /// <summary>
        /// Returns the Default table.
        /// This method is asynchronous and may not have an immediate result.
        /// Check [IsDone](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.IsDone) to see if the data is available,
        /// if it is false then you can use the [Completed](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Completed) event to get a callback when it is finished,
        /// yield on the operation or call [WaitForCompletion](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.WaitForCompletion)
        /// to force the operation to complete.
        /// </summary>
        /// <returns></returns>
        public AsyncOperationHandle<TTable> GetDefaultTableAsync()
        {
            return GetTableAsync(GetDefaultTable());
        }

        /// <summary>
        /// Returns the named table.
        /// This method is asynchronous and may not have an immediate result.
        /// Check [IsDone](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.IsDone) to see if the data is available,
        /// if it is false then you can use the [Completed](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Completed) event to get a callback when it is finished,
        /// yield on the operation or call [WaitForCompletion](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.WaitForCompletion)
        /// to force the operation to complete.
        /// </summary>
        /// <remarks>
        /// Internally the following is performed when a table is requested:
        /// ![](../manual/images/scripting/GetTable.dot.svg)
        /// </remarks>
        /// <param name="tableReference">The table identifier. Can be either the name of the table or the table collection name Guid.</param>
        /// <param name="locale">The <see cref="Locale"/> to load the table from, use null to default to <see cref="LocalizationSettings.SelectedLocale"/>.</param>
        /// <returns></returns>
        public virtual AsyncOperationHandle<TTable> GetTableAsync(TableReference tableReference, Locale locale = null)
        {
            // Extract the Locale Id or use a placeholder if we are using the selected locale and it is not ready yet.
            bool localeAvailable = locale != null || LocalizationSettings.SelectedLocaleAsync.IsDone;
            bool useSelectedLocalePlaceholder = true;
            if (localeAvailable)
            {
                if (locale == null)
                {
                    if (LocalizationSettings.SelectedLocaleAsync.Result == null)
                        return AddressablesInterface.ResourceManager.CreateCompletedOperation<TTable>(null, "SelectedLocale is null. Database could not get table.");
                    locale = LocalizationSettings.SelectedLocaleAsync.Result;
                }
                useSelectedLocalePlaceholder = false;
            }

            // Do we have a cached operation already running?
            tableReference.Validate();
            var tableIdString = tableReference.ReferenceType == TableReference.Type.Guid ? TableReference.StringFromGuid(tableReference.TableCollectionNameGuid) : tableReference.TableCollectionName;
            var localeId = useSelectedLocalePlaceholder ? k_SelectedLocaleId : locale.Identifier;
            if (TableOperations.TryGetValue((localeId, tableIdString), out var operationHandle))
            {
                // Something has invalidated the handle, possibly caused by an unexpected release. We should remove it and try again.
                if (!operationHandle.IsValid())
                    TableOperations.Remove((localeId, tableIdString));
                else
                    return operationHandle;
            }

            // Start a new operation
            var operation = CreateLoadTableOperation();
            operation.Init(this, tableReference, locale);
            operation.Dependency = LocalizationSettings.InitializationOperation;
            var handle = AddressablesInterface.ResourceManager.StartOperation(operation, LocalizationSettings.InitializationOperation);

            if (useSelectedLocalePlaceholder || tableReference.ReferenceType == TableReference.Type.Guid)
            {
                // When using a Guid we increment the reference count.
                // We do not increment for placeholders as we only ever have 1 reference for them, we dont share it between
                // table name and guid, because the register operation will use the actual selected locale and not the placeholder.
                // We treat the table name as default and do not increment for that one.
                if (!useSelectedLocalePlaceholder)
                    AddressablesInterface.Acquire(handle);
                TableOperations[(localeId, tableIdString)] = handle;
            }
            else
            {
                // Register the table name and Guid
                RegisterTableNameOperation(handle, localeId, tableIdString);
            }

            // Register the table operation later. This will fully register everything including shared table data, table name and guid.
            RegisterCompletedTableOperation(handle);

            return handle;
        }

        public TTable GetTableARRealSync(TableReference tableReference, Locale locale) {
            // Do we have a cached operation already running?
            tableReference.Validate();
            var tableIdString = tableReference.ReferenceType == TableReference.Type.Guid ? TableReference.StringFromGuid(tableReference.TableCollectionNameGuid) : tableReference.TableCollectionName;
            if (TableOperations.TryGetValue((locale.Identifier, tableIdString), out var operationHandle)) {
                // Something has invalidated the handle, possibly caused by an unexpected release. We should remove it and try again.
                return operationHandle.IsValid() ? operationHandle.Result : null;
            }
            return null;
        }

        /// <summary>
        /// Returns the named table.
        /// Uses [WaitForCompletion](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.WaitForCompletion) to force the loading to complete synchronously.
        /// Please note that [WaitForCompletion](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.WaitForCompletion) is not supported on
        /// [WebGL](https://docs.unity3d.com/Packages/com.unity.addressables@latest/index.html?subfolder=/manual/SynchronousAddressables.html#webgl).
        /// </summary>
        /// <param name="tableReference">The table identifier. Can be either the name of the table or the table collection name Guid.</param>
        /// <param name="locale">The <see cref="Locale"/> to load the table from, use null to default to cref="LocalizationSettings.SelectedLocale"/>.</param>
        /// <returns></returns>
        public virtual TTable GetTable(TableReference tableReference, Locale locale = null) => GetTableAsync(tableReference, locale).WaitForCompletion();

        /// <summary>
        /// Preloads the selected table. If the table is an <see cref="AssetTable"/> its assets will also be loaded.
        /// Check [IsDone](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.IsDone) to see if the data is available,
        /// if it is false then you can use the [Completed](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Completed) event to get a callback when it is finished,
        /// yield on the operation or call [WaitForCompletion](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.WaitForCompletion)
        /// to force the operation to complete.
        /// </summary>
        /// <param name="tableReference">A reference to the table. A table reference can be either the name of the table or the table collection name Guid.</param>
        /// <param name="locale">The <see cref="Locale"/> to use instead of the default <see cref="LocalizationSettings.SelectedLocale"/></param>
        /// <returns></returns>
        public AsyncOperationHandle PreloadTables(TableReference tableReference, Locale locale = null)
        {
            // Start a new operation
            var operation = CreatePreloadTablesOperation();
            operation.Init(this, new[] { tableReference }, locale);
            operation.Dependency = LocalizationSettings.InitializationOperation;
            var handle = AddressablesInterface.ResourceManager.StartOperation(operation, LocalizationSettings.InitializationOperation);

            if (LocalizationSettings.Instance.IsPlaying)
                handle.CompletedTypeless += ReleaseNextFrame;

            return handle;
        }

        /// <summary>
        /// Preloads the matching tables for the selected Locale. If the tables are <see cref="AssetTable"/> then their assets will also be loaded.
        /// Check [IsDone](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.IsDone) to see if the data is available,
        /// if it is false then you can use the [Completed](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Completed) event to get a callback when it is finished,
        /// yield on the operation or call [WaitForCompletion](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.WaitForCompletion)
        /// to force the operation to complete.
        /// </summary>
        /// <param name="tableReferences">An IList of tableReferences to check for the string.</param>
        /// <param name="locale">The <see cref="Locale"/> to use instead of the default <see cref="LocalizationSettings.SelectedLocale"/></param>
        /// <returns></returns>
        /// <example>
        /// This shows how to manually preload tables instead of marking them as Preload in the editor.
        /// <code source="../../../DocCodeSamples.Tests/LocalizedStringDatabaseSamples.cs" region="preload-example"/>
        /// </example>
        public AsyncOperationHandle PreloadTables(IList<TableReference> tableReferences, Locale locale = null)
        {
            // Start a new operation
            var operation = CreatePreloadTablesOperation();
            operation.Init(this, tableReferences, locale);
            operation.Dependency = LocalizationSettings.InitializationOperation;
            var handle = AddressablesInterface.ResourceManager.StartOperation(operation, LocalizationSettings.InitializationOperation);

            if (LocalizationSettings.Instance.IsPlaying)
                handle.CompletedTypeless += ReleaseNextFrame;

            return handle;
        }

        /// <summary>
        /// Releases all tables that are currently loaded in the database.
        /// This will also release any references to the <see cref="SharedTableData"/> providing there are no other references to it, such as different Locale versions of the table that have been loaded.
        /// </summary>
        /// <param name="locale">The <see cref="Locale"/> to release tables for, when <see langword="null"/> all locales will be released.</param>
        public void ReleaseAllTables(Locale locale = null)
        {
            using (HashSetPool<TTable>.Get(out var releasedTables))
            {
                foreach (var to in TableOperations.Values)
                {
                    if (!to.IsValid())
                        continue;

                    if (locale != null && to.Result.LocaleIdentifier != locale.Identifier)
                        continue;

                    // We may have multiple references to the table so we keep track in order to only call release once.
                    if (to.Result != null && !releasedTables.Contains(to.Result))
                    {
                        ReleaseTableContents(to.Result);
                        releasedTables.Add(to.Result);
                    }
                    AddressablesInterface.Release(to);
                }
            }

            foreach (var shared in SharedTableDataOperations)
            {
                AddressablesInterface.SafeRelease(shared.Value);
            }
            SharedTableDataOperations.Clear();

            if (m_PreloadOperationHandle.IsValid())
            {
                //Debug.Assert(m_PreloadOperationHandle.IsDone, "Disposing an incomplete preload operation");

                if (m_PreloadOperationHandle.IsDone)
                    AddressablesInterface.Release(m_PreloadOperationHandle);
                m_PreloadOperationHandle = default;
            }

            TableOperations.Clear();
        }

        /// <summary>
        /// Releases all references to the table that matches the <paramref name="tableReference"/> and <paramref name="locale"/>.
        /// This will also release any references to the <see cref="SharedTableData"/> providing there are no other references to it, such as different Locale versions of the table that have been loaded.
        /// A table is released by calling <see cref="AddressableAssets.Addressables.Release"/> on it which decrements the ref-count.
        /// When a given Asset's ref-count is zero, that Asset is ready to be unloaded.
        /// For more information, read the Addressables section [on when memory is cleared](https://docs.unity3d.com/Packages/com.unity.addressables@latest/index.html?subfolder=/manual/MemoryManagement.html).
        /// </summary>
        /// <param name="tableReference">A reference to the table. A table reference can be either the name of the table or the table collection name Guid.</param>
        /// <param name="locale">The Locale version of the table that should be unloaded. When <see langword="null"/> the <see cref="LocalizationSettings.SelectedLocale"/> will be used.</param>
        /// <example>
        /// This shows how to release a table but prevent it from being unloaded.
        /// <code source="../../../DocCodeSamples.Tests/LocalizedStringDatabaseSamples.cs" region="release-example"/>
        /// </example>
        public void ReleaseTable(TableReference tableReference, Locale locale = null)
        {
            tableReference.Validate();
            var usingSelectedLocale = locale == LocalizationSettings.SelectedLocaleAsync.Result;
            if (locale == null)
            {
                locale = LocalizationSettings.SelectedLocaleAsync.Result;
                usingSelectedLocale = true;
                if (locale == null)
                    return;
            }

            // Get the shared table data
            SharedTableData sharedTableData;
            if (tableReference.ReferenceType == TableReference.Type.Guid)
            {
                if (!SharedTableDataOperations.TryGetValue(tableReference.TableCollectionNameGuid, out var sharedTableDataOperationHandle) || sharedTableDataOperationHandle.Result == null)
                    return;
                sharedTableData = sharedTableDataOperationHandle.Result;
            }
            else
            {
                var nameAndLocale = (locale.Identifier, tableReference.TableCollectionName);
                if (!TableOperations.TryGetValue(nameAndLocale, out var operationHandleName) || operationHandleName.Result == null)
                    return;
                sharedTableData = operationHandleName.Result.SharedData;
            }

            if (sharedTableData == null)
                return;

            // We may have multiple references to the table(Guid, Table name, placeholders etc) so we will iterate through and remove them all.
            // We also need to see if the Shared table data is still being used or if we can also release that.
            int sharedTableDataUsers = 0;
            bool removedContents = false;
            using (ListPool<(LocaleIdentifier localeIdentifier, string tableNameOrGuid)>.Get(out var itemsToRemove))
            {
                foreach (var tableOperation in TableOperations)
                {
                    if (!tableOperation.Value.IsValid() || tableOperation.Value.Result == null || tableOperation.Value.Result.SharedData != sharedTableData)
                        continue;

                    // Check locale and placeholder
                    if (tableOperation.Key.localeIdentifier == locale.Identifier || usingSelectedLocale && tableOperation.Key.localeIdentifier == k_SelectedLocaleId)
                    {
                        // We only want to do this once.
                        if (!removedContents)
                        {
                            ReleaseTableContents(tableOperation.Value.Result);
                            removedContents = true;
                        }

                        AddressablesInterface.SafeRelease(tableOperation.Value);

                        itemsToRemove.Add(tableOperation.Key);
                    }
                    else
                    {
                        sharedTableDataUsers++;
                    }
                }

                // Remove the items from the dictionary
                foreach (var tableKey in itemsToRemove)
                {
                    TableOperations.Remove(tableKey);
                }

                // If there's no other references to the shared table data then we can also remove that.
                if (sharedTableDataUsers == 0 && SharedTableDataOperations.TryGetValue(sharedTableData.TableCollectionNameGuid, out var sharedTableDataOperationHandle))
                {
                    AddressablesInterface.SafeRelease(sharedTableDataOperationHandle);
                    SharedTableDataOperations.Remove(sharedTableData.TableCollectionNameGuid);
                }
            }
        }

        /// <summary>
        /// Returns all the tables available.
        /// This method is asynchronous and may not have an immediate result.
        /// Check [IsDone](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.IsDone) to see if the tables are available.
        /// if it is false then you can use the [Completed](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Completed) event to get a callback when it is finished,
        /// yield on the operation or call [WaitForCompletion](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.WaitForCompletion)
        /// to force the operation to complete.```
        /// </summary>
        /// <param name="locale">The <see cref="Locale"/> to load the table from, use null to default to cref="LocalizationSettings.SelectedLocale"/>.</param>
        /// <returns></returns>
        /// <example>
        /// This example gets all the tables and waits for the completion of the operation.
        /// <code source="../../../DocCodeSamples.Tests/LocalizedStringDatabaseSamples.cs" region="get-all-tables-async"/>
        /// </example>
        /// <example>
        /// This example gets all tables and uses [WaitForCompletion](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.WaitForCompletion) to force the loading to complete synchronously.
        /// <code source="../../../DocCodeSamples.Tests/LocalizedStringDatabaseSamples.cs" region="get-all-tables"/>
        /// </example>
        public virtual AsyncOperationHandle<IList<TTable>> GetAllTables(Locale locale = null)
        {
            var operation = LoadAllTablesOperation<TTable, TEntry>.Pool.Get();
            operation.Init(this, locale);
            operation.Dependency = LocalizationSettings.InitializationOperation;
            var handle = AddressablesInterface.ResourceManager.StartOperation(operation, LocalizationSettings.InitializationOperation);

            if (LocalizationSettings.Instance.IsPlaying)
                handle.CompletedTypeless += ReleaseNextFrame;

            return handle;
        }

        /// <summary>
        /// Checks if the table is currently loaded or not.
        /// </summary>
        /// <param name="tableReference">The table identifier. Can be either the name of the table or the table collection name Guid.</param>
        /// <param name="locale">The <see cref="Locale"/> to load the table from, use null to default to cref="LocalizationSettings.SelectedLocale"/>.</param>
        /// <returns></returns>
        public virtual bool IsTableLoaded(TableReference tableReference, Locale locale = null)
        {
            var tableIdString = tableReference.ReferenceType == TableReference.Type.Guid ? TableReference.StringFromGuid(tableReference.TableCollectionNameGuid) : tableReference.TableCollectionName;
            var localeAndName = locale != null ? (locale.Identifier, tableIdString) : (LocalizationSettings.SelectedLocaleAsync.Result.Identifier, tableIdString);
            if (TableOperations.TryGetValue(localeAndName, out var TableOperationHandle))
                return TableOperationHandle.Status == AsyncOperationStatus.Succeeded;
            else
                return false;
        }

        internal virtual LoadTableOperation<TTable, TEntry> CreateLoadTableOperation() => LoadTableOperation<TTable, TEntry>.Pool.Get();
        internal virtual PreloadTablesOperation<TTable, TEntry> CreatePreloadTablesOperation() => PreloadTablesOperation<TTable, TEntry>.Pool.Get();

        /// <summary>
        /// Returns the entry from the requested table. A table entry will contain the localized item and metadata.
        /// This method is asynchronous and may not have an immediate result.
        /// Check [IsDone](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.IsDone) to see if the data is available,
        /// if it is false then you can use the [Completed](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Completed) event to get a callback when it is finished,
        /// yield on the operation or call [WaitForCompletion](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.WaitForCompletion)
        /// to force the operation to complete.
        /// Once the Completed event has been called, during the next update, the internal operation will be returned to a pool so that it can be reused.
        /// If you do plan to keep hold of the handle after completion then you should call [Acquire](xref::UnityEngine.ResourceManagement.AsyncOperationHandle.Acquire)
        /// to prevent the operation being reused and <see cref="AddressableAssets.Addressables.Release(AsyncOperationHandle)"/> to finally return the operation back to the pool.
        /// </summary>
        /// <remarks>
        /// Internally the following is performed when an Entry is requested.
        /// First the table will be requested using <see cref="GetTableAsync(TableReference, Locale)"/>.
        /// Once the table is loaded the entry will be extracted like so:
        /// ![](../manual/images/GetEntry.dot.svg)
        /// </remarks>
        /// <param name="tableReference">The table identifier. Can be either the name of the table or the table collection name Guid.</param>
        /// <param name="tableEntryReference">A reference to the entry in the table.</param>
        /// <param name="locale">The <see cref="Locale"/> to load the table from. Null will use <see cref="LocalizationSettings.SelectedLocale"/>.</param>
        /// <param name="fallbackBehavior">A Enum which determines if a Fallback should be used when no value could be found for the Locale.</param>
        /// <returns></returns>
        public virtual AsyncOperationHandle<TableEntryResult> GetTableEntryAsync(TableReference tableReference, TableEntryReference tableEntryReference, Locale locale = null, FallbackBehavior fallbackBehavior = FallbackBehavior.UseProjectSettings)
        {
            var loadTableOperation = GetTableAsync(tableReference, locale);
            var getTableEntryOperation = GetTableEntryOperation<TTable, TEntry>.Pool.Get();
            var useFallback = fallbackBehavior != FallbackBehavior.UseProjectSettings ? fallbackBehavior == FallbackBehavior.UseFallback : UseFallback;

            getTableEntryOperation.Init(this, loadTableOperation, tableReference, tableEntryReference, locale, useFallback, true);
            getTableEntryOperation.Dependency = loadTableOperation;
            var handle = AddressablesInterface.ResourceManager.StartOperation(getTableEntryOperation, loadTableOperation);

            return handle;
        }

        /// <summary>
        /// Returns the entry from the requested table. A table entry will contain the localized item and metadata.
        /// Uses [WaitForCompletion](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.WaitForCompletion) to force the loading to complete synchronously.
        /// Please note that [WaitForCompletion](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.WaitForCompletion) is not supported on
        /// [WebGL](https://docs.unity3d.com/Packages/com.unity.addressables@latest/index.html?subfolder=/manual/SynchronousAddressables.html#webgl).
        /// </summary>
        /// <param name="tableReference">The table identifier. Can be either the name of the table or the table collection name Guid.</param>
        /// <param name="tableEntryReference">A reference to the entry in the table.</param>
        /// <param name="locale">The <see cref="Locale"/> to load the table from. Null will use <see cref="LocalizationSettings.SelectedLocale"/>.</param>
        /// <param name="fallbackBehavior">A Enum which determines if a Fallback should be used when no value could be found for the Locale.</param>
        /// <returns>The table entry result which contains the table </returns>
        public virtual TableEntryResult GetTableEntry(TableReference tableReference, TableEntryReference tableEntryReference, Locale locale = null, FallbackBehavior fallbackBehavior = FallbackBehavior.UseProjectSettings)
        {
            return GetTableEntryAsync(tableReference, tableEntryReference, locale, fallbackBehavior).WaitForCompletion();
        }

        public TableEntryResult GetTableEntryARRealSync(TableReference tableReference, TableEntryReference tableEntryReference, Locale locale, FallbackBehavior fallbackBehavior = FallbackBehavior.UseProjectSettings) {
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                return GetTableEntry(tableReference, tableEntryReference, locale, fallbackBehavior);
            }
#endif
            
            var tTable = GetTableARRealSync(tableReference, locale);
            bool allowWaitForCompletionFallback = false;
            string log = null;
            
            if (tTable != null) {
                var tTableEntry = tTable.GetEntryFromReference(tableEntryReference);
#if UNITY_EDITOR
                bool validEntry = tTableEntry != null && !string.IsNullOrWhiteSpace(tTableEntry.LocalizedValue);
#else
                bool validEntry = tTableEntry != null; // In build empty entries are removed
#endif
                if (validEntry) {
                    // Found table and entry, best case scenario
                    return new TableEntryResult(tTableEntry, tTable);
                }

                var useFallback = fallbackBehavior != FallbackBehavior.UseProjectSettings ? fallbackBehavior == FallbackBehavior.UseFallback : UseFallback;
                if (useFallback) {
                    // If fallback allowed, let's try to find the fallback table synchronously first
                    tTable = GetTableARRealSync(tableReference, LocalizationSettings.ProjectLocale);
                    if (tTable != null) {
                        tTableEntry = tTable.GetEntryFromReference(tableEntryReference);
                        if (tTableEntry != null) {
                            // Found fallback table and entry in it
                            return new TableEntryResult(tTableEntry, tTable);
                        }
                    } else {
                        // Fallback table not found, we might need to load it using GetTableEntry method with WaitForCompletion
                        allowWaitForCompletionFallback = true;
                        log = $"GetTableEntryARRealSync failed to find FALLBACK table {tableReference.TableCollectionName} (for key {tableEntryReference.Key}), it will load the fallback table now (with WaitForCompletion).";
                    }
                }
            } else {
#if AR_DEBUG || DEBUG
                // Can't find table for selected language - this is possible only for OldLocs, so it's only for debug
                allowWaitForCompletionFallback = true;
                log = $"GetTableEntryARRealSync failed to find table {tableReference.TableCollectionName} (for key {tableEntryReference.Key}). Using the default method with WaitForCompletion.";
#endif
            }

            if (allowWaitForCompletionFallback) {
                Debug.LogError(log);
                return GetTableEntry(tableReference, tableEntryReference, locale, fallbackBehavior);
            }

            return default;
        }

        internal AsyncOperationHandle<SharedTableData> GetSharedTableData(Guid tableNameGuid)
        {
            if (SharedTableDataOperations.TryGetValue(tableNameGuid, out var sharedTableDataOp))
                return sharedTableDataOp;

            sharedTableDataOp = AddressablesInterface.LoadAssetFromGUID<SharedTableData>(TableReference.StringFromGuid(tableNameGuid));
            SharedTableDataOperations[tableNameGuid] = sharedTableDataOp;
            return sharedTableDataOp;
        }

        internal virtual void ReleaseTableContents(TTable table) {}

        /// <summary>
        /// Called before the LocaleChanged event is sent out in order to give the database a chance to prepare.
        /// </summary>
        /// <param name="locale"></param>
        public virtual void OnLocaleChanged(Locale locale)
        {
            ReleaseAllTables();
        }

        void PatchTableContents(AsyncOperationHandle<TTable> tableOperation)
        {
            // This should only be called once, after the table has loaded. It gives users the opurtunity to patch a Localized table.
            // For example you may want to read in some extra data from a csv file after the game has been built.
            if (TablePostprocessor != null && tableOperation.Result != null)
                TablePostprocessor.PostprocessTable(tableOperation.Result);
        }

        /// <summary>
        /// Resets the state of the provider by removing all the cached tables and clearing the preload operation.
        /// </summary>
        public void ResetState()
        {
            ReleaseAllTables();
        }

        /// <summary>
        /// Calls <see cref="ReleaseAllTables(Locale)"/>..
        /// </summary>
        void IDisposable.Dispose()
        {
            ReleaseAllTables();
        }
    }
}
