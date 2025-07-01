using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Main.Utility.Tags;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility.Collections;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Awaken.TG.Editor.Helpers.Tags {
    internal class TagsBrowser : EditorWindow, ISerializationCallbackReceiver {
        static bool IsOpen { get; set; }
        
        public void OnBeforeSerialize() {
            treeViewState = treeView.state;
        }
        public void OnAfterDeserialize() { }

        class TreeView : UnityEditor.IMGUI.Controls.TreeView {
            public TagsCache tagsCache;
            public TreeView(State state) : base(state.baseState) {
                noSearchExpandState = state.noSearchExpandState;
                enableItemHovering = true;
                SelectedObject = state.selectedObject;
                for (int i = 0; i < state.itemPaths.Count; ++i) {
                    itemIDs.Add(state.itemPaths[i], state.itemIDs[i]);
                }
            }

            public override void OnGUI(Rect rect) {
                base.OnGUI(rect);
                TryShowTooltip();
            }

            public void JumpToTag(string path) {
                JumpToItem(path);
            }

            bool TryShowTooltip() {
                if (hoveredItem != null) {
                    string context = hoveredItem switch {
                        LeafItem => (hoveredItem as LeafItem)?.Context,
                        FolderItem => (hoveredItem as FolderItem)?.Context,
                        _ => string.Empty
                    };

                    if (!string.IsNullOrWhiteSpace(context)) {
                        GUIStyle tooltipStyle = EditorStyles.label;
                        tooltipStyle.wordWrap = true;
                        EditorGUILayout.LabelField(context, tooltipStyle); 
                        return true;
                    }

                    return false;
                }
                
                return false;
            }
            
            void JumpToItem(string path) {
                nextFramedItemPath = path;
                Reload();

                if (itemIDs.TryGetValue(path, out int itemID)) {
                    SetSelection(new List<int> {itemID}, TreeViewSelectionOptions.RevealAndFrame | TreeViewSelectionOptions.FireSelectionChanged);
                } else {
                    SetSelection(new List<int>());
                }
            }

            static readonly Texture2D folderOpenIcon = EditorGUIUtility.Load("FMOD/FolderIconOpen.png") as Texture2D;
            static readonly Texture2D folderClosedIcon = EditorGUIUtility.Load("FMOD/FolderIconClosed.png") as Texture2D;
            static readonly Texture2D eventIcon = EditorGUIUtility.Load("FMOD/EventIcon.png") as Texture2D;

            class LeafItem : TreeViewItem {
                [field: SerializeField]
                public string Context { get; private set; }
                public string TagPath { get; }
                
                public LeafItem(int id, int depth, string tagPath, string context) : base(id, depth) {
                    TagPath = tagPath;
                    Context = context;
                }
            }

            class FolderItem : TreeViewItem {
                [field: SerializeField]
                public string Context { get; private set; }

                public FolderItem(int id, int depth, string displayName, string context) : base(id, depth, displayName) {
                    Context = context;
                }
            }

            FolderItem CreateFolderItem(string name, string path, bool hasChildren, bool forceExpanded, TreeViewItem parent) {
                FolderItem item = new (AffirmItemID("folder:" + path), 0, name, tagsCache.GetKindContext(name));
                bool expanded;

                if (!hasChildren) {
                    expanded = false;
                } else if (forceExpanded || expandNextFolderSet || (nextFramedItemPath != null && nextFramedItemPath.StartsWith(path))) {
                    SetExpanded(item.id, true);
                    expanded = true;
                } else {
                    expanded = IsExpanded(item.id);
                }

                if (expanded) {
                    item.icon = folderOpenIcon;
                } else {
                    item.icon = folderClosedIcon;

                    if (hasChildren) {
                        item.children = CreateChildListForCollapsedParent();
                    }
                }

                parent.AddChild(item);
                return item;
            }

            protected override TreeViewItem BuildRoot() {
                return new TreeViewItem(-1, -1);
            }

            Dictionary<string, int> itemIDs = new();

            int AffirmItemID(string path) {
                int id;

                if (!itemIDs.TryGetValue(path, out id)) {
                    id = itemIDs.Count;
                    itemIDs.Add(path, id);
                }

                return id;
            }

            bool expandNextFolderSet;
            string nextFramedItemPath;
            string[] searchStringSplit;

            protected override IList<TreeViewItem> BuildRows(TreeViewItem root) {
                if (hasSearch) {
                    searchStringSplit = searchString.Split(' ');
                }

                rootItem.children?.Clear();

                foreach (var entry in tagsCache.entries) {
                    CreateSubTree(entry.kind.value, entry.values, value => $"{entry.kind.value}{TagsEditing.KindValueSeparator}{value}");
                }

                List<TreeViewItem> rows = new();

                AddChildrenInOrder(rows, rootItem);

                SetupDepthsFromParentsAndChildren(rootItem);

                expandNextFolderSet = false;
                nextFramedItemPath = null;

                return rows;
            }

            void CreateSubTree(string kind, TagsCache.StringWithContext[] values, Func<string, string> getPath) {
                var records = ArrayUtils.Select(values, value => (source: value.value, path: getPath(value.value), show: true));
                Array.Sort(records, (a, b) => string.Compare(a.path, b.path, StringComparison.OrdinalIgnoreCase));
                
                if (hasSearch) {
                    bool none = true;
                    for (int i = 0; i < records.Length; i++) {
                        var show = searchStringSplit.All(word => word.Length <= 0 || records[i].path.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0);
                        records[i].show = show;
                        if (show) {
                            none = false;
                        }
                    }

                    // remove filtered folders
                    if (none) {
                        return;
                    }
                }
                
                TreeViewItem root = CreateFolderItem(kind, kind, records.Length > 0, false, rootItem);
                List<TreeViewItem> currentFolderItems = new();

                foreach (var record in records) {
                    if (!record.show) {
                        continue;
                    }
                    TreeViewItem parent = CreateFolderItems(record.path, currentFolderItems, root, out string leafName);
                    if (parent != null) {
                        TreeViewItem leafItem = new LeafItem(AffirmItemID(record.path), 0, record.path, tagsCache.GetValueContext(TagUtils.TagKind(record.path), record.source));
                        leafItem.displayName = leafName;
                        leafItem.icon = eventIcon;

                        parent.AddChild(leafItem);
                    }
                }
            }

            TreeViewItem CreateFolderItems(string path, List<TreeViewItem> currentFolderItems, TreeViewItem root, out string leafName) {
                TreeViewItem parent = root;

                // Skip the type prefix at the start of the path
                int elementStart = path.IndexOf(TagsEditing.KindValueSeparator) + 1;

                for (int i = 0;; ++i) {
                    if (!IsExpanded(parent.id)) {
                        leafName = null;
                        return null;
                    }

                    int elementEnd = path.IndexOf(TagsEditing.KindValueSeparator, elementStart);

                    if (elementEnd < 0) {
                        // No more folders; elementStart points to the event name
                        break;
                    }

                    string folderName = path.Substring(elementStart, elementEnd - elementStart);

                    if (i < currentFolderItems.Count && folderName != currentFolderItems[i].displayName) {
                        currentFolderItems.RemoveRange(i, currentFolderItems.Count - i);
                    }

                    if (i == currentFolderItems.Count) {
                        FolderItem folderItem = CreateFolderItem(folderName, path.Substring(0, elementEnd), true, false, parent);
                        currentFolderItems.Add(folderItem);
                    }

                    elementStart = elementEnd + 1;
                    parent = currentFolderItems[i];
                }

                leafName = path.Substring(elementStart);
                return parent;
            }

            static void AddChildrenInOrder(List<TreeViewItem> list, TreeViewItem item) {
                if (item.children != null) {
                    foreach (TreeViewItem child in item.children.Where(child => child is FolderItem)) {
                        list.Add(child);

                        AddChildrenInOrder(list, child);
                    }

                    foreach (TreeViewItem child in
                        item.children.Where(child => !(child == null || child is FolderItem))) {
                        list.Add(child);
                    }
                }
            }

            protected override bool CanMultiSelect(TreeViewItem item) => false;
            protected override bool CanChangeExpandedState(TreeViewItem item) => item.hasChildren;

            IList<int> noSearchExpandState;
            protected override void SearchChanged(string newSearch) {
                if (!string.IsNullOrEmpty(newSearch.Trim())) {
                    expandNextFolderSet = true;

                    if (noSearchExpandState == null) {
                        // A new search is beginning
                        noSearchExpandState = GetExpanded();
                        SetExpanded(new List<int>());
                    }
                } else {
                    if (noSearchExpandState != null) {
                        // A search is ending
                        SetExpanded(noSearchExpandState);
                        noSearchExpandState = null;
                    }
                }
            }

            public string SelectedObject { get; private set; }
            public string DoubleClickedObject { get; private set; }

            protected override void SelectionChanged(IList<int> selectedIDs) {
                SelectedObject = null;

                if (selectedIDs.Count > 0) {
                    TreeViewItem item = FindItem(selectedIDs[0], rootItem);

                    if (item is LeafItem) {
                        SelectedObject = (item as LeafItem).TagPath;
                    }
                }
            }

            protected override void DoubleClickedItem(int id) {
                TreeViewItem item = FindItem(id, rootItem);

                if (item is LeafItem leafItem) {
                    DoubleClickedObject = leafItem.TagPath;
                }
            }

            float oldBaseIndent;

            protected override void BeforeRowsGUI() {
                oldBaseIndent = baseIndent;
                DoubleClickedObject = null;
            }

            protected override void RowGUI(RowGUIArgs args) {
                if (hasSearch) {
                    // Hack to undo TreeView flattening the hierarchy when searching
                    baseIndent = oldBaseIndent + args.item.depth * depthIndentWidth;
                }

                base.RowGUI(args);

                TreeViewItem item = args.item;

                if (Event.current.type == EventType.MouseUp && item is FolderItem && item.hasChildren) {
                    Rect rect = args.rowRect;
                    rect.xMin = GetContentIndent(item);

                    if (rect.Contains(Event.current.mousePosition)) {
                        SetExpanded(item.id, !IsExpanded(item.id));
                        Event.current.Use();
                    }
                }
            }

            protected override void AfterRowsGUI() {
                baseIndent = oldBaseIndent;
            }

            [Serializable]
            public class State {
                public State() : this(new TreeViewState()) { }

                public State(TreeViewState baseState) {
                    this.baseState = baseState;
                }

                public TreeViewState baseState;
                public List<int> noSearchExpandState;
                public string selectedObject;
                public List<string> itemPaths = new();
                public List<int> itemIDs = new();
                public TagsCategory typeFilter = TagsCategory.Flag;
            }

            public new State state {
                get {
                    State result = new(base.state);

                    if (noSearchExpandState != null) {
                        result.noSearchExpandState = new List<int>(noSearchExpandState);
                    }

                    result.selectedObject = SelectedObject;

                    foreach ((string path, int id) in itemIDs) {
                        result.itemPaths.Add(path);
                        result.itemIDs.Add(id);
                    }

                    result.typeFilter = TagsCategory.Flag;

                    return result;
                }
            }
        }

        Texture2D borderIcon;
        GUIStyle borderStyle;
        
        void AffirmResources() {
            if (borderIcon == null) {
                borderIcon = EditorGUIUtility.Load("FMOD/Border.png") as Texture2D;

                borderStyle = new GUIStyle(GUI.skin.box);
                borderStyle.normal.background = borderIcon;
                borderStyle.margin = new RectOffset();
            }
        }

        [SerializeField] TreeView.State treeViewState;

        [NonSerialized] TreeView treeView;
        [NonSerialized] SearchField searchField;
        [NonSerialized] DateTime LastKnownCacheTime;

        SerializedProperty outputProperty;
        List<string> currentTags;

        bool InChooserMode => outputProperty != null;

        void OnGUI() {
            if (!IsOpen) {
                return;
            }
            
            AffirmResources();
                           
            if (InChooserMode) {
                GUILayout.BeginVertical(borderStyle, GUILayout.ExpandWidth(true));
            }

            treeView.searchString = searchField.OnGUI(treeView.searchString);

            Rect treeRect = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            treeRect.y += 2;
            treeRect.height -= 2;

            treeView.OnGUI(treeRect);

            if (InChooserMode) {
                GUILayout.EndVertical();
                HandleChooserModeEvents();
            }
        }

        void HandleChooserModeEvents() {
            if (Event.current.isKey) {
                KeyCode keyCode = Event.current.keyCode;

                if ((keyCode == KeyCode.Return || keyCode == KeyCode.KeypadEnter) && treeView.SelectedObject != null) {
                    SetOutputProperty(treeView.SelectedObject);
                    Event.current.Use();
                    Close();
                } else if (keyCode == KeyCode.Escape) {
                    Event.current.Use();
                    Close();
                }
            } else if (treeView.DoubleClickedObject != null) {
                SetOutputProperty(treeView.DoubleClickedObject);
                Close();
            }
        }

        void SetOutputProperty(string tag) {
            currentTags.Add(tag);
            TagsEditing.SetTagProperty(outputProperty, currentTags);
            outputProperty.serializedObject.ApplyModifiedProperties();
        }

        public void Show(SerializedProperty property, TagsCache tagsDefinition, List<string> tags) {
            BeginInspectorPopup(property, tagsDefinition, tags);
            treeView.Reload();
        }

        void BeginInspectorPopup(SerializedProperty property, TagsCache tagsDefinition, List<string> tags) {
            treeView.tagsCache = tagsDefinition;
            outputProperty = property;
            currentTags = tags;
            searchField.SetFocus();
        }
        
        public void OnEnable() {
            treeViewState ??= new TreeView.State();
            searchField = new SearchField();
            treeView = new TreeView(treeViewState);
            searchField.downOrUpArrowKeyPressed += treeView.SetFocus;
            IsOpen = true;
        }

        void OnDestroy() {
            IsOpen = false;
        }
    }
}