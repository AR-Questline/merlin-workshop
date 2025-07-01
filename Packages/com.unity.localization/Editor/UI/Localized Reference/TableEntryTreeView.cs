using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Tables;

namespace UnityEditor.Localization.UI
{
    /// <summary>
    /// Tree view item to represent an entry from a table.
    /// </summary>
    internal class TableEntryTreeViewItem : TreeViewItem
    {
        public LocalizationTableCollection TableCollection { get; set; }
        public SharedTableData.SharedTableEntry SharedEntry { get; set; }

        public TableEntryTreeViewItem(LocalizationTableCollection collection, SharedTableData.SharedTableEntry sharedEntry, int id, int depth) :
            base(id, depth)
        {
            TableCollection = collection;
            if (sharedEntry != null)
            {
                SharedEntry = sharedEntry;
                displayName = SharedEntry.Key;
            }
        }
    }


    internal class EntryTreeView : TableEntryTreeView
    {
        LocalizationTableCollection m_Selected;

        public EntryTreeView(Type assetType, LocalizationTableCollection selected, Action<LocalizationTableCollection, SharedTableData.SharedTableEntry> selectionHandler)
        {
            m_Selected = selected;
            m_AssetType = assetType;
            m_SelectionHandler = selectionHandler;
            Reload();
        }

        protected internal override ReadOnlyCollection<AssetTableCollection> GetAssetTableCollections()
        {
            return new ReadOnlyCollection<AssetTableCollection>(new[] { m_Selected as AssetTableCollection });
        }

        protected internal override ReadOnlyCollection<StringTableCollection> GetStringTableCollections()
        {
            return new ReadOnlyCollection<StringTableCollection>(new[] { m_Selected as StringTableCollection });
        }

        protected override TreeViewItem FindOrCreateGroup(TreeViewItem root, LocalizationTableCollection collection, ref int nodeId)
        {
            return root;
        }
    }

    /// <summary>
    /// Allows for selecting a table entry from the projects tables.
    /// </summary>
    internal class TableEntryTreeView : TreeView
    {
        protected Type m_AssetType;
        protected Action<LocalizationTableCollection, SharedTableData.SharedTableEntry> m_SelectionHandler;

        public TreeViewItem Root { get; private set; }

        public TableEntryTreeView()
            : base(new TreeViewState())
        {
            showAlternatingRowBackgrounds = true;
            showBorder = true;
        }

        public TableEntryTreeView(Type assetType, Action<LocalizationTableCollection, SharedTableData.SharedTableEntry> selectionHandler)
            : this()
        {
            m_AssetType = assetType;
            m_SelectionHandler = selectionHandler;

            showAlternatingRowBackgrounds = true;
            showBorder = true;
            Reload();
        }

        protected override bool CanMultiSelect(TreeViewItem item) => false;

        internal protected virtual ReadOnlyCollection<StringTableCollection> GetStringTableCollections() => LocalizationEditorSettings.GetStringTableCollections();
        internal protected virtual ReadOnlyCollection<AssetTableCollection> GetAssetTableCollections() => LocalizationEditorSettings.GetAssetTableCollections();

        protected override TreeViewItem BuildRoot()
        {
            Root = new TreeViewItem(-1, -1);
            var id = 1;

            Root.AddChild(new TableEntryTreeViewItem(null, null, id++, 0) { displayName = $"None ({m_AssetType.Name})" });

            if (m_AssetType == typeof(string))
            {
                var stringTableCollections = GetStringTableCollections();
                foreach (var collection in stringTableCollections)
                {
                    var group = FindOrCreateGroup(Root, collection, ref id);

                    var tableNode = new TreeViewItem(id++, 0, collection.TableCollectionName)
                    {
                        icon = AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(collection)) as Texture2D
                    };
                    group.AddChild(tableNode);

                    var sharedData = collection.SharedData;
                    foreach (var entry in sharedData.Entries)
                    {
                        tableNode.AddChild(new TableEntryTreeViewItem(collection, entry, id++, 1));
                    }
                }
            }
            else
            {
                var assetTableCollections = GetAssetTableCollections();
                foreach (var collection in assetTableCollections)
                {
                    var group = FindOrCreateGroup(Root, collection, ref id);

                    var tableNode = new TreeViewItem(id++, 0, collection.TableCollectionName)
                    {
                        icon = AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(collection)) as Texture2D
                    };
                    group.AddChild(tableNode);

                    var sharedData = collection.SharedData;

                    // Record all the types that are not compatible with this one, then we can later discard the incompatible entries.
                    var incompatibleTypes = new HashSet<long>();
                    foreach (var md in sharedData.Metadata.MetadataEntries)
                    {
                        if (md is AssetTypeMetadata assetTypeMd && !m_AssetType.IsAssignableFrom(assetTypeMd.Type))
                        {
                            incompatibleTypes.UnionWith(assetTypeMd.EntriesLookup.Keys);
                        }
                    }

                    foreach (var entry in sharedData.Entries)
                    {
                        if (!incompatibleTypes.Contains(entry.Id))
                        {
                            tableNode.AddChild(new TableEntryTreeViewItem(collection, entry, id++, 1));
                        }
                    }
                }
            }

            if (!Root.hasChildren)
            {
                Root.AddChild(new TreeViewItem(1, 0, "No Tables Found."));
            }

            SetupDepthsFromParentsAndChildren(Root);

            return Root;
        }

        protected virtual TreeViewItem FindOrCreateGroup(TreeViewItem root, LocalizationTableCollection collection, ref int nodeId)
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
            if (selectedIds.Count == 0)
                return;

            var selected = FindItem(selectedIds[0], rootItem);
            if (selected is TableEntryTreeViewItem keyNode)
            {
                m_SelectionHandler(keyNode.TableCollection, keyNode.SharedEntry);
                return;
            }

            // Toggle the foldout
            if (selected.hasChildren)
            {
                SetExpanded(selected.id, !IsExpanded(selected.id));
            }

            // Ignore Table selections. We just care about table entries.
            SetSelection(new int[] {});
        }
    }
}
