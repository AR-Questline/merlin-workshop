using System;
using UnityEngine.Localization.Operations;
using UnityEngine.Localization.Tables;
using UnityEngine.Pool;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine.Localization.Settings
{
    /// <summary>
    /// The Localized Asset Database provides a single point of access for all localized assets.
    /// </summary>
    /// <remarks>
    /// A localized asset must derive from <see cref="Object"/>.
    /// </remarks>
    [Serializable]
    public class LocalizedAssetDatabase : LocalizedDatabase<AssetTable, AssetTableEntry>
    {
        /// <summary>
        /// Returns a handle to a localized asset loading operation from the <see cref="LocalizedDatabase{TTable, TEntry}.DefaultTable"/>.
        /// This method is asynchronous and may not have an immediate result.
        /// Check [IsDone](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.IsDone) to see if the data is available,
        /// if it is false then you can use the [Completed](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Completed) event to get a callback when it is finished,
        /// yield on the operation or call [WaitForCompletion](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.WaitForCompletion)
        /// to force the operation to complete.
        /// Once the Completed event has been called, during the next update, the internal operation will be returned to a pool so that it can be reused.
        /// If you do plan to keep hold of the handle after completion then you should call [Acquire](xref::UnityEngine.ResourceManagement.AsyncOperationHandle.Acquire)
        /// to prevent the operation being reused and <see cref="AddressableAssets.Addressables.Release(AsyncOperationHandle)"/> to finally return the operation back to the pool.
        /// </summary>
        /// <typeparam name="TObject">The type of asset that should be loaded.</typeparam>
        /// <param name="tableEntryReference">A reference to the entry in the <see cref="LocalizedDatabase{TTable, TEntry}.DefaultTable"/></param>
        /// <param name="locale">The <see cref="Locale"/> to load the table from. Null will use <see cref="LocalizationSettings.SelectedLocale"/>.</param>
        /// <param name="fallbackBehavior">Determines if a fallback should be used when no value could be found for the Locale.</param>
        /// <returns></returns>
        public AsyncOperationHandle<TObject> GetLocalizedAssetAsync<TObject>(TableEntryReference tableEntryReference, Locale locale = null, FallbackBehavior fallbackBehavior = FallbackBehavior.UseProjectSettings) where TObject : Object
        {
            return GetLocalizedAssetAsync<TObject>(GetDefaultTable(), tableEntryReference, locale, fallbackBehavior);
        }

        /// <summary>
        /// Returns a localized asset from the <see cref="LocalizedDatabase{TTable, TEntry}.DefaultTable"/>.
        /// </summary>
        /// <typeparam name="TObject">The type of asset that should be loaded.</typeparam>
        /// <param name="tableEntryReference">A reference to the entry in the <see cref="LocalizedDatabase{TTable, TEntry}.DefaultTable"/></param>
        /// <param name="locale">The <see cref="Locale"/> to load the table from. Null will use <see cref="LocalizationSettings.SelectedLocale"/>.</param>
        /// <returns></returns>
        public TObject GetLocalizedAsset<TObject>(TableEntryReference tableEntryReference, Locale locale = null) where TObject : Object
        {
            return GetLocalizedAssetAsync<TObject>(GetDefaultTable(), tableEntryReference, locale).WaitForCompletion();
        }

        /// <summary>
        /// Returns a handle to a localized asset loading operation from the requested table.
        /// This method is asynchronous and may not have an immediate result.
        /// Check [IsDone](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.IsDone) to see if the data is available,
        /// if it is false then you can use the [Completed](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Completed) event to get a callback when it is finished,
        /// yield on the operation or call [WaitForCompletion](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.WaitForCompletion)
        /// to force the operation to complete.
        /// Once the Completed event has been called, during the next update, the internal operation will be returned to a pool so that it can be reused.
        /// If you do plan to keep hold of the handle after completion then you should call [Acquire](xref::UnityEngine.ResourceManagement.AsyncOperationHandle.Acquire)
        /// to prevent the operation being reused and <see cref="AddressableAssets.Addressables.Release(AsyncOperationHandle)"/> to finally return the operation back to the pool.
        /// </summary>
        /// <typeparam name="TObject">The type of asset that should be loaded.</typeparam>
        /// <param name="tableReference">A reference to the table that the asset should be loaded from.</param>
        /// <param name="tableEntryReference">A reference to the entry in the table.</param>
        /// <param name="locale">The <see cref="Locale"/> to load the table from. Null will use <see cref="LocalizationSettings.SelectedLocale"/>.</param>
        /// <param name="fallbackBehavior">Determines if a fallback should be used when no value could be found for the Locale.</param>
        /// <returns></returns>
        public virtual AsyncOperationHandle<TObject> GetLocalizedAssetAsync<TObject>(TableReference tableReference, TableEntryReference tableEntryReference, Locale locale = null, FallbackBehavior fallbackBehavior = FallbackBehavior.UseProjectSettings) where TObject : Object
        {
            return GetLocalizedAssetAsyncInternal<TObject>(tableReference, tableEntryReference, locale, fallbackBehavior);
        }

        /// <summary>
        /// Returns a localized asset from the requested table.
        /// </summary>
        /// <typeparam name="TObject">The type of asset that should be loaded.</typeparam>
        /// <param name="tableReference">A reference to the table that the asset should be loaded from.</param>
        /// <param name="tableEntryReference">A reference to the entry in the table.</param>
        /// <param name="locale">The <see cref="Locale"/> to load the table from. Null will use <see cref="LocalizationSettings.SelectedLocale"/>.</param>
        /// <returns></returns>
        public virtual TObject GetLocalizedAsset<TObject>(TableReference tableReference, TableEntryReference tableEntryReference, Locale locale = null) where TObject : Object
        {
            return GetLocalizedAssetAsyncInternal<TObject>(tableReference, tableEntryReference, locale).WaitForCompletion();
        }

        /// <summary>
        /// Implementation for all versions of <see cref="GetLocalizedAssetAsync"/>.
        /// </summary>
        /// <typeparam name="TObject">The type of asset that should be loaded.</typeparam>
        /// <param name="tableReference">A reference to the table that the asset should be loaded from.</param>
        /// <param name="tableEntryReference">A reference to the entry in the table.</param>
        /// <param name="locale">The <see cref="Locale"/> to use instead of the default <see cref="LocalizationSettings.SelectedLocale"/></param>
        /// <param name="fallbackBehavior">Determines if a fallback should be used when no value could be found for the Locale.</param>
        /// <returns></returns>
        protected virtual AsyncOperationHandle<TObject> GetLocalizedAssetAsyncInternal<TObject>(TableReference tableReference, TableEntryReference tableEntryReference, Locale locale, FallbackBehavior fallbackBehavior = FallbackBehavior.UseProjectSettings) where TObject : Object
        {
            var tableEntryOperation = GetTableEntryAsync(tableReference, tableEntryReference, locale, fallbackBehavior);

            var operation = LoadAssetOperation<TObject>.Pool.Get();
            operation.Init(tableEntryOperation, true);
            operation.Dependency = tableEntryOperation;
            var handle = AddressablesInterface.ResourceManager.StartOperation(operation, tableEntryOperation);

            return handle;
        }

        internal override void ReleaseTableContents(AssetTable table) => table.ReleaseAssets();
    }
}
