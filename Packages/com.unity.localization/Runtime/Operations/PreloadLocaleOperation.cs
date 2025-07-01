using System;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.Pool;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace UnityEngine.Localization.Operations
{
    /// <summary>
    /// Performs preloading for a single <see cref="Locale"/> in the following order:
    /// <list type="number">
    /// <item>BeginPreloading: Load preload resources - these are the tables that are marked with the preload tag.</item>
    /// <item>LoadTables: Load the tables using the resources, if any exist.</item>
    /// <item>LoadTableContents: Preload the tables contents, if a table implements <see cref="IPreloadRequired"/>.</item>
    /// </list>
    /// </summary>
    /// <typeparam name="TTable"></typeparam>
    /// <typeparam name="TEntry"></typeparam>
    class PreloadLocaleOperation<TTable, TEntry> : WaitForCurrentOperationAsyncOperationBase<LocalizedDatabase<TTable, TEntry>>
        where TTable : DetailedLocalizationTable<TEntry>
        where TEntry : TableEntry
    {
        readonly Action<AsyncOperationHandle<IList<IResourceLocation>>> m_LoadTablesAction;
        readonly Action<AsyncOperationHandle<TTable>> m_LoadTableContentsAction;
        readonly Action<AsyncOperationHandle> m_FinishPreloadingAction;
        readonly Action<AsyncOperationHandle<IList<AsyncOperationHandle>>> m_PreloadTablesCompletedAction;

        LocalizedDatabase<TTable, TEntry> m_Database;
        Locale m_Locale;
        AsyncOperationHandle<IList<IResourceLocation>> m_LoadResourcesOperation;
        AsyncOperationHandle<IList<AsyncOperationHandle>> m_LoadTablesGroupOperation;
        AsyncOperationHandle<IList<AsyncOperationHandle>> m_LoadTableContentsOperation;
        readonly List<AsyncOperationHandle> m_LoadTablesOperations = new List<AsyncOperationHandle>();
        readonly List<AsyncOperationHandle> m_PreloadTableContentsOperations = new List<AsyncOperationHandle>();
        readonly List<string> m_ResourceLabels = new List<string>();
        float m_Progress;

        protected override float Progress => m_Progress;

        protected override string DebugName => $"Preload ({m_Locale}) {m_Database.GetType()}";

        public static readonly ObjectPool<PreloadLocaleOperation<TTable, TEntry>> Pool = new ObjectPool<PreloadLocaleOperation<TTable, TEntry>>(
            () => new PreloadLocaleOperation<TTable, TEntry>(), collectionCheck: false);

        public PreloadLocaleOperation()
        {
            m_LoadTablesAction = LoadTables;
            m_LoadTableContentsAction = LoadTableContents;
            m_FinishPreloadingAction = FinishPreloading;
            m_PreloadTablesCompletedAction = PreloadTablesCompleted;
        }

        public void Init(LocalizedDatabase<TTable, TEntry> database, Locale locale)
        {
            Debug.Assert(locale != null);
            m_Database = database;
            m_Locale = locale;
            m_LoadTablesOperations.Clear();
            m_PreloadTableContentsOperations.Clear();
        }

        protected override void Execute()
        {
            BeginPreloading();
        }

        void BeginPreloading()
        {
            m_Progress = 0;
            var localeLabel = AddressHelper.FormatAssetLabel(m_Locale.Identifier);
            m_ResourceLabels.Clear();
            m_ResourceLabels.Add(localeLabel);
            m_ResourceLabels.Add(LocalizationSettings.PreloadLabel);
            m_LoadResourcesOperation = AddressablesInterface.LoadResourceLocationsWithLabelsAsync(m_ResourceLabels, Addressables.MergeMode.Intersection, typeof(TTable));

            if (!m_LoadResourcesOperation.IsValid())
            {
                CompleteAndRelease(true, null);
                return;
            }

            if (m_LoadResourcesOperation.IsDone)
            {
                LoadTables(m_LoadResourcesOperation);
            }
            else
            {
                CurrentOperation = m_LoadResourcesOperation;
                m_LoadResourcesOperation.Completed += m_LoadTablesAction;
            }
        }

        void LoadTables(AsyncOperationHandle<IList<IResourceLocation>> loadResourcesOperation)
        {
            if (loadResourcesOperation.Status != AsyncOperationStatus.Succeeded)
            {
                CompleteAndRelease(false, "Failed to locate preload tables for " + m_Locale);
                return;
            }

            // Do we need to preload any tables?
            if (loadResourcesOperation.Result.Count == 0)
            {
                m_Progress = 1;
                CompleteAndRelease(true, null);
                return;
            }

            // Load the tables
            foreach (var resourceLocation in loadResourcesOperation.Result)
            {
                var tableOperation = AddressablesInterface.LoadTableFromLocation<TTable>(resourceLocation);
                m_LoadTablesOperations.Add(tableOperation);

                if (tableOperation.IsDone)
                    LoadTableContents(tableOperation);
                else
                    tableOperation.Completed += m_LoadTableContentsAction;
            }

            m_LoadTablesGroupOperation = AddressablesInterface.CreateGroupOperation(m_LoadTablesOperations);
            if (m_LoadTablesGroupOperation.IsDone)
            {
                PreloadTablesCompleted(m_LoadTablesGroupOperation);
            }
            else
            {
                CurrentOperation = m_LoadTablesGroupOperation;
                m_LoadTablesGroupOperation.Completed += m_PreloadTablesCompletedAction;
            }
        }

        void LoadTableContents(AsyncOperationHandle<TTable> operation)
        {
            // Update progress.
            m_Progress += 1.0f / m_LoadTablesOperations.Count;

            if (operation.Result == null)
                return;

            var table = operation.Result;
            var tableCollectionName = table.TableCollectionName;

            if (m_Database.TableOperations.TryGetValue((table.LocaleIdentifier, tableCollectionName), out var tableOp))
            {
                // We need to release this operation but if we do it now it may cause errors in Addressables so we will defer it to later.
                LocalizationBehaviour.ReleaseNextFrame(operation);

                // If the operation is still loading then we can leave it to continue, no need to register this operation.
                if (tableOp.IsDone && !ReferenceEquals(tableOp.Result, table))
                {
                    Debug.LogError($"A table with the same key `{tableCollectionName}` already exists. Something went wrong during preloading of {m_Locale}. Table {table} does not match {tableOp.Result}.");
                    return;
                }
            }
            else
            {
                m_Database.RegisterCompletedTableOperation(operation);
            }

            if (table is IPreloadRequired preloadRequired)
            {
                var preloadOperation = preloadRequired.PreloadOperation;
                if (!preloadOperation.IsDone)
                {
                    m_PreloadTableContentsOperations.Add(preloadOperation);
                }
            }
        }

        void PreloadTablesCompleted(AsyncOperationHandle<IList<AsyncOperationHandle>> obj)
        {
            if (m_PreloadTableContentsOperations.Count == 0)
            {
                CompleteAndRelease(true, null);
                return;
            }

            m_LoadTableContentsOperation = AddressablesInterface.CreateGroupOperation(m_PreloadTableContentsOperations);
            if (m_LoadTableContentsOperation.IsDone)
            {
                FinishPreloading(m_LoadTableContentsOperation);
            }
            else
            {
                CurrentOperation = m_LoadTableContentsOperation;
                m_LoadTableContentsOperation.CompletedTypeless += m_FinishPreloadingAction;
            }
        }

        void FinishPreloading(AsyncOperationHandle op)
        {
            m_Progress = 1;
            CompleteAndRelease(op.Status == AsyncOperationStatus.Succeeded, null);
        }

        void CompleteAndRelease(bool success, string errorMsg)
        {
            AddressablesInterface.ReleaseAndReset(ref m_LoadResourcesOperation);
            AddressablesInterface.ReleaseAndReset(ref m_LoadTablesGroupOperation);
            AddressablesInterface.ReleaseAndReset(ref m_LoadTableContentsOperation);
            Complete(m_Database, success, errorMsg);
        }

        protected override void Destroy()
        {
            base.Destroy();
            Pool.Release(this);
        }
    }
}
