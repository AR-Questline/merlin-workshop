using System;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine.Localization
{
    public abstract partial class LocalizedTable<TTable, TEntry>
        #if UNITY_EDITOR
        : ISerializationCallbackReceiver
        #endif
        where TTable : DetailedLocalizationTable<TEntry>
        where TEntry : TableEntry
    {
        /// <summary>
        /// The current loading operation for the table when using <see cref="TableChanged"/> or null if one is not available.
        /// </summary>
        [Obsolete("CurrentLoadingOperation is deprecated, use CurrentLoadingOperationHandle instead.")]
        public AsyncOperationHandle<TTable>? CurrentLoadingOperation => CurrentLoadingOperationHandle.IsValid() ? CurrentLoadingOperationHandle : default;
    }
}
