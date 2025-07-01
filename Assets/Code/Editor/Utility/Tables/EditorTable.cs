using System;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.Tables {
    public class EditorTable : TreeView {

        EditorTableHeader _header;
        TreeViewItem _root;

        public EditorTable(EditorTableHeader header, TreeViewItem root) : base(new TreeViewState(), header) {
            showBorder = true;
            _header = header;
            _root = root;
            Reload();
        }
        
        protected override TreeViewItem BuildRoot() {
            return _root;
        }

        protected override void RowGUI(RowGUIArgs args) {
            for (int i = 0; i < args.GetNumVisibleColumns(); ++i) {
                var column = _header.GetColumn(args.GetColumn(i)) as EditorTableColumn;
                if (column.ColumnType == EditorTableColumnType.Name) {
                    args.rowRect = args.GetCellRect(i);
                    base.RowGUI(args);
                } else {
                    column.DrawCell(args.GetCellRect(i), args.item);
                }
            }
        }

        public TreeViewItem FirstSelected() {
            var selection = GetSelection();
            return selection.Any() ? FindItem(selection[0], _root) : null;
        }
    }

    public class EditorTableColumn : MultiColumnHeaderState.Column {
        public EditorTableColumnType ColumnType { get; }
        Action<Rect, TreeViewItem> _onDraw;
        
        EditorTableColumn(string name, float width, EditorTableColumnType columnType, Action<Rect, TreeViewItem> onDraw=null) {
            headerContent = new GUIContent(name);
            ColumnType = columnType;
            this.width = width;
            _onDraw = onDraw;
        }

        public void DrawCell(Rect rect, TreeViewItem item) {
            _onDraw?.Invoke(rect, item);
        }

        
        /// <summary>
        /// Special column that contains name of table element.
        /// Its drawing is implemented in EditorTable.RowGUI.
        /// </summary>
        public static EditorTableColumn Name(string name, float width) => new(name, width, EditorTableColumnType.Name);

        /// <summary>
        /// Column that draw custom cells of table element.
        /// Its drawing is implemented in onDraw action.
        /// </summary>
        public static EditorTableColumn Custom(string name, float width, Action<Rect, TreeViewItem> onDraw) => new(name, width, EditorTableColumnType.Custom, onDraw);
    }
    
    public enum EditorTableColumnType {
        Name,
        Custom,
    }
    
    public class EditorTableHeader : MultiColumnHeader {
        public EditorTableHeader(params EditorTableColumn[] columns) : base(new MultiColumnHeaderState(columns)) { }
    }
}