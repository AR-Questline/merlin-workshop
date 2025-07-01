using System;
using System.Collections.Generic;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.Pool;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace UnityEngine.Localization.Operations
{
    class LoadTableOperation<TTable, TEntry> : WaitForCurrentOperationAsyncOperationBase<TTable>
        where TTable : DetailedLocalizationTable<TEntry>
        where TEntry : TableEntry
    {
        readonly Action<AsyncOperationHandle<SharedTableData>> m_LoadTableByGuidAction;
        readonly Action<AsyncOperationHandle<IList<IResourceLocation>>> m_LoadTableResourceAction;
        readonly Action<AsyncOperationHandle<TTable>> m_TableLoadedAction;
        readonly Action<AsyncOperationHandle<TTable>> m_CustomTableLoadedAction;

        LocalizedDatabase<TTable, TEntry> m_Database;
        TableReference m_TableReference;
        AsyncOperationHandle<TTable> m_LoadTableOperation;
        Locale m_SelectedLocale;
        string m_CollectionName;

        public static readonly ObjectPool<LoadTableOperation<TTable, TEntry>> Pool = new ObjectPool<LoadTableOperation<TTable, TEntry>>(
            () => new LoadTableOperation<TTable, TEntry>(), collectionCheck: false);

        public Action<AsyncOperationHandle<TTable>> RegisterTableOperation { get; private set; }

        public LoadTableOperation()
        {
            m_LoadTableByGuidAction = LoadTableByGuid;
            m_LoadTableResourceAction = LoadTableResource;
            m_TableLoadedAction = TableLoaded;
            m_CustomTableLoadedAction = CustomTableLoaded;
        }

        public void Init(LocalizedDatabase<TTable, TEntry> database, TableReference tableReference, Locale locale)
        {
            m_Database = database;
            m_TableReference = tableReference;
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

            // Extract the collection name
            if (m_TableReference.ReferenceType == TableReference.Type.Guid)
            {
                // We need to load the SharedTableData so we can resolve the name of the table
                var operation = m_Database.GetSharedTableData(m_TableReference.TableCollectionNameGuid);
                if (operation.IsDone)
                {
                    LoadTableByGuid(operation);
                }
                else
                {
                    CurrentOperation = operation;
                    operation.Completed += m_LoadTableByGuidAction;
                }
            }
            else
            {
                FindTableByName(m_TableReference.TableCollectionName);
            }
        }

        void LoadTableByGuid(AsyncOperationHandle<SharedTableData> operationHandle)
        {
            if (operationHandle.Status == AsyncOperationStatus.Succeeded)
            {
                FindTableByName(operationHandle.Result.TableCollectionName);
            }
            else
            {
                Complete(null, false, $"Failed to extract table name from shared table data {m_TableReference}. Load Shared Table data operation failed.");
            }
        }

        void FindTableByName(string collectionName)
        {
            m_CollectionName = collectionName;
            if (!TryLoadWithTableProvider())
                DefaultLoadTableByName();
        }

        bool TryLoadWithTableProvider()
        {
            // Check if the custom provider wants to provide this table
            if (m_Database.TableProvider != null)
            {
                m_LoadTableOperation = m_Database.TableProvider.ProvideTableAsync<TTable>(m_CollectionName, m_SelectedLocale);
                if (m_LoadTableOperation.IsValid())
                {
                    if (m_LoadTableOperation.IsDone)
                    {
                        CustomTableLoaded(m_LoadTableOperation);
                    }
                    else
                    {
                        m_LoadTableOperation.Completed += m_CustomTableLoadedAction;
                        CurrentOperation = m_LoadTableOperation;
                    }
                    return true;
                }
            }
            return false;
        }

        void DefaultLoadTableByName()
        {
            // Check the table exists
            var tableResourceOp = AddressablesInterface.LoadTableLocationsAsync(m_CollectionName, m_SelectedLocale.Identifier, typeof(TTable));
            if (tableResourceOp.IsDone)
            {
                LoadTableResource(tableResourceOp);
            }
            else
            {
                CurrentOperation = tableResourceOp;
                tableResourceOp.Completed += m_LoadTableResourceAction;
            }
        }

        void LoadTableResource(AsyncOperationHandle<IList<IResourceLocation>> operationHandle)
        {
            if (operationHandle.Status != AsyncOperationStatus.Succeeded || operationHandle.Result.Count == 0)
            {
                AddressablesInterface.Release(operationHandle);
                Complete(null, true, $"Could not find a {m_SelectedLocale} table with the name '{m_CollectionName}`");
                return;
            }

            m_LoadTableOperation = AddressablesInterface.LoadTableFromLocation<TTable>(operationHandle.Result[0]);
            if (m_LoadTableOperation.IsDone)
            {
                TableLoaded(m_LoadTableOperation);
            }
            else
            {
                CurrentOperation = m_LoadTableOperation;
                m_LoadTableOperation.Completed += m_TableLoadedAction;
            }

            AddressablesInterface.Release(operationHandle);
        }

        void CustomTableLoaded(AsyncOperationHandle<TTable> operationHandle)
        {
            if (operationHandle.Status == AsyncOperationStatus.Succeeded && operationHandle.Result != null)
                Complete(operationHandle.Result, true, null);
            else
                DefaultLoadTableByName();
        }

        void TableLoaded(AsyncOperationHandle<TTable> operationHandle)
        {
            Complete(operationHandle.Result, operationHandle.Status == AsyncOperationStatus.Succeeded, null);
        }

        protected override void Destroy()
        {
            base.Destroy();

            AddressablesInterface.ReleaseAndReset(ref m_LoadTableOperation);

            Pool.Release(this);
        }

        public override string ToString() => $"{GetType().Name}, Selected Locale: {m_SelectedLocale}, Table: {m_TableReference}";
    }
}
