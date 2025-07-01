using System;
using System.Collections.Generic;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.Pool;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine.Localization.Operations
{
    class PreloadDatabaseOperation<TTable, TEntry> : WaitForCurrentOperationAsyncOperationBase<LocalizedDatabase<TTable, TEntry>>
        where TTable : DetailedLocalizationTable<TEntry>
        where TEntry : TableEntry
    {
        readonly Action<AsyncOperationHandle> m_CompleteOperation;
        readonly Action<AsyncOperationHandle<IList<AsyncOperationHandle>>> m_CompleteGenericGroup;

        LocalizedDatabase<TTable, TEntry> m_Database;

        protected override float Progress => CurrentOperation.IsValid() ? CurrentOperation.PercentComplete : base.Progress;

        protected override string DebugName => $"Preload {m_Database.GetType()}";

        public static readonly ObjectPool<PreloadDatabaseOperation<TTable, TEntry>> Pool = new ObjectPool<PreloadDatabaseOperation<TTable, TEntry>>(
            () => new PreloadDatabaseOperation<TTable, TEntry>(), collectionCheck: false);

        public PreloadDatabaseOperation()
        {
            m_CompleteOperation = CompleteOperation;
            m_CompleteGenericGroup = CompleteGenericGroup;
        }

        public void Init(LocalizedDatabase<TTable, TEntry> database)
        {
            m_Database = database;
        }

        protected override void Execute()
        {
            var selectedLocale = LocalizationSettings.SelectedLocaleAsync;
            if (selectedLocale.Result == null)
            {
                Complete(m_Database, true, null);
                return;
            }

            switch (LocalizationSettings.PreloadBehavior)
            {
                case PreloadBehavior.NoPreloading:
                    Complete(m_Database, true, null);
                    break;

                case PreloadBehavior.PreloadSelectedLocale:
                    var preloadHandle = PreloadLocale(selectedLocale.Result);
                    if (preloadHandle.IsDone)
                    {
                        m_CompleteOperation(preloadHandle);
                    }
                    else
                    {
                        preloadHandle.Completed += m_CompleteOperation;
                        CurrentOperation = preloadHandle;
                    }
                    break;

                case PreloadBehavior.PreloadSelectedLocaleAndFallbacks:
                    using (HashSetPool<Locale>.Get(out var locales))
                    {
                        locales.Add(selectedLocale.Result);
                        GetAllFallbackLocales(selectedLocale.Result, locales);
                        PreloadLocales(locales);
                    }
                    break;

                case PreloadBehavior.PreloadAllLocales:
                    PreloadLocales(LocalizationSettings.AvailableLocales.Locales);
                    break;
            }
        }

        /// <summary>
        /// Recursively collects all fallback locales.
        /// </summary>
        /// <param name="current"></param>
        /// <param name="locales"></param>
        void GetAllFallbackLocales(Locale current, HashSet<Locale> locales)
        {
            foreach (var locale in current.GetFallbacks())
            {
                if (locales.Contains(locale))
                    continue;

                locales.Add(locale);
                GetAllFallbackLocales(locale, locales);
            }
        }

        AsyncOperationHandle PreloadLocale(Locale locale)
        {
            var operation = PreloadLocaleOperation<TTable, TEntry>.Pool.Get();
            operation.Init(m_Database, locale);
            return AddressablesInterface.ResourceManager.StartOperation(operation, default);
        }

        void PreloadLocales(ICollection<Locale> locales)
        {
            // CreateGenericGroupOperation creates a copy of the list so its safe for us to return it to the pool.
            using (ListPool<AsyncOperationHandle>.Get(out var preloadHandles))
            {
                foreach (var locale in locales)
                {
                    var preloadHandle = PreloadLocale(locale);
                    if (!preloadHandle.IsDone)
                        preloadHandles.Add(preloadHandle);
                }

                if (preloadHandles.Count > 0)
                {
                    var operation = AddressablesInterface.CreateGroupOperation(preloadHandles);
                    operation.Completed += m_CompleteGenericGroup;
                    CurrentOperation = operation;
                }
                else
                {
                    Complete(m_Database, true, null);
                }
            }
        }

        void CompleteOperation(AsyncOperationHandle operationHandle)
        {
            AddressablesInterface.Release(operationHandle);
            Complete(m_Database, true, null);
        }

        void CompleteGenericGroup(AsyncOperationHandle<IList<AsyncOperationHandle>> operationHandle)
        {
            AddressablesInterface.Release(operationHandle);
            Complete(m_Database, true, null);
        }

        protected override void Destroy()
        {
            base.Destroy();
            Pool.Release(this);
        }
    }
}
