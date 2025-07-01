#if UNITY_EDITOR // We only need the original type in editor.

using System;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine.Localization
{
    public partial class LocalizedString : LocalizedReference
    {
        /// <summary>
        /// The current loading operation for the string when using <see cref="StringChanged"/> or null if one is not available.
        /// A string may not be immediately available, such as when loading the <see cref="StringTable"/> asset, so all string operations are wrapped
        /// with an <see cref="AsyncOperationHandle"/>.
        /// See also <seealso cref="RefreshString"/>
        /// </summary>
        [Obsolete("CurrentLoadingOperation is deprecated, use CurrentLoadingOperationHandle instead.", true)]
        public AsyncOperationHandle<LocalizedStringDatabase.TableEntryResult>? CurrentLoadingOperation => throw new NotSupportedException("CurrentLoadingOperation is obsolete. Please use CurrentLoadingOperationHandle instead.");

        [Obsolete("RegisterChangeHandler is obsolete. Please use the StringChanged event instead.", true)]
        public void RegisterChangeHandler(ChangeHandler handler)
        {
            throw new NotSupportedException("RegisterChangeHandler is obsolete. Please use the StringChanged event instead.");
        }

        [Obsolete("ClearChangeHandler is now obsolete. You should unsubscribe from the StringChanged event instead.", true)]
        public void ClearChangeHandler()
        {
            throw new NotSupportedException("ClearChangeHandler is now obsolete. You should use the StringChanged event instead.");
        }
    }
}
#endif
