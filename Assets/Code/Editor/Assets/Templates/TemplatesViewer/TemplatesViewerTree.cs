using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Assets.Templates.TemplatesViewer.Columns.ColumnTypes;
using Awaken.TG.Main.Templates;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Awaken.TG.Editor.Assets.Templates.TemplatesViewer {
    public class TemplatesViewerTree : TreeView {

        public TreeViewItem Root { get; private set; }
        IEnumerable<ITemplate> _templates;
        bool _initialSort;
        TemplatesViewerConfig _config;
        GUIStyle _errorStyle;

        public TemplatesViewerTree(TreeViewState treeViewState, MultiColumnHeader multiColumnHeader, IEnumerable<ITemplate> templates, TemplatesViewerConfig config)
            : base(treeViewState, multiColumnHeader) {
            _templates = templates;
            _config = config;
            _errorStyle = new GUIStyle(EditorStyles.label);
            _errorStyle.normal.textColor = Color.red;
            
            multiColumnHeader.height = 25;
            showBorder = true;
            showAlternatingRowBackgrounds = true;
            
            multiColumnHeader.sortingChanged += OnSortingChanged;

            Reload();
        }

        protected override TreeViewItem BuildRoot () {
            Root = new TreeViewItem(-1, -1, "root");

            int id = 0;
            foreach (ITemplate template in _templates) {
                Root.AddChild(new TemplatesViewerTreeItem(id++,0, template));
            }

            if (!Root.hasChildren) {
                Root.AddChild(TemplateViewItem.EmptyItem);
            }

            SetupDepthsFromParentsAndChildren (Root);

            return Root;
        }
        
        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
	        var rows = base.BuildRows(root);
	        if (!_initialSort) {
		        _initialSort = true;
				SortIfNeeded(rows);
	        }
	        return rows;
        }

        protected override bool DoesItemMatchSearch(TreeViewItem item, string search) {
	        if (item is TemplatesViewerTreeItem) {
		        if (base.DoesItemMatchSearch(item, search)) {
			        return true;
		        }
	        }
	        return false;
        }

        protected override void RowGUI(RowGUIArgs args) {
            for (int i = 0; i < args.GetNumVisibleColumns (); ++i)
            {
                CellGUI(args.GetCellRect(i), args.item, args.GetColumn(i), ref args);
            }
        }
        
        void CellGUI (Rect cellRect, TreeViewItem item, int columnIndex, ref RowGUIArgs args) {
            //CenterRectUsingSingleLineHeight(ref cellRect);

            if (item is TemplatesViewerTreeItem templateItem) {
	            try {
		            int dataIndex = multiColumnHeader.GetColumn(columnIndex).userData;
		            TemplatesViewerColumn columnData = _config.GetColumn(dataIndex);
		            columnData.DrawCell(cellRect, templateItem);
	            } catch (Exception e) {
		            EditorGUI.LabelField(cellRect, e.Message, _errorStyle);
	            }
            }
        }

        protected override float GetCustomRowHeight(int row, TreeViewItem item) {
            
            float height = base.GetCustomRowHeight(row, item);

            if (item is TemplatesViewerTreeItem templatesItem) {
	            foreach (int columnIndex in multiColumnHeader.state.visibleColumns) {
		            int dataIndex = multiColumnHeader.GetColumn(columnIndex).userData;
		            TemplatesViewerColumn columnData = _config.GetColumn(dataIndex);
		            float columnHeight = columnData.GetRowHeight(templatesItem);
		            if (columnHeight > height) {
			            height = columnHeight;
		            }
	            }
            }

            return height;
        }
        
        void OnSortingChanged (MultiColumnHeader multiColumnHeader)
		{
			SortIfNeeded(GetRows());
		}

		void SortIfNeeded(IList<TreeViewItem> rows)
		{
			if (rows.Count <= 1)
				return;
			
			if (multiColumnHeader.sortedColumnIndex == -1)
			{
				return;
			}

			SortByMultipleColumns ();
			Repaint();
		}

		void SortByMultipleColumns ()
		{
			var sortedColumns = multiColumnHeader.state.sortedColumns;

			if (sortedColumns.Length == 0)
				return;

			var items = rootItem.children.Cast<TemplatesViewerTreeItem>();
			var orderedQuery = InitialOrder (items, sortedColumns);
			for (int i=1; i<sortedColumns.Length; i++)
			{
				bool ascending = multiColumnHeader.IsSortedAscending(sortedColumns[i]);
				int columnDataIndex = multiColumnHeader.GetColumn(sortedColumns[i]).userData;
				TemplatesViewerColumn column = _config.GetColumn(columnDataIndex);
				if (ascending) {
					orderedQuery = orderedQuery.ThenBy(i => column.SortingObject(i));
				} else {
					orderedQuery = orderedQuery.ThenByDescending(i => column.SortingObject(i));
				}
			}

			rootItem.children = orderedQuery.Cast<TreeViewItem>().ToList();
			BuildRows(Root);
		}

		IOrderedEnumerable<TemplatesViewerTreeItem> InitialOrder(IEnumerable<TemplatesViewerTreeItem> items, int[] sortedColumns)
		{
			bool ascending = multiColumnHeader.IsSortedAscending(sortedColumns[0]);
			int columnDataIndex = multiColumnHeader.GetColumn(sortedColumns[0]).userData;
			TemplatesViewerColumn column = _config.GetColumn(columnDataIndex);

			if (ascending) {
				return items.OrderBy(i => column.SortingObject(i));
			} else {
				return items.OrderByDescending(i => column.SortingObject(i));
			}
		}
    }
}