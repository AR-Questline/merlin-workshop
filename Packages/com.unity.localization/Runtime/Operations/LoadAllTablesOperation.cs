using System;
using System.Collections.Generic;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.Pool;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine.Localization.Operations
{
    class LoadAllTablesOperation<TTable, TEntry> : WaitForCurrentOperationAsyncOperationBase<IList<TTable>>
        where TTable : DetailedLocalizationTable<TEntry>
        where TEntry : TableEntry
    {
        readonly Action<AsyncOperationHandle<IList<TTable>>> m_LoadingCompletedAction;

        AsyncOperationHandle<IList<TTable>> m_AllTablesOperation;
        LocalizedDatabase<TTable, TEntry> m_Database;
        Locale m_SelectedLocale;

        public static readonly ObjectPool<LoadAllTablesOperation<TTable, TEntry>> Pool = new ObjectPool<LoadAllTablesOperation<TTable, TEntry>>(
            () => new LoadAllTablesOperation<TTable, TEntry>(), collectionCheck: false);

        public LoadAllTablesOperation()
        {
            m_LoadingCompletedAction = LoadingCompleted;
        }

        public void Init(LocalizedDatabase<TTable, TEntry> database, Locale locale)
        {
            m_Database = database;
            m_SelectedLocale = locale;
        }

        protected override void Execute()
        {
            if (m_SelectedLocale == null)
            {
                m_SelectedLocale = LocalizationSettings.SelectedLocale;
                if (m_SelectedLocale == null)
                {
                    Complete(null, false, "SelectedLocale is null. Could not load table.");
                    return;
                }
            }

            var label = m_SelectedLocale != null ? AddressHelper.FormatAssetLabel(m_SelectedLocale.Identifier) : AddressHelper.FormatAssetLabel(LocalizationSettings.SelectedLocaleAsync.Result.Identifier);
            m_AllTablesOperation = AddressablesInterface.LoadAssetsWithLabel<TTable>(label, null);

            if (m_AllTablesOperation.IsDone)
            {
                LoadingCompleted(m_AllTablesOperation);
                return;
            }

            m_AllTablesOperation.Completed += m_LoadingCompletedAction;
            CurrentOperation = m_AllTablesOperation;
        }

        void LoadingCompleted(AsyncOperationHandle<IList<TTable>> obj)
        {
            // Cache the loading operations so we can release on a per asset basis.
            if (obj.Result != null)
            {
                foreach (var table in obj.Result)
                {
                    if (table == null)
                        continue;

                    var tableOp = m_Database.GetTableAsync(table.TableCollectionName, m_SelectedLocale);
                    Debug.Assert(tableOp.IsDone);
                }
            }

            Complete(obj.Result, obj.Status == AsyncOperationStatus.Succeeded, obj.OperationException?.Message);
        }

        protected override void Destroy()
        {
            base.Destroy();
            Pool.Release(this);
            AddressablesInterface.ReleaseAndReset(ref m_AllTablesOperation);
        }
    }
}
