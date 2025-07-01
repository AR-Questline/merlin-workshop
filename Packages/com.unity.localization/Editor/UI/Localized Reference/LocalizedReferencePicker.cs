#if ENABLE_SEARCH

using UnityEditor.Localization.Search;
using UnityEditor.Search;
using UnityEngine.Localization.Tables;
using UnityEngine.Search;

namespace UnityEditor.Localization.UI
{
    class LocalizedReferencePicker<TCollection> where TCollection : LocalizationTableCollection
    {
        readonly SearchContext m_SearchContext;
        readonly string m_Title;
        readonly int m_UndoGroup;

        ISearchView m_View;
        bool m_Revert = true;

        SerializedTableReference m_Table;
        SerializedTableEntryReference m_Entry;

        public LocalizedReferencePicker(SearchContext context, string title, SerializedProperty tableProperty, SerializedProperty entryProperty)
        {
            m_SearchContext = context;
            m_Title = title;
            m_Table = new SerializedTableReference(tableProperty);
            m_Entry = new SerializedTableEntryReference(entryProperty);

            Undo.IncrementCurrentGroup();
            m_UndoGroup = Undo.GetCurrentGroup();
            Selection.selectionChanged += CloseNoRevert; // Close if Undo changes the selection.
        }

        void CloseNoRevert()
        {
            m_Revert = false;
            m_View?.Close();
        }

        public void Show()
        {
            #if UNITY_2022_2_OR_NEWER
            var state = new SearchViewState(m_SearchContext)
            {
                title = m_Title,
                ignoreSaveSearches = true,
                hideAllGroup = false,
                queryBuilderEnabled = true,
                hideTabs = true,
                flags = SearchViewFlags.DisableInspectorPreview | SearchViewFlags.OpenInBuilderMode,
                selectHandler = Select,
                trackingHandler = Track
            };
            m_View = UnityEditor.Search.SearchService.ShowPicker(state);
            #else
            var state = new SearchViewState(m_SearchContext) { title = m_Title };
            m_View = UnityEditor.Search.SearchService.ShowPicker(m_SearchContext, Select, Track, null, null, m_Title);
            #endif
        }

        void Select(SearchItem item, bool cancelled)
        {
            if (cancelled)
            {
                if (m_Revert)
                    Undo.RevertAllDownToGroup(m_UndoGroup);
            }
            else
            {
                // Apply selection
                var selection = (TableEntrySearchData)item.data;
                SetItem(selection?.Collection, selection?.Entry);
                Undo.CollapseUndoOperations(m_UndoGroup);
            }
            m_View = null;
            Selection.selectionChanged -= CloseNoRevert;
        }

        void Track(SearchItem item)
        {
            var selection = (TableEntrySearchData)item.data;
            SetItem(selection?.Collection, selection?.Entry);
        }

        void SetItem(LocalizationTableCollection collection, SharedTableData.SharedTableEntry entry)
        {
            m_Entry.SetReference(entry);
            m_Table.SetReference(collection);
            m_Table.TableNameProperty.serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif
