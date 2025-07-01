using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEditor.Localization.Bridge;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace UnityEditor.Localization.UI
{
    class GenericAssetTableListViewMultiColumnHeader<T1, T2> : MultiColumnHeaderBridge
        where T1 : LocalizationTable
        where T2 : GenericAssetTableTreeViewItem<T1>, new()
    {
        const float k_MetadataLabelWidth = 28;

        static readonly GUIContent k_MissingTableMenuItem = EditorGUIUtility.TrTextContent("Missing Tables");

        static readonly GUIContent k_MetadataIcon = EditorGUIUtility.TrTextContent(string.Empty, "Edit table metadata", EditorIcons.Metadata);

        LocalizationTableCollection TableCollection { get; set; }

        public GenericAssetTableListViewMultiColumnHeader(MultiColumnHeaderState state, GenericAssetTableListView<T1, T2> _, LocalizationTableCollection collection)
            : base(state)
        {
            TableCollection = collection;

            var visibleColumns = new List<int>();
            for (int i = 0; i < state.columns.Length; ++i)
            {
                if (state.columns[i] is VisibleColumn col && col.Visible)
                    visibleColumns.Add(i);
                else if (state.columns[i] is MissingTableColumn && MissingTableColumn.Visible)
                    visibleColumns.Add(i);
            }
            state.visibleColumns = visibleColumns.ToArray();
        }

        protected override void AddColumnHeaderContextMenuItems(GenericMenu menu)
        {
            menu.AddItem(EditorGUIUtility.TrTextContent("Resize to Fit"), false, ResizeToFit);

            menu.AddSeparator("");

            for (int i = 0; i < state.columns.Length; ++i)
            {
                var column = state.columns[i];

                var menuText = column.headerContent.text;
                if (column.allowToggleVisibility)
                    menu.AddItem(new GUIContent(menuText), state.visibleColumns.Contains(i), ToggleVisibilityCallback, i);
                else if (column is MissingTableColumn)
                {
                    menu.AddSeparator("");
                    menu.AddItem(k_MissingTableMenuItem, IsColumnVisible(i), ToggleMissingTableColumnsVisibility);
                    break;
                }
            }
        }

        void ToggleVisibilityCallback(object userData)
        {
            int colIdx = (int)userData;
            var visibleCol = GetColumn(colIdx) as VisibleColumn;
            if (visibleCol != null)
                visibleCol.Visible = !visibleCol.Visible;
            ToggleVisibility(colIdx);
        }

        void ToggleMissingTableColumnsVisibility()
        {
            var show = !MissingTableColumn.Visible;
            MissingTableColumn.Visible = show;
            for (int i = 0; i < state.columns.Length; ++i)
            {
                var column = state.columns[i];
                if (column is MissingTableColumn)
                {
                    if (show != IsColumnVisible(i))
                        ToggleVisibility(i);
                }
            }
        }

        protected override void ColumnHeaderGUI(MultiColumnHeaderState.Column column, Rect headerRect, int columnIndex)
        {
            switch (column)
            {
                case TableColumn<T1> tableCol:
                {
                    if (Event.current.rawType == EventType.Repaint && MetadataEditorWindow.IsSelectedForEditing(TableCollection, tableCol.TableLocale))
                    {
                        EditorIcons.SelectionRect.Draw(headerRect, string.Empty, false, false, false, false);
                    }

                    headerRect.width -= k_MetadataLabelWidth + 7;
                    var btnRect = new Rect(headerRect.xMax + 2, headerRect.y + 3, k_MetadataLabelWidth, 20);

                    if (GUI.Button(btnRect, k_MetadataIcon))
                    {
                        MetadataEditorWindow.ShowWindow(TableCollection, tableCol.TableLocale);
                    }
                    headerRect.xMin += 2;
                    base.ColumnHeaderGUI(column, headerRect, columnIndex);
                    break;
                }

                case VisibleColumn _:
                    base.ColumnHeaderGUI(column, headerRect, columnIndex);
                    break;

                case MissingTableColumn mtc:
                {
                    if (GUI.Button(headerRect, mtc.headerContent))
                    {
                        CreateMissingTable(mtc);
                    }
                    break;
                }
            }
        }

        void CreateMissingTable(MissingTableColumn mtc)
        {
            TableCollection.AddNewTable(mtc.TableLocale.Identifier);
        }
    }

    class VisibleColumn : MultiColumnHeaderState.Column
    {
        const string k_ColumnVisiblePref = "Localization-Table-Window-{0}";

        public bool defaultVisibility { get; set; } = true;

        public bool Visible
        {
            get => EditorPrefs.GetBool(string.Format(k_ColumnVisiblePref, headerContent.text), defaultVisibility);
            set => EditorPrefs.SetBool(string.Format(k_ColumnVisiblePref, headerContent.text), value);
        }
    }

    class TableColumn<T1> : VisibleColumn where T1 : LocalizationTable
    {
        public T1 Table { get; set; }
        public SerializedObject SerializedObjectTable { get; private set; }
        public SerializedObject SerializedObjectSharedTableData { get; private set; }

        public Locale TableLocale { get; set; }
        public bool Selected { get; set; }

        LocalizationTableCollection m_TableCollection;

        public TableColumn(LocalizationTableCollection collection, LocalizationTable table, Locale locale)
        {
            m_TableCollection = collection;
            minWidth = 100;
            Table = table as T1;
            SerializedObjectTable = new SerializedObject(Table);
            SerializedObjectSharedTableData = new SerializedObject(Table.SharedData);
            TableLocale = locale;
            headerContent = new GUIContent(TableLocale != null ? TableLocale.ToString() : table.LocaleIdentifier.Code);
            canSort = false;
            allowToggleVisibility = true;
        }

        public override string ToString() => "Table: " + Table;
    }

    class KeyColumn : VisibleColumn
    {
        public SharedTableData SharedData { get; }

        public KeyColumn(SharedTableData sharedData)
        {
            minWidth = 100;
            SharedData = sharedData;
            headerContent = new GUIContent("Key");
            headerTextAlignment = TextAlignment.Center;
            canSort = true;
            allowToggleVisibility = true;
        }

        public override string ToString() => "Key";
    }

    class KeyIdColumn : VisibleColumn
    {
        public SharedTableData Keys { get; }

        public KeyIdColumn(SharedTableData sharedData)
        {
            minWidth = 100;
            Keys = sharedData;
            headerContent = new GUIContent("Key Id");
            headerTextAlignment = TextAlignment.Center;
            canSort = true;
            defaultVisibility = false;
            allowToggleVisibility = true;
        }

        public override string ToString() => "Key Id";
    }

    class MissingTableColumn : MultiColumnHeaderState.Column
    {
        const string k_ColumnVisiblePref = "Localization-Table-Window-{0}";

        public Locale TableLocale { get; }

        public static bool Visible
        {
            get => EditorPrefs.GetBool(string.Format(k_ColumnVisiblePref, "Missing Tables"), false);
            set => EditorPrefs.SetBool(string.Format(k_ColumnVisiblePref, "Missing Tables"), value);
        }

        public MissingTableColumn(Locale locale)
        {
            minWidth = 200;
            TableLocale = locale;
            headerContent = new GUIContent("Add " + TableLocale);
            headerTextAlignment = TextAlignment.Center;
            canSort = false;
            allowToggleVisibility = false;
        }

        public override string ToString() => "Missing Table: " + TableLocale;
    }
}
