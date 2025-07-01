using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace UnityEditor.Localization.UI
{
    /// <summary>
    /// Tree view item to represent an entry from a table.
    /// </summary>
    internal class TableTreeViewItem : TreeViewItem
    {
        public LocalizationTableCollection TableCollection { get; set; }

        public TableTreeViewItem(LocalizationTableCollection collection, int id) :
            base(id, 0)
        {
            TableCollection = collection;

            if (collection != null)
                displayName = collection.TableCollectionName;
        }
    }

    /// <summary>
    /// Allows for selecting a table entry from the projects tables.
    /// </summary>
    internal class TableTreeView : TreeView
    {
        readonly Action<LocalizationTableCollection> m_SelectionHandler;
        Type m_TableType;

        public TreeViewItem Root { get; private set; }

        public TableTreeView(Type tableType, Action<LocalizationTableCollection> selectionHandler)
            : base(new TreeViewState())
        {
            m_TableType = tableType;
            m_SelectionHandler = selectionHandler;

            showAlternatingRowBackgrounds = true;
            showBorder = true;
            Reload();
        }

        protected override bool CanMultiSelect(TreeViewItem item) => false;

        internal protected virtual List<LocalizationTableCollection> GetCollections()
        {
            var tableCollections = new List<LocalizationTableCollection>();
            if (m_TableType == typeof(StringTable))
                tableCollections.AddRange(LocalizationEditorSettings.GetStringTableCollections());
            else
                tableCollections.AddRange(LocalizationEditorSettings.GetAssetTableCollections());
            return tableCollections;
        }

        protected override TreeViewItem BuildRoot()
        {
            Root = new TreeViewItem(-1, -1);
            var id = 1;

            Root.AddChild(new TableTreeViewItem(null, id++) { displayName = $"None ({m_TableType.Name})" });

            var tableCollections = GetCollections();

            foreach (var collection in tableCollections)
            {
                var group = FindOrCreateGroup(Root, collection, ref id);

                var tableNode = new TableTreeViewItem(collection, id++)
                {
                    icon = AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(collection)) as Texture2D
                };
                group.AddChild(tableNode);
            }

            if (!Root.hasChildren)
            {
                Root.AddChild(new TreeViewItem(1, 0, "No Tables Found."));
            }

            SetupDepthsFromParentsAndChildren(Root);

            return Root;
        }

        TreeViewItem FindOrCreateGroup(TreeViewItem root, LocalizationTableCollection collection, ref int nodeId)
        {
            var currentRoot = root;
            if (collection.Group == collection.DefaultGroupName)
            {
                return currentRoot;
            }

            var subGroups = collection.Group.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var g in subGroups)
            {
                var foundGroup = currentRoot.children?.FirstOrDefault(c => c.displayName == g);
                if (foundGroup == null)
                {
                    var node = new TreeViewItem(nodeId++, 0, g);
                    currentRoot.AddChild(node);
                    foundGroup = node;
                }
                currentRoot = foundGroup;
            }
            return currentRoot;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (FindItem(selectedIds[0], rootItem) is TableTreeViewItem keyNode)
            {
                m_SelectionHandler(keyNode.TableCollection);
            }
            else
            {
                // Ignore Group selections. We just care about tables.
                SetSelection(new int[] {});
            }
        }
    }
}
