#if UNITY_EDITOR // We only need the original type in editor.

using System;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine.Localization
{
    public partial class LocalizedAsset<TObject> : LocalizedAssetBase where TObject : Object
    {
        /// <summary>
        /// The current loading operation for the asset when using <see cref="AssetChanged"/>. This is <see langword="null"/> if a loading operation is not available.
        /// </summary>
        [Obsolete("CurrentLoadingOperation is deprecated, use CurrentLoadingOperationHandle instead.", true)]
        public AsyncOperationHandle<TObject>? CurrentLoadingOperation => throw new NotSupportedException("CurrentLoadingOperation is obsolete. Please use CurrentLoadingOperationHandle instead.");

        [Obsolete("RegisterChangeHandler is obsolete. Please use the AssetChanged event instead.", true)]
        public void RegisterChangeHandler(ChangeHandler handler)
        {
            throw new NotSupportedException("RegisterChangeHandler is obsolete. Please use the AssetChanged event instead.");
        }

        [Obsolete("ClearChangeHandler is now obsolete. You should unsubscribe from the AssetChanged event instead.", true)]
        public void ClearChangeHandler()
        {
            throw new NotSupportedException("ClearChangeHandler is now obsolete. You should use the AssetChanged event instead.");
        }
    }
}
#endif
