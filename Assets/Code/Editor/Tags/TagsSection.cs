using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Helpers.Tags;
using Awaken.TG.Main.UIToolkit;
using Unity.Properties;
using UnityEngine.UIElements;

namespace Awaken.TG.Editor.Tags {
    public class TagsSection {
        const string DarkerBackgroundStyle = "darker-bg";

        public Foldout Foldout { get; private set; }
        public MultiColumnTreeView Table { get; }
        public TagsCache TagsAsset { get; }

        List<TreeViewItemData<TagValueAccessor>> _rootItems;

        public TagsSection(MultiColumnTreeView tablePrototype, TagsCache tagsAsset) {
            Table = tablePrototype;
            TagsAsset = tagsAsset;

            SetupFoldout();
            SetupTable();
        }

        void SetupFoldout() {
            Foldout = new Foldout { value = false };
            Foldout.AddToClassList(DarkerBackgroundStyle);
        }

        void SetupTable() {
            _rootItems = SyncRootItems();
            Table.SetRootItems(_rootItems);

            TagsTableFactory.SetupColumn(this, TagsTableFactory.ColumnName, () => new Label());
            TagsTableFactory.SetupColumn(this, TagsTableFactory.ColumnContext, () => new TextField());
            TagsTableFactory.SetupColumn(this, TagsTableFactory.ColumnActions, () => new Button());

            Foldout.Add(Table);
            UpdateFoldoutInfo();
            UpdateFoldoutState();
        }

        List<TreeViewItemData<TagValueAccessor>> SyncRootItems() {
            int id = 0;

            return TagsAsset.entries.Where(tag => !string.IsNullOrWhiteSpace(tag.kind.value)).Select(
                entry => {
                    var children = entry.values
                        .Where(value => !string.IsNullOrWhiteSpace(value.value))
                        .Select(value => new TreeViewItemData<TagValueAccessor>(id++, new TagValueAccessor(TagsAsset, entry.kind.value, value.value)))
                        .OrderBy(value => value.data.Token)
                        .ToList();
                    return new TreeViewItemData<TagValueAccessor>(id++, new TagValueAccessor(TagsAsset, entry.kind.value, null), children);
                }).OrderBy(kind => kind.data.Token).ToList();
        }

        public void RemoveTag(int index) {
            int id = Table.GetIdForIndex(index);
            var tagAccessor = Table.GetItemDataForId<TagValueAccessor>(id);
            int parentId = Table.GetParentIdForIndex(index);

            Table.TryRemoveItem(id);
            
            if (tagAccessor.IsKind == false && Table.viewController.GetChildrenIds(parentId).Any() == false) { 
                Table.TryRemoveItem(parentId);
            }
            
            tagAccessor.Remove();
            
            UpdateFoldoutState();
            UpdateFoldoutInfo();
        }

        public void Filter(ChangeEvent<string> filter) {
            if (string.IsNullOrWhiteSpace(filter.newValue)) {
                ResetTable();
                return;
            }

            List<TreeViewItemData<TagValueAccessor>> newRoots = new();

            _rootItems.ForEach(root => {
                if (root.data.Token.Contains(filter.newValue, StringComparison.OrdinalIgnoreCase)) {
                    newRoots.Add(root);
                } else {
                    List<TreeViewItemData<TagValueAccessor>> newChildren = root.children
                        .Where(child => child.data.Token.Contains(filter.newValue, StringComparison.OrdinalIgnoreCase)).ToList();
                    if (newChildren.Count > 0) {
                        newRoots.Add(new TreeViewItemData<TagValueAccessor>(root.id, root.data, newChildren));
                    } 
                }
            });

            Table.SetRootItems(newRoots);
            UpdateFoldoutState(newRoots.Count != 0);
            Foldout.value = true;
            Table.ExpandAll();
        }
        
        void UpdateFoldoutInfo() {
            if (Table.itemsSource.Count <= 0) {
                Foldout.Add(new Label("No tags found"));
                Table.SetActiveOptimized(false);
            }
        }

        void UpdateFoldoutState(bool visible = true) {
            Foldout.text = $"{TagsAsset.category} <size=11px>{Table.GetTreeCount()} tags</size>";
            Foldout.SetActiveOptimized(visible);
        }

        void ResetTable() {
            _rootItems = SyncRootItems();
            Table.SetRootItems(_rootItems);
            UpdateFoldoutState();
            Table.CollapseAll();
            UpdateFoldoutInfo();
            Foldout.value = false;
        }
    }
    
    [Serializable]
    public class TagValueAccessor {
        readonly TagsCache _cache;
        readonly string _kind;
        readonly string _value;
        
        public bool IsKind => _value == null && _kind != null;

        [CreateProperty] public string Token {
            get => _value ?? _kind;
            set { }
        }

        [CreateProperty]
        public string Context {
            get {
                if (_cache.TryFindEntry(_kind, out var entry)) {
                    if (_value == null) {
                        return entry.kind.context;
                    }

                    if (entry.TryFindValue(_value, out var stringWithContext)) {
                        return stringWithContext.context;
                    }
                }

                return null;
            }
            set {
                if (_cache.TryFindEntryIndex(_kind, out var iEntry)) {
                    ref var entry = ref _cache.entries[iEntry];
                    if (this._value == null) {
                        entry.kind.context = value;
                        entry.dirty = true;
                        _cache.dirty = true;
                    } else if (entry.TryFindValueIndex(this._value, out var iValue)) {
                        entry.values[iValue].context = value;
                        entry.dirty = true;
                    }
                }
            }
        }

        public TagValueAccessor(TagsCache cache, string kind, string value) {
            _cache = cache;
            _kind = kind;
            _value = value;
        }
        
        public void Remove() {
            if (IsKind) {
                TagsCacheUtils.RemoveTagKind(_kind, _cache.category);
            } else {
                string tag = $"{_kind}:{_value}";
                TagsCacheUtils.RemoveTag(tag, _cache.category);
            }
        }
    }
}