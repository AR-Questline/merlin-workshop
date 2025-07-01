using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine.Localization.Tables;
using UnityEngine.UIElements;

namespace UnityEditor.Localization.UI
{
    #if UNITY_2023_3_OR_NEWER
    [UxmlElement]
    #endif
    partial class ProjectCollectionsTableSelector : VisualElement
    {
        #if !UNITY_2023_3_OR_NEWER
        internal new class UxmlFactory : UxmlFactory<ProjectCollectionsTableSelector> {}
        #endif

        [Flags]
        public enum CollectionType
        {
            String = 1,
            Asset = 2,
            Both = String | Asset
        }

        public CollectionType VisibleType { get; set; } = CollectionType.String;

        public string SearchString { get; set; }

        public Dictionary<LocalizationTableCollection, HashSet<int>> SelectedTableIndexes { get; } = new Dictionary<LocalizationTableCollection, HashSet<int>>();

        VisualElement m_ContentContainer;

        public ProjectCollectionsTableSelector()
        {
            var asset = Resources.GetTemplateAsset(nameof(ProjectCollectionsTableSelector));
            asset.CloneTree(this);
            m_ContentContainer = this.Q("select-list");

            var searchField = this.Q<ToolbarSearchField>("search-field");
            searchField.RegisterValueChangedCallback(SearchChanged);

            var selectAllButton = this.Q<Button>("select-all-button");
            selectAllButton.clicked += () => SelectVisible(true);
            var selectNoneButton = this.Q<Button>("select-none-button");
            selectNoneButton.clicked += () => SelectVisible(false);

            Initialize();
        }

        void SearchChanged(ChangeEvent<string> evt)
        {
            bool showAll = string.IsNullOrEmpty(evt.newValue);
            this.Query<Foldout>().ForEach(f =>
            {
                bool showFoldout = false;
                foreach (var child in f.Children())
                {
                    if (showAll || child.name.Contains(evt.newValue))
                    {
                        showFoldout = true;
                        child.style.display = DisplayStyle.Flex;
                    }
                    else
                    {
                        child.style.display = DisplayStyle.None;
                    }
                }

                f.style.display = showFoldout ? DisplayStyle.Flex : DisplayStyle.None;
            });
        }

        void SelectVisible(bool selected)
        {
            this.Query<Foldout>().ForEach(f =>
            {
                if (f.style.display != DisplayStyle.None)
                {
                    foreach (var child in f.Children())
                    {
                        if (child.style.display != DisplayStyle.None)
                        {
                            var toggle = child.Q<Toggle>();
                            toggle.value = selected;
                        }
                    }
                }
            });
        }

        public void SetSelection(LocalizationTableCollection collection)
        {
            SelectedTableIndexes.Clear();
            SelectedTableIndexes[collection] = new HashSet<int>(Enumerable.Range(0, collection.Tables.Count));
            Initialize(false);
        }

        public void SetSelection(LocalizationTable table)
        {
            SelectedTableIndexes.Clear();
            var collection = LocalizationEditorSettings.GetCollectionFromTable(table);

            var hashSet = new HashSet<int>();
            for (int i = 0; i < collection.Tables.Count; ++i)
            {
                if (collection.Tables[i].asset == table)
                {
                    hashSet.Add(i);
                    break;
                }
            }

            SelectedTableIndexes[collection] = hashSet;
            Initialize(false);
        }

        public void Initialize(bool defaultSelectState = true)
        {
            m_ContentContainer.Clear();

            if (VisibleType.HasFlag(CollectionType.String))
            {
                var stringTableCollections = LocalizationEditorSettings.GetStringTableCollections();
                foreach (var collection in stringTableCollections)
                {
                    AddCollection(collection, defaultSelectState);
                }
            }

            if (VisibleType.HasFlag(CollectionType.Asset))
            {
                var assetTableCollections = LocalizationEditorSettings.GetAssetTableCollections();
                foreach (var collection in assetTableCollections)
                {
                    AddCollection(collection, defaultSelectState);
                }
            }
        }

        void AddCollection(LocalizationTableCollection collection, bool defaultSelectState)
        {
            if (!SelectedTableIndexes.TryGetValue(collection, out var selectedTables))
            {
                selectedTables = defaultSelectState ? new HashSet<int>(Enumerable.Range(0, collection.Tables.Count)) : new HashSet<int>();
                SelectedTableIndexes[collection] = selectedTables;
            }

            var collectionElement = new Foldout { text = collection.TableCollectionName, name = collection.TableCollectionName, value = selectedTables.Count > 0 };
            m_ContentContainer.Add(collectionElement);
            for (int i = 0; i < collection.Tables.Count; ++i)
            {
                // TODO: We could get the table name without loading the actual asset by using the instance Id and getting the file name.
                var tableName = collection.Tables[i].asset.name;

                var toggleRow = new VisualElement { name = tableName, style = { flexDirection = FlexDirection.Row }};
                collectionElement.Add(toggleRow);
                var toggle = new Toggle { value = selectedTables.Contains(i) };
                toggleRow.Add(toggle);
                toggleRow.Add(new Label(tableName));

                int index = i;
                toggle.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue)
                        selectedTables.Add(index);
                    else
                        selectedTables.Remove(index);
                });
            }
        }
    }
}
