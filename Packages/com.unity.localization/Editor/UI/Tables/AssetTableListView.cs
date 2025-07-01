using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace UnityEditor.Localization.UI
{
    class LocalizedAssetTableListView : GenericAssetTableListView<AssetTable, AssetTableTreeViewItem>
    {
        const float k_ThumbnailPreviewHeight = 80;

        protected override float MinRowHeight => 22;

        public LocalizedAssetTableListView(AssetTableCollection tableCollection) :
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

        protected override IReadOnlyCollection<AssetTable> GetCollectionTables() => (TableCollection as AssetTableCollection).AssetTables;

        protected override float GetCustomRowHeight(int row, TreeViewItem item)
        {
            var height = base.GetCustomRowHeight(row, item);
            if (item is AssetTableTreeViewItem tvi && tvi.AssetType != typeof(Object))
            {
                if (EditorGUIUtility.HasObjectThumbnail(tvi.AssetType))
                    return Mathf.Max(k_ThumbnailPreviewHeight, height) + k_RowFooterHeightWithPadding + k_RowVerticalPadding;
            }

            return height;
        }

        protected override void DrawItemField(Rect cellRect, int colIdx, TableColumn<AssetTable> col, AssetTableTreeViewItem item)
        {
            EditorGUI.BeginChangeCheck();
            var newAsset = EditorGUI.ObjectField(cellRect, item.GetTableAsset(colIdx), item.AssetType, false);
            if (EditorGUI.EndChangeCheck())
            {
                var oldType = item.AssetType;
                item.SetAsset(newAsset, colIdx);

                if (oldType != item.AssetType)
                    RefreshCustomRowHeights();
            }
        }
    }
}
