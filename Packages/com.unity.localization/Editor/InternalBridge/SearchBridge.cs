#if ENABLE_SEARCH
using UnityEditor.Search;
using UnityEngine;

namespace UnityEditor.Localization.Bridge
{
    static class SearchBridge
    {
        public static SearchColumn CreateColumn(string path, string selector, string provider, GUIContent content, SearchColumnFlags options = SearchColumnFlags.Default)
        {
            return new SearchColumn(path, selector, provider, content, options);
        }
    }
}
#endif
