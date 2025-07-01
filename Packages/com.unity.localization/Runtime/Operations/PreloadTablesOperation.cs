using System;
using System.Collections.Generic;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.Pool;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine.Localization.Operations
{
    class PreloadTablesOperation<TTable, TEntry> : WaitForCurrentOperationAsyncOperationBase<LocalizedDatabase<TTable, TEntry>>
        where TTable : DetailedLocalizationTable<TEntry>
        where TEntry : TableEntry
    {
        LocalizedDatabase<TTable, TEntry> m_Database;
        readonly List<AsyncOperationHandle<TTable>> m_LoadTables = new List<AsyncOperationHandle<TTable>>();
        readonly List<AsyncOperationHandle> m_LoadTablesOperation = new List<AsyncOperationHandle>();
        readonly List<AsyncOperationHandle> m_PreloadTablesOperations = new List<AsyncOperationHandle>();
        readonly Action<AsyncOperationHandle<IList<AsyncOperationHandle>>> m_LoadTableContentsAction;
        readonly Action<AsyncOperationHandle> m_FinishPreloadingAction;

        AsyncOperationHandle<IList<AsyncOperationHandle>> m_LoadTablesOperationHandle;
        AsyncOperationHandle<IList<AsyncOperationHandle>> m_PreloadTablesContentsHandle;
        IList<TableReference> m_TableReferences;
        Locale m_SelectedLocale;

        public static readonly ObjectPool<PreloadTablesOperation<TTable, TEntry>> Pool = new ObjectPool<PreloadTablesOperation<TTable, TEntry>>(
            () => new PreloadTablesOperation<TTable, TEntry>(), collectionCheck: false);

        public PreloadTablesOperation()
        {
            m_LoadTableContentsAction = a =>
            {
                LoadTableContents();
                AddressablesInterface.Release(a);
            };
            m_FinishPreloadingAction = FinishPreloading;
        }

        public void Init(LocalizedDatabase<TTable, TEntry> database, IList<TableReference> tableReference, Locale locale = null)
        {
            m_Database = database;
            m_TableReferences = tableReference;
            m_SelectedLocale = locale;
        }

        protected override void Execute()
        {
            BeginPreloadingTables();
        }

        void BeginPreloadingTables()
        {
            foreach (var tableReference in m_TableReferences)
            {
                var table = m_Database.GetTableAsync(tableReference, m_SelectedLocale);
                m_LoadTables.Add(table);

                if (!table.IsDone)
                {
                    m_LoadTablesOperation.Add(table);
                }
            }

            if (m_LoadTablesOperation.Count > 0)
            {
                m_LoadTablesOperationHandle = AddressablesInterface.CreateGroupOperation(m_LoadTablesOperation);
                if (!m_LoadTablesOperationHandle.IsDone)
                {
                    CurrentOperation = m_LoadTablesOperationHandle;
                    m_LoadTablesOperationHandle.Completed += m_LoadTableContentsAction;
                    return;
                }
            }

            LoadTableContents();
        }

        void LoadTableContents()
        {
            // Iterate through the loaded tables, add them to our known tables and preload the actual table contents if required.
            foreach (var table in m_LoadTables)
            {
                if (table.Result == null)
                {
                    Complete(null, false, "Table is null.");
                    return;
                }

                if (table.Result is IPreloadRequired preloadRequired)
                {
                    m_PreloadTablesOperations.Add(preloadRequired.PreloadOperation);
                }
            }

            if (m_PreloadTablesOperations.Count == 0)
            {
                Complete(m_Database, true, null);
                return;
            }

            m_PreloadTablesContentsHandle = AddressablesInterface.CreateGroupOperation(m_PreloadTablesOperations);
            if (!m_PreloadTablesContentsHandle.IsDone)
            {
                CurrentOperation = m_PreloadTablesContentsHandle;
                m_PreloadTablesContentsHandle.CompletedTypeless += m_FinishPreloadingAction;
            }
            else
            {
                FinishPreloading(m_PreloadTablesContentsHandle);
            }
        }

        void FinishPreloading(AsyncOperationHandle op)
        {
            Complete(m_Database, op.Status == AsyncOperationStatus.Succeeded, null);
        }

        protected override void Destroy()
        {
            base.Destroy();
            AddressablesInterface.ReleaseAndReset(ref m_LoadTablesOperationHandle);
            AddressablesInterface.ReleaseAndReset(ref m_PreloadTablesContentsHandle);

            m_LoadTables.Clear();
            m_LoadTablesOperation.Clear();
            m_PreloadTablesOperations.Clear();
            m_TableReferences = null;
            Pool.Release(this);
        }
    }
}
