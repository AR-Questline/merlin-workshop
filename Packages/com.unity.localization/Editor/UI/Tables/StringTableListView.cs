using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace UnityEditor.Localization.UI
{
    class StringTableListView : GenericAssetTableListView<StringTable, StringTableTreeViewItem>
    {
        protected static readonly Color k_SelectedColor = new Color(0.37f, 0.41f, 0.7f);
        static readonly GUIContent k_WordWrap = EditorGUIUtility.TrTextContent("Wordwrap");
        static readonly GUIContent k_SmartStringIcon = EditorGUIUtility.TrTextContent("{S}", "Smart Formatting enabled");

        const float k_SmartStringIconWidth = 20;

        public StringTableListView(LocalizationTableCollection tableCollection) :
            base(tableCollection)
        {
        }

        protected override void UndoRedoPerformed()
        {
            rootItem.children.ForEach(i =>
            {
                if (i is AssetTableTreeViewItem tvi)
                {
                    tvi.RefreshFields();
                }
            });
            base.UndoRedoPerformed();
        }

        protected override IReadOnlyCollection<StringTable> GetCollectionTables() => (TableCollection as StringTableCollection).StringTables;

        protected override float GetCustomRowHeight(int row, TreeViewItem item)
        {
            float maxTextHeight = 0;
            if (item is StringTableTreeViewItem stringTableItem)
            {
                var visibleColumns = multiColumnHeader.state.visibleColumns;
                foreach (var colIdx in visibleColumns)
                {
                    maxTextHeight = Mathf.Max(maxTextHeight, stringTableItem.GetHeight(colIdx));
                }
            }

            var totalHeight = maxTextHeight + k_RowFooterHeightWithPadding + k_RowVerticalPadding;
            var baseHeight = base.GetCustomRowHeight(row, item);
            return Mathf.Max(totalHeight, baseHeight);
        }

        protected override void DrawItemField(Rect cellRect, int colIdx, TableColumn<StringTable> col, StringTableTreeViewItem item)
        {
            if (item.Draw(colIdx, cellRect, this))
                RefreshCustomRowHeights();
        }

        protected override void DrawItemFieldFooter(Rect cellRect, int colIdx, TableColumn<StringTable> col, StringTableTreeViewItem item)
        {
            base.DrawItemFieldFooter(cellRect, colIdx, col, item);

            // Smart string icon
            if (item.IsSmart(colIdx))
            {
                cellRect.y += k_RowVerticalPadding - 1;
                cellRect.x += cellRect.width - k_EntryMenuButtonWidth - k_SmartStringIconWidth;
                cellRect.width = k_SmartStringIconWidth;
                cellRect.height = 16;
                GUI.Label(cellRect, k_SmartStringIcon);
            }
        }

        internal protected override void PopulateEntryDropdown(GenericMenu menu, int colIdx, TableColumn<StringTable> col, StringTableTreeViewItem item)
        {
            base.PopulateEntryDropdown(menu, colIdx, col, item);

            menu.AddItem(k_WordWrap, StringTableTreeViewItem.WordWrap, () =>
            {
                StringTableTreeViewItem.WordWrap = !StringTableTreeViewItem.WordWrap;
            });

            var isSmartCol = item.IsSmart(colIdx);
            menu.AddItem(new GUIContent($"Smart String ({col.TableLocale})"), isSmartCol, () =>
            {
                item.SetSmart(colIdx, !isSmartCol);
            });

            var isSmartAll = item.AreAllSmart();
            menu.AddItem(new GUIContent("Smart String (All)"), isSmartAll, () =>
            {
                item.SetSmartAll(!isSmartAll);
            });

            RefreshCustomRowHeights();
        }
    }
}
