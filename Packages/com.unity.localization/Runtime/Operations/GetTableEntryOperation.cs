using System;
using System.Collections.Generic;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.Pool;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine.Localization.Operations
{
    class GetTableEntryOperation<TTable, TEntry> : WaitForCurrentOperationAsyncOperationBase<LocalizedDatabase<TTable, TEntry>.TableEntryResult>
        where TTable : DetailedLocalizationTable<TEntry>
        where TEntry : TableEntry
    {
        readonly Action<AsyncOperationHandle<TTable>> m_ExtractEntryFromTableAction;

        AsyncOperationHandle<TTable> m_LoadTableOperation;
        TableReference m_TableReference;
        TableEntryReference m_TableEntryReference;
        LocalizedDatabase<TTable, TEntry> m_Database;
        Locale m_SelectedLocale;
        Locale m_CurrentLocale;

        HashSet<Locale> m_HandledFallbacks;
        List<Locale> m_FallbackQueue;
        bool m_UseFallback;
        bool m_AutoRelease;

        public static readonly ObjectPool<GetTableEntryOperation<TTable, TEntry>> Pool = new ObjectPool<GetTableEntryOperation<TTable, TEntry>>(
            () => new GetTableEntryOperation<TTable, TEntry>(), collectionCheck: false);

        public GetTableEntryOperation()
        {
            m_ExtractEntryFromTableAction = ExtractEntryFromTable;
        }

        public void Init(LocalizedDatabase<TTable, TEntry> database, AsyncOperationHandle<TTable> loadTableOperation, TableReference tableReference, TableEntryReference tableEntryReference, Locale selectedLoale, bool UseFallBack, bool autoRelease)
        {
            m_Database = database;
            m_LoadTableOperation = loadTableOperation;
            AddressablesInterface.Acquire(m_LoadTableOperation);
            m_TableReference = tableReference;
            m_TableEntryReference = tableEntryReference;
            m_SelectedLocale = selectedLoale;
            m_UseFallback = UseFallBack;
            m_AutoRelease = autoRelease;
        }

        protected override void Execute()
        {
            var tableHandle = m_LoadTableOperation;
            m_LoadTableOperation = default; // Don't hold on to it because it may change due to entry override, fallback etc. The final result will be assigned here later.

            if (m_SelectedLocale == null)
            {
                m_SelectedLocale = LocalizationSettings.SelectedLocaleAsync.Result;
                if (m_SelectedLocale == null)
                {
                    CompleteAndRelease(default, false, "SelectedLocale is null. Could not get table entry.");
                    AddressablesInterface.SafeRelease(tableHandle);
                    return;
                }
            }

            m_CurrentLocale = m_SelectedLocale;
            ExtractEntryFromTable(tableHandle);
        }

        void ExtractEntryFromTable(AsyncOperationHandle<TTable> asyncOperation)
        {
            var entry = asyncOperation.Result?.GetEntryFromReference(m_TableEntryReference);

            if (HandleEntryOverride(asyncOperation, entry) || HandleFallback(asyncOperation, entry))
                return;

            m_LoadTableOperation = asyncOperation;
            CompleteAndRelease(new LocalizedDatabase<TTable, TEntry>.TableEntryResult(entry, asyncOperation.Result), true, null);
        }

        bool HandleEntryOverride(AsyncOperationHandle<TTable> asyncOperation, TEntry entry)
        {
            // First check for an Entry level override. This applies to Locale only.
            if (entry != null)
            {
                for (int i = 0; i < entry.MetadataEntries.Count; ++i)
                {
                    var md = entry.MetadataEntries[i];
                    if (md is IEntryOverride entryOverride)
                    {
                        if (ApplyEntryOverride(entryOverride, asyncOperation, entry))
                            return true;
                    }
                }
            }

            // Now check for a shared override. This applies to all Locales for this table entry.
            var sharedEntry = entry?.SharedEntry ?? asyncOperation.Result?.SharedData.GetEntryFromReference(m_TableEntryReference);
            if (sharedEntry != null)
            {
                for (int i = 0; i < sharedEntry.Metadata.MetadataEntries.Count; ++i)
                {
                    var md = sharedEntry.Metadata.MetadataEntries[i];
                    if (md is IEntryOverride entryOverride)
                    {
                        if (ApplyEntryOverride(entryOverride, asyncOperation, entry))
                            return true;
                    }
                }
            }

            return false;
        }

        bool ApplyEntryOverride(IEntryOverride entryOverride, AsyncOperationHandle<TTable> asyncOperation, TEntry entry)
        {
            if (entryOverride == null)
                return false;

            var overrideType = entryOverride.GetOverride(out var tableReference, out var tableEntry);
            if (overrideType == EntryOverrideType.None)
                return false;

            if (overrideType == EntryOverrideType.Entry)
            {
                // Swap the entry but keep the same table.
                m_TableEntryReference = tableEntry;

                // Start the process again with the new entry
                ExtractEntryFromTable(asyncOperation);
                return true;
            }

            if (overrideType == EntryOverrideType.Table)
            {
                var sharedEntry = entry?.SharedEntry ?? asyncOperation.Result?.SharedData.GetEntryFromReference(m_TableEntryReference);

                // Use the key as the id may not be the same in both tables
                m_TableEntryReference = sharedEntry.Key;
            }
            else if (overrideType == EntryOverrideType.TableAndEntry)
            {
                m_TableEntryReference = tableEntry;
            }

            AddressablesInterface.Release(asyncOperation);
            asyncOperation = m_Database.GetTableAsync(tableReference, m_CurrentLocale);
            AddressablesInterface.Acquire(asyncOperation);

            if (asyncOperation.IsDone)
            {
                ExtractEntryFromTable(asyncOperation);
            }
            else
            {
                CurrentOperation = asyncOperation;
                asyncOperation.Completed += m_ExtractEntryFromTableAction;
            }

            return true;
        }

        Locale GetNextFallback(Locale currentLocale)
        {
            if (m_FallbackQueue == null)
            {
                m_FallbackQueue = ListPool<Locale>.Get();
                m_HandledFallbacks = HashSetPool<Locale>.Get();
            }

            if (!m_HandledFallbacks.Contains(currentLocale))
                m_HandledFallbacks.Add(currentLocale);

            // Extract the fallbacks and add them to our queue.
            var fallbacks = currentLocale.GetFallbacks();
            if (fallbacks != null)
            {
                foreach (var fallback in fallbacks)
                {
                    if (!m_HandledFallbacks.Contains(fallback))
                    {
                        m_HandledFallbacks.Add(fallback);
                        m_FallbackQueue.Add(fallback);
                    }
                }
            }

            if (m_FallbackQueue.Count == 0)
                return null;

            // Return the next fallback
            var fb = m_FallbackQueue[0];
            m_FallbackQueue.RemoveAt(0);
            return fb;
        }

        bool HandleFallback(AsyncOperationHandle<TTable> asyncOperation, TEntry entry)
        {
            if ((entry == null || string.IsNullOrEmpty(entry.Data.Localized)) && m_UseFallback)
            {
                var fallbackLocale = GetNextFallback(m_CurrentLocale);
                if (fallbackLocale != null)
                {
                    m_CurrentLocale = fallbackLocale;
                    AddressablesInterface.Release(asyncOperation);

                    asyncOperation = m_Database.GetTableAsync(m_TableReference, m_CurrentLocale);
                    AddressablesInterface.Acquire(asyncOperation);

                    if (asyncOperation.IsDone)
                    {
                        ExtractEntryFromTable(asyncOperation);
                    }
                    else
                    {
                        CurrentOperation = asyncOperation;
                        asyncOperation.Completed += m_ExtractEntryFromTableAction;
                    }

                    return true;
                }
            }

            return false;
        }

        void CompleteAndRelease(LocalizedDatabase<TTable, TEntry>.TableEntryResult result, bool success, string errorMsg)
        {
            Complete(result, success, errorMsg);

            if (m_AutoRelease && LocalizationSettings.Instance.IsPlaying)
            {
                // We need internal access for Handle here.
                LocalizationBehaviour.ReleaseNextFrame(Handle);
            }
        }

        protected override void Destroy()
        {
            AddressablesInterface.SafeRelease(m_LoadTableOperation);
            m_LoadTableOperation = default;

            base.Destroy();
            Pool.Release(this);

            if (m_FallbackQueue != null)
            {
                ListPool<Locale>.Release(m_FallbackQueue);
                HashSetPool<Locale>.Release(m_HandledFallbacks);
                m_FallbackQueue = null;
                m_HandledFallbacks = null;
            }
        }

        public override string ToString() => $"{GetType().Name}, Current Locale: {m_CurrentLocale}, Selected Locale: {m_SelectedLocale}, Table: {m_TableReference}, Entry: {m_TableEntryReference}, Fallback: {m_UseFallback}";
    }
}
