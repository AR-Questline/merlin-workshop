using System;
using System.Collections.Generic;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.Localization.Tables;
using UnityEngine.Pool;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine.Localization.Operations
{
    class GetLocalizedStringOperation : WaitForCurrentOperationAsyncOperationBase<string>
    {
        LocalizedStringDatabase m_Database;
        AsyncOperationHandle<LocalizedStringDatabase.TableEntryResult> m_TableEntryOperation;
        TableReference m_TableReference;
        TableEntryReference m_TableEntryReference;
        Locale m_SelectedLocale;
        IList<object> m_Arguments;
        IVariableGroup m_LocalVariables;
        bool m_AutoRelease;

        public static readonly ObjectPool<GetLocalizedStringOperation> Pool = new ObjectPool<GetLocalizedStringOperation>(
            () => new GetLocalizedStringOperation(), collectionCheck:false);

        public void Init(AsyncOperationHandle<LocalizedStringDatabase.TableEntryResult> tableEntryOperation, Locale locale, LocalizedStringDatabase database, TableReference tableReference, TableEntryReference tableEntryReference, IList<object> arguments, IVariableGroup localVariables, bool autoRelease)
        {
            m_TableEntryOperation = tableEntryOperation;
            m_SelectedLocale = locale;
            AddressablesInterface.Acquire(m_TableEntryOperation);
            m_Database = database;
            m_TableReference = tableReference;
            m_TableEntryReference = tableEntryReference;
            m_Arguments = arguments;
            m_LocalVariables = localVariables;
            m_AutoRelease = autoRelease;
        }

        protected override void Execute()
        {
            if (m_SelectedLocale == null)
            {
                m_SelectedLocale = LocalizationSettings.SelectedLocaleAsync.Result;
                if (m_SelectedLocale == null)
                {
                    CompleteAndRelease(null, false, "SelectedLocale is null. Could not get localized string.");
                    return;
                }
            }

            if (m_TableEntryOperation.Status != AsyncOperationStatus.Succeeded)
            {
                CompleteAndRelease(null, false, "Load Table Operation Failed");
                return;
            }

            try
            {
                var entry = m_TableEntryOperation.Result.Entry;
                var formatCache = entry?.GetOrCreateFormatCache();
                if (formatCache != null)
                    formatCache.LocalVariables = m_LocalVariables;

                var result = m_Database.GenerateLocalizedString(m_TableEntryOperation.Result.Table, entry, m_TableReference, m_TableEntryReference, m_SelectedLocale, m_Arguments);

                if (formatCache != null)
                    formatCache.LocalVariables = null;

                CompleteAndRelease(result, true, null);
            }
            catch (Exception e)
            {
                CompleteAndRelease(null, false, e.Message);
            }
        }

        public void CompleteAndRelease(string result, bool success, string errorMsg)
        {
            Complete(result, success, errorMsg);
            AddressablesInterface.SafeRelease(m_TableEntryOperation);

            if (m_AutoRelease && LocalizationSettings.Instance.IsPlaying)
            {
                // We need internal access for Handle here.
                LocalizationBehaviour.ReleaseNextFrame(Handle);
            }
        }

        protected override void Destroy()
        {
            base.Destroy();
            Pool.Release(this);
        }

        public override string ToString() => $"{GetType().Name}, Locale: {m_SelectedLocale}, Table: {m_TableReference}, Entry: {m_TableEntryReference}";
    }
}
