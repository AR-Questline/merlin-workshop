using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEditor.Localization.Bridge;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.Pool;
using Object = UnityEngine.Object;
using TreeView = UnityEditor.IMGUI.Controls.TreeView;

namespace UnityEditor.Localization.UI
{
    abstract class GenericAssetTableListView<T1, T2> : TreeView, IDisposable
        where T1 : LocalizationTable
        where T2 : GenericAssetTableTreeViewItem<T1>, new()
    {
        const string k_DragId = "GenericAssetTableListViewDragging";
        const string k_CurrentPagePref = "Localization-CurrentPage";
        const string k_PageSizePref = "Localization-PageSize";
        const int k_DefaultPageSize = 50;
        const int k_TableStartIndex = 2; // Key, Key Id and then tables
        const int k_PageControlsHeight = 20;
        const float k_RowFooterHeight = 22;
        internal static readonly float k_RowVerticalPadding = EditorGUIUtility.standardVerticalSpacing * 2;
        internal static readonly float k_RowFooterHeightWithPadding = k_RowFooterHeight + (k_RowVerticalPadding * 2);
        const float k_RemoveButtonWidth = 27;
        protected const float k_EntryMenuButtonWidth = 20;

        static readonly GUIContent k_NewEntry = EditorGUIUtility.TrTextContent("New Entry");
        static readonly GUIContent k_RemoveEntry = EditorGUIUtility.TrIconContent("Toolbar Minus");
        static readonly GUIContent k_PrevPage = new GUIContent(EditorGUIUtility.IconContent("Animation.PrevKey").image as Texture2D);
        static readonly GUIContent k_NextPage = new GUIContent(EditorGUIUtility.IconContent("Animation.NextKey").image as Texture2D);
        static readonly GUIContent k_FirstPage = new GUIContent(EditorGUIUtility.IconContent("Animation.FirstKey").image as Texture2D);
        static readonly GUIContent k_LastPage = new GUIContent(EditorGUIUtility.IconContent("Animation.LastKey").image as Texture2D);
        static readonly GUIContent k_PageSize = new GUIContent(EditorGUIUtility.TrTextContent("Page Size"));
        static readonly GUIStyle k_SelectedStyle = "TV Selection";

        protected string TableCollectionName => TableCollection.TableCollectionName;

        public LocalizationTableCollection TableCollection { get; private set; }

        protected virtual float MinRowHeight => 60;

        int PageSize
        {
            get => m_PageSize;
            set
            {
                var clampedValue = Mathf.Clamp(value, 1, 1000);
                if (clampedValue != m_PageSize)
                {
                    m_PageSize = clampedValue;
                    TotalPages = Mathf.CeilToInt(TableCollection.SharedData.Entries.Count / (float)m_PageSize);
                    m_CurrentPage = Mathf.Clamp(value, 1, TotalPages);
                    EditorPrefs.SetInt(k_PageSizePref, m_PageSize);
                    ReloadPages();
                }
            }
        }

        int CurrentPage
        {
            get => m_CurrentPage;
            set
            {
                var clampedValue = Mathf.Clamp(value, 1, TotalPages);
                if (clampedValue != m_CurrentPage)
                {
                    m_CurrentPage = clampedValue;
                    EditorPrefs.SetInt(k_CurrentPagePref, m_CurrentPage);
                    ReloadPages();
                }
            }
        }

        int TotalPages
        {
            get => m_TotalPages;
            set
            {
                m_TotalPages = value;
                m_TotalPagesLabel = new GUIContent($"/{value}");
            }
        }

        const int k_AddItemId = int.MaxValue;

        TreeViewItem m_AddKeyItem;
        SearchField m_SearchField;

        int m_CurrentPage = -1;
        int m_PageSize = -1;
        int m_TotalPages = -1;
        GUIContent m_TotalPagesLabel;

        List<TreeViewItem> m_AllItems = new List<TreeViewItem>();
        List<TreeViewItem> m_VisibleRows = new List<TreeViewItem>();
        TreeViewItem m_Root;

        List<LocalizationTable> m_SortedTables = new List<LocalizationTable>();

        protected GenericAssetTableListView(LocalizationTableCollection tableCollection) :
            base(new TreeViewState())
        {
            TableCollection = tableCollection;
            m_SearchField = new SearchField();
            m_SearchField.downOrUpArrowKeyPressed += SetFocusAndEnsureSelectedItem;
            Undo.undoRedoPerformed += UndoRedoPerformed;
            LocalizationEditorSettings.EditorEvents.TableEntryAdded += EditorEvents_TableEntryModified;
            LocalizationEditorSettings.EditorEvents.TableEntryRemoved += EditorEvents_TableEntryModified;
            LocalizationEditorSettings.EditorEvents.AssetTableEntryAdded += EditorEvents_AssetTableEntryAdded;
            LocalizationEditorSettings.EditorEvents.AssetTableEntryRemoved += EditorEvents_AssetTableEntryRemoved;
            LocalizationEditorSettings.EditorEvents.TableAddedToCollection += EditorEvents_TableAddedOrRemoveFromCollection;
            LocalizationEditorSettings.EditorEvents.TableRemovedFromCollection += EditorEvents_TableAddedOrRemoveFromCollection;
            LocalizationEditorSettings.EditorEvents.CollectionModified += EditorEvents_CollectionModified;
            LocalizationEditorSettings.EditorEvents.LocaleSortOrderChanged += EditorEvents_LocaleSortOrderChanged;

            m_PageSize = EditorPrefs.GetInt(k_PageSizePref, k_DefaultPageSize);
            m_CurrentPage = EditorPrefs.GetInt(k_CurrentPagePref, 1);
            cellMargin = 5;
        }

        ~GenericAssetTableListView()
        {
            Dispose();
        }

        public void Dispose()
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            LocalizationEditorSettings.EditorEvents.TableEntryAdded -= EditorEvents_TableEntryModified;
            LocalizationEditorSettings.EditorEvents.TableEntryRemoved -= EditorEvents_TableEntryModified;
            LocalizationEditorSettings.EditorEvents.AssetTableEntryAdded -= EditorEvents_AssetTableEntryAdded;
            LocalizationEditorSettings.EditorEvents.AssetTableEntryRemoved -= EditorEvents_AssetTableEntryRemoved;
            LocalizationEditorSettings.EditorEvents.TableAddedToCollection -= EditorEvents_TableAddedOrRemoveFromCollection;
            LocalizationEditorSettings.EditorEvents.TableRemovedFromCollection -= EditorEvents_TableAddedOrRemoveFromCollection;
            LocalizationEditorSettings.EditorEvents.CollectionModified -= EditorEvents_CollectionModified;
            LocalizationEditorSettings.EditorEvents.LocaleSortOrderChanged -= EditorEvents_LocaleSortOrderChanged;
        }

        void EditorEvents_CollectionModified(object sender, LocalizationTableCollection collection)
        {
            if (sender != this && collection == TableCollection)
            {
                // Tables may have been modified.
                InitializeColumns();
                Reload();
            }
        }

        void EditorEvents_TableEntryModified(LocalizationTableCollection collection, SharedTableData.SharedTableEntry entry)
        {
            if (collection == TableCollection)
                Reload();
        }

        void EditorEvents_AssetTableEntryAdded(AssetTableCollection collection, AssetTable table, AssetTableEntry entry)
        {
            if (collection == TableCollection)
            {
                // If the changed item is being displayed then force a refresh.
                var item = rootItem.children.FirstOrDefault(tbl => tbl is AssetTableTreeViewItem at && at.KeyId == entry.KeyId) as AssetTableTreeViewItem;
                item?.RefreshFields();
            }
        }

        void EditorEvents_AssetTableEntryRemoved(AssetTableCollection collection, AssetTable table, AssetTableEntry entry, string removedAssetGuid)
        {
            if (collection == TableCollection)
            {
                // If the changed item is being displayed then force a refresh.
                var item = rootItem.children.FirstOrDefault(tbl => tbl is AssetTableTreeViewItem at && at.KeyId == entry.KeyId) as AssetTableTreeViewItem;
                item?.RefreshFields();
            }
        }

        void EditorEvents_TableAddedOrRemoveFromCollection(LocalizationTableCollection collection, LocalizationTable table)
        {
            if (collection == TableCollection)
            {
                Initialize();
                Reload();
            }
        }

        void EditorEvents_LocaleSortOrderChanged(object sender, Locale locale)
        {
            InitializeColumns();
            Reload();
        }

        protected override float GetCustomRowHeight(int row, TreeViewItem item)
        {
            float height = k_RowFooterHeightWithPadding + k_RowVerticalPadding; // Plus top padding

            if (item is T2 i)
            {
                // Height of key field
                height += Mathf.Max(EditorStyles.textArea.CalcSize(new GUIContent(i.Key)).y, MinRowHeight);
            }
            else
            {
                height += base.GetCustomRowHeight(row, item);
            }

            return height;
        }

        protected virtual void UndoRedoPerformed()
        {
            RefreshCustomRowHeights();
        }

        public virtual void Initialize()
        {
            InitializeColumns();
            Reload();
            multiColumnHeader.sortingChanged += mch => Reload();
        }

        protected override bool CanMultiSelect(TreeViewItem item) => false; // Disable multi select

        protected abstract IReadOnlyCollection<T1> GetCollectionTables();

        protected virtual void InitializeColumns()
        {
            showBorder = true;
            var keys = TableCollection.SharedData;

            var columns = new List<MultiColumnHeaderState.Column>
            {
                new KeyColumn(keys),
                new KeyIdColumn(keys)
            };

            var localesWithNoMatchingTable = ListPool<Locale>.Get();
            var projectTables = ListPool<T1>.Get();
            projectTables.AddRange(GetCollectionTables());
            m_SortedTables.Clear();

            // Add the tables in the order of Locales so Locale sorting order is respected.
            foreach (var locale in LocalizationEditorSettings.GetLocales())
            {
                var matchingTableIdx = projectTables.FindIndex(tbl => tbl?.LocaleIdentifier.Code == locale.Identifier.Code);
                if (matchingTableIdx != -1)
                {
                    var table = projectTables[matchingTableIdx];
                    m_SortedTables.Add(table);
                    projectTables.RemoveAt(matchingTableIdx);
                    columns.Add(new TableColumn<T1>(TableCollection, table, locale) {  headerTextAlignment = TextAlignment.Left });
                }
                else
                {
                    localesWithNoMatchingTable.Add(locale);
                }
            }

            // We need to add the remaining tables that do not have any Locales.
            for (int i = 0; i < projectTables.Count; ++i)
                m_SortedTables.Add(projectTables[i]);

            // Tables with missing Locales
            projectTables.ForEach(tbl => columns.Add(new TableColumn<T1>(TableCollection, tbl, null)));

            // Locales with no matching tables
            localesWithNoMatchingTable.ForEach(l => columns.Add(new MissingTableColumn(l)));

            var multiColState = new MultiColumnHeaderState(columns.ToArray());
            multiColumnHeader = new GenericAssetTableListViewMultiColumnHeader<T1, T2>(multiColState, this, TableCollection);
            multiColumnHeader.visibleColumnsChanged += (header) => RefreshCustomRowHeights();
            multiColumnHeader.ResizeToFit();
            ListPool<Locale>.Release(localesWithNoMatchingTable);
            ListPool<T1>.Release(projectTables);
        }

        internal protected virtual T2 CreateTreeViewItem(int index, SharedTableData.SharedTableEntry entry)
        {
            var item = new T2() { id = index, SharedEntry = entry };
            item.Initialize(TableCollection, k_TableStartIndex, m_SortedTables);
            return item;
        }

        void ReloadPages()
        {
            m_Root = rootItem;
            Reload();
            m_Root = null;
        }

        protected override TreeViewItem BuildRoot()
        {
            if (m_Root != null)
                return m_Root;

            var root = new TreeViewItem(-1, -1, "root");
            m_AllItems.Clear();

            if (TableCollection.SharedData == null)
            {
                Debug.LogError($"No {nameof(SharedTableData)} assigned to Table: " + TableCollectionName);
                SetupParentsAndChildrenFromDepths(root, m_AllItems);
                return root;
            }

            var sharedEntries = TableCollection.SharedData.Entries;

            // Apply Sorting?
            if (multiColumnHeader.sortedColumnIndex >= 0)
            {
                var ascend = multiColumnHeader.IsSortedAscending(multiColumnHeader.sortedColumnIndex);
                if (multiColumnHeader.sortedColumnIndex == 0)
                    sharedEntries.Sort((a, b) => ascend ? string.Compare(b.Key, a.Key) : string.Compare(a.Key, b.Key));
                else if (multiColumnHeader.sortedColumnIndex == 1)
                    sharedEntries.Sort((a, b) => ascend ? b.Id.CompareTo(a.Id) : a.Id.CompareTo(b.Id));
            }

            for (int i = 0; i < sharedEntries.Count; ++i)
            {
                var tvi = CreateTreeViewItem(i, sharedEntries[i]);
                m_AllItems.Add(tvi);
            }

            // At the end we add an extra node which will be used to add new keys.
            m_AddKeyItem = new GenericAssetTableTreeViewItem<T1>() { id = k_AddItemId, displayName = "Add Key" };
            m_AllItems.Add(m_AddKeyItem);
            SetupParentsAndChildrenFromDepths(root, m_AllItems);
            return root;
        }

        void DrawPageControls(Rect rect)
        {
            var buttonWidth = GUILayout.Width(30);

            GUILayout.BeginArea(rect);
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button(new GUIContent(k_FirstPage), EditorStyles.toolbarButton, buttonWidth))
            {
                CurrentPage = 0;
            }
            if (GUILayout.Button(new GUIContent(k_PrevPage), EditorStyles.toolbarButton, buttonWidth))
            {
                CurrentPage--;
            }

            var size = EditorStyles.textField.CalcSize(new GUIContent(CurrentPage.ToString()));
            EditorGUI.BeginChangeCheck();
            var page = EditorGUILayout.DelayedIntField(CurrentPage, EditorStyles.toolbarTextField, GUILayout.ExpandWidth(false), GUILayout.Width(size.x));
            if (EditorGUI.EndChangeCheck())
            {
                CurrentPage = page;
            }

            var totalePagesWidth = EditorStyles.textField.CalcSize(m_TotalPagesLabel).x;
            GUILayout.Label(m_TotalPagesLabel, GUILayout.Width(totalePagesWidth));
            if (GUILayout.Button(new GUIContent(k_NextPage), EditorStyles.toolbarButton, buttonWidth))
            {
                CurrentPage++;
            }
            if (GUILayout.Button(new GUIContent(k_LastPage), EditorStyles.toolbarButton, buttonWidth))
            {
                CurrentPage = TotalPages;
            }

            GUILayout.FlexibleSpace();
            EditorGUI.BeginChangeCheck();

            EditorGUI.BeginChangeCheck();
            PageSize = EditorGUILayout.DelayedIntField(k_PageSize, PageSize, EditorStyles.toolbarTextField);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        public override void OnGUI(Rect rect)
        {
            rect.yMax -= k_PageControlsHeight;
            base.OnGUI(rect);

            rect.y = rect.yMax;
            rect.height = k_PageControlsHeight;
            DrawPageControls(rect);
        }

        protected override void AfterRowsGUI()
        {
            base.AfterRowsGUI();

            if (Event.current.rawType == EventType.Repaint)
            {
                Rect rect = Rect.zero;
                for (int i = 0; i < multiColumnHeader.state.visibleColumns.Length; ++i)
                {
                    rect = multiColumnHeader.GetColumnRect(i);

                    // Calculate height without the Add Key row.
                    const float borderPadding = 2;

                    rect.height = totalHeight - borderPadding - MultiColumnHeader.DefaultGUI.defaultHeight;

                    if (CurrentPage == TotalPages)
                    {
                        // Remove lines from the New Key row
                        rect.height -= k_RowFooterHeightWithPadding + rowHeight + 4;
                    }

                    rect.x += rect.width - 1;
                    rect.width = 1;
                    MultiColumnHeaderBridge.DrawDivider(rect);
                }

                // Bottom horizontal line
                rect.x = 0;
                rect.y = rect.yMax - 1;
                rect.height = 1;
                rect.width = multiColumnHeader.state.widthOfAllVisibleColumns;
                MultiColumnHeaderBridge.DrawDivider(rect);
            }
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            // We control the background drawing so we can limit how far the alternating rows are drawn. We dont want them drawn for empty rows.
            if (Event.current.rawType == EventType.Repaint)
            {
                if (args.item.id == k_AddItemId) // Hide the selection rect for the add key row.
                    DefaultStyles.backgroundEven.Draw(args.rowRect, false, false, false, false);
                else if (args.selected)
                    k_SelectedStyle.Draw(args.rowRect, false, false, false, false);
                else if (args.row % 2 == 1)
                    DefaultStyles.backgroundEven.Draw(args.rowRect, false, false, false, false);
            }

            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                var cellRect = args.GetCellRect(i);
                var colId = args.GetColumn(i);
                var col = multiColumnHeader.GetColumn(colId);

                if (args.item.id == k_AddItemId)
                {
                    if (colId == 0)
                    {
                        DrawNewKeyField(cellRect);
                    }
                }
                else
                {
                    cellRect.height -= k_RowFooterHeight + (k_RowVerticalPadding * 3);
                    cellRect.y += k_RowVerticalPadding;

                    var item = args.item as T2;
                    switch (col)
                    {
                        case KeyColumn _:
                            // Apply cell margin as it is not applied to the first column.
                            cellRect.x += cellMargin;
                            cellRect.width -= 2f * cellMargin;

                            DrawKeyField(cellRect, item);
                            cellRect.MoveToNextLine();
                            cellRect.height = k_RowFooterHeight;
                            DrawKeyFieldFooter(cellRect, item);

                            break;

                        case KeyIdColumn _:
                            DrawKeyIdField(cellRect, item);
                            break;

                        case TableColumn<T1> tc:
                            if (Event.current.rawType == EventType.Repaint && MetadataEditorWindow.IsSelectedForEditing(tc.Table, item.KeyId))
                            {
                                var unadjustedCellRect = args.GetCellRect(i);
                                unadjustedCellRect.x -= cellMargin;
                                unadjustedCellRect.width += (2f * cellMargin) - 1;
                                EditorIcons.SelectionRect.Draw(unadjustedCellRect, string.Empty, false, false, false, false);
                            }

                            DrawItemField(cellRect, colId, tc, item);
                            cellRect.MoveToNextLine();
                            cellRect.height = k_RowFooterHeight;
                            DrawItemFieldFooter(cellRect, colId, tc, item);
                            break;

                        case MissingTableColumn mtc:
                            DrawMissingTableField(cellRect, colId, mtc);
                            break;

                        default:
                            Debug.LogError($"Unexpected column type \"{col.GetType().Name}\"");
                            break;
                    }
                }
            }
        }

        protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
        {
            return item.id != k_AddItemId && base.DoesItemMatchSearch(item, search); // Ignore add button
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var rows = base.BuildRows(root);
            if (hasSearch)
                rows.Add(m_AddKeyItem);

            TotalPages = Mathf.CeilToInt(rows.Count / (float)m_PageSize);
            m_CurrentPage = Mathf.Clamp(m_CurrentPage, 1, TotalPages);

            m_VisibleRows.Clear();
            var startIndex = (m_CurrentPage - 1) * m_PageSize;
            var endIndex = Mathf.Min(startIndex + m_PageSize, rows.Count);
            for (int i = startIndex; i < endIndex; i++)
            {
                m_VisibleRows.Add(rows[i]);
            }

            return m_VisibleRows;
        }

        protected virtual void DrawKeyIdField(Rect cellRect, T2 keyItem) => EditorGUI.LabelField(cellRect, keyItem.KeyId.ToString());

        protected virtual void DrawKeyField(Rect cellRect, T2 keyItem)
        {
            EditorGUI.BeginChangeCheck();
            var newKey = EditorGUI.TextArea(cellRect, keyItem.Key);
            if (EditorGUI.EndChangeCheck())
            {
                if (TableCollection.SharedData.Contains(newKey))
                {
                    Debug.LogWarningFormat("Cannot rename key {0} to {1}. Key must be unique and this one has already been used.", keyItem.Key, newKey);
                }
                else
                {
                    Undo.RecordObject(TableCollection.SharedData, "Rename key");
                    TableCollection.SharedData.RenameKey(keyItem.Key, newKey);
                    EditorUtility.SetDirty(TableCollection.SharedData);
                    RefreshCustomRowHeights();
                }
            }
        }

        protected virtual void DrawKeyFieldFooter(Rect cellRect, T2 keyItem)
        {
            cellRect.x += cellRect.width - k_RemoveButtonWidth;
            cellRect.width = k_RemoveButtonWidth;
            if (GUI.Button(cellRect, k_RemoveEntry))
            {
                var objects = new Object[TableCollection.Tables.Count + 1];
                for (int i = 0; i < TableCollection.Tables.Count; ++i)
                {
                    objects[i] = TableCollection.Tables[i].asset;
                }
                objects[TableCollection.Tables.Count] = TableCollection.SharedData;

                // RecordObject has a significant performance hit when removing from large tables due to GenerateUndoDiffs. (LOC-1042)
                Undo.RegisterCompleteObjectUndo(objects, "Remove key from collection");
                keyItem.OnDeleteKey();
                TableCollection.RemoveEntry(keyItem.KeyId);

                if (MetadataEditorWindow.IsSelectedForEditing(keyItem.KeyId))
                    MetadataEditorWindow.CloseWindow();

                foreach (var o in objects)
                    EditorUtility.SetDirty(o);

                Reload();
            }
        }

        /// <summary>
        /// Draws a field for adding a new key.
        /// </summary>
        protected virtual void DrawNewKeyField(Rect cellRect)
        {
            const float border = 3;
            cellRect.yMin += border;
            cellRect.height -= border;
            cellRect.xMin += border;
            cellRect.width -= border;

            if (string.IsNullOrEmpty(searchString) && GUI.Button(cellRect, k_NewEntry))
            {
                AddNewKey();
                var s = state;
                s.scrollPos += new Vector2(0, 100);
                Reload();

                CurrentPage = TotalPages;
                var index = TableCollection.SharedData.Entries.Count - 1;
                SetSelection(new int[] { index }, TreeViewSelectionOptions.RevealAndFrame);
            }
        }

        protected virtual void DrawMissingTableField(Rect cellRect, int colIdx, MissingTableColumn col)
        {
            // Just a blank field by default.
        }

        protected virtual void AddNewKey()
        {
            Undo.RecordObject(TableCollection.SharedData, "Add new key");
            TableCollection.SharedData.AddKey();
            EditorUtility.SetDirty(TableCollection.SharedData);
        }

        protected abstract void DrawItemField(Rect cellRect, int colIdx, TableColumn<T1> col, T2 item);

        protected virtual void DrawItemFieldFooter(Rect cellRect, int colIdx, TableColumn<T1> col, T2 item)
        {
            cellRect.y += k_RowVerticalPadding;
            cellRect.x += cellRect.width - k_EntryMenuButtonWidth;
            cellRect.width = k_EntryMenuButtonWidth;
            cellRect.height = 27;

            if (EditorGUI.DropdownButton(cellRect, GUIContent.none, FocusType.Passive, EditorStylesBridge.PaneOptions))
            {
                cellRect.height = EditorStylesBridge.PaneOptions.fixedHeight;

                var menu = new GenericMenu();
                PopulateEntryDropdown(menu, colIdx, col, item);
                menu.DropDown(cellRect);
            }
        }

        internal protected virtual void PopulateEntryDropdown(GenericMenu menu, int colIdx, TableColumn<T1> col, T2 item)
        {
            menu.AddItem(new GUIContent("See Metadata"), false, () =>
            {
                MetadataEditorWindow.ShowWindow(col.Table, item.KeyId);
            });

            // Add custom menu items
            object[] args = { TableCollection, col.Table, item.KeyId, menu };
            foreach (var t in TypeCache.GetMethodsWithAttribute<LocalizationEntryMenuAttribute>())
            {
                var parameters = t.GetParameters();
                if (!t.IsStatic ||
                    t.ReturnType != typeof(void) ||
                    parameters.Length != 4 ||
                    parameters[0].ParameterType != typeof(LocalizationTableCollection) ||
                    parameters[1].ParameterType != typeof(LocalizationTable) ||
                    parameters[2].ParameterType != typeof(long) ||
                    parameters[3].ParameterType != typeof(GenericMenu))
                {
                    Debug.LogError($"{nameof(LocalizationEntryMenuAttribute)} method {t.Name} must have the following signature: static void {t.Name}(LocalizationTableCollection, LocalizationTable, long, GenericMenu)");
                    continue;
                }

                t.Invoke(null, args);
            }
        }

        protected override bool CanStartDrag(CanStartDragArgs args) => true;

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            var draggedRows = args.draggedItemIDs;

            if (draggedRows.Count == 1)
            {
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.SetGenericData(k_DragId, draggedRows[0]);
                DragAndDrop.objectReferences = new Object[] {};   // this is required for dragging to work
                DragAndDrop.StartDrag("Move Key");
            }
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            if (args.dragAndDropPosition == DragAndDropPosition.OutsideItems)
                return DragAndDropVisualMode.None;

            // Check if we can handle the current drag data (could be dragged in from other areas/windows in the editor)
            var dragDropData = DragAndDrop.GetGenericData(k_DragId);
            if (dragDropData == null)
                return DragAndDropVisualMode.None;

            var itemIndex = (int)dragDropData;
            if (args.dragAndDropPosition != DragAndDropPosition.BetweenItems)
                return DragAndDropVisualMode.None;

            // Don't allow insertion on either side of the selected item, it results in no change.
            if (itemIndex == args.insertAtIndex || args.insertAtIndex == itemIndex + 1)
                return DragAndDropVisualMode.Rejected;

            if (args.performDrop)
            {
                var sharedData = TableCollection.SharedData;
                Undo.RecordObject(sharedData, "Move Key");

                var newIndex = Mathf.Clamp(args.insertAtIndex, 0, sharedData.Entries.Count);
                sharedData.Entries.Insert(newIndex, sharedData.Entries[itemIndex]);
                if (newIndex <= itemIndex)
                    ++itemIndex;
                sharedData.Entries.RemoveAt(itemIndex);
                SetSelection(new[] { newIndex });
                Reload();
            }
            return DragAndDropVisualMode.Move;
        }
    }
}
