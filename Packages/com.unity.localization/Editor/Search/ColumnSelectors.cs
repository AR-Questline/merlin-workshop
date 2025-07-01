#if ENABLE_SEARCH

using UnityEditor.Search;

namespace UnityEditor.Localization.Search
{
    // Used by TableSearchProvider, these are serialized into the Columns so need to remain static and not be changed.
    static class ColumnSelectors
    {
        public static object SelectTableEntry(SearchColumnEventArgs args)
        {
            if (!(args.item.data is TableEntrySearchData e))
                return null;
            return e.Entry.Key;
        }

        public static object SelectTableEntryId(SearchColumnEventArgs args)
        {
            if (!(args.item.data is TableEntrySearchData e))
                return null;
            return e.Entry.Id;
        }

        public static object SelectTableCollection(SearchColumnEventArgs args)
        {
            if (!(args.item.data is TableEntrySearchData e))
                return null;
            return e.Collection.TableCollectionName;
        }

        public static object SelectTableCollectionGroup(SearchColumnEventArgs args)
        {
            if (!(args.item.data is TableEntrySearchData e))
                return null;
            return e.Collection.Group;
        }
    }
}

#endif
