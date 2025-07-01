using System;
using System.Linq;
using Awaken.TG.Editor.DataViews.Data;
using Awaken.TG.Editor.DataViews.Headers;
using Awaken.TG.Editor.DataViews.Structure;
using Awaken.TG.Editor.DataViews.Utils;
using Awaken.TG.Editor.Helpers;
using Awaken.TG.Editor.Utility.Localization;
using Awaken.TG.Main.Crafting.Recipes;
using Awaken.Utility.Collections;
using Awaken.Utility.UI;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.DataViews {
    public class DataView : EditorWindow {
        public const char ExportDataSeparator = '\t';
        public const string ExportDataExtension = ".tsv";
        
        public const int ScrollSize = 20;
        
        public const int NamesWidth = 300;
        public const int BulkSelectionWidth = 40;
        public const int PopupWidth = PopupButtonWidth * 6;
        public const int PopupButtonWidth = 60;
        public const int HeadersHeight = 2;

        static GUIStyle s_headerStyle;
        static GUIStyle s_nameButtonStyle;
        
        static GUIStyle HeaderStyle => s_headerStyle ??= new(EditorStyles.label) {
            wordWrap = true
        };
        static GUIStyle NameButtonStyle => s_nameButtonStyle ??= new(EditorStyles.objectField) {
            alignment = TextAnchor.MiddleLeft
        };

        static readonly GUIContent NoneContent = new("None");
        public static float Padding => DataViewPreferences.Instance.padding;

        GenericMenu.MenuFunction2 _changeTypeDelegate;
        GenericMenu.MenuFunction2 ChangeTypeDelegate => _changeTypeDelegate ??= o => ChangeType(o as DataViewTab);

        DataViewTab _tab;
        GUIContent _typeName = NoneContent;
        DataViewHeader[] _headers = Array.Empty<DataViewHeader>();
        float _rowHeight;

        DataViewRow[] _originalRows = Array.Empty<DataViewRow>();
        DataViewRow[] _filteredRows = Array.Empty<DataViewRow>();
        int _selectedObjectCount;
        int _lastSelectedObjectIndex = -1;
        string _bulkLineName;
        DataViewBulkEditor _bulkEditor;
        
        float _scrollX, _scrollY;
        int _scrollXInt, _scrollYInt;
        int _hoveredRow;
        int _hoveredRowTemp;

        bool _drawSort;
        bool _drawFilter;
        bool _drawExport;
        bool _drawImport;

        DataViewSorterPopup _sorter = new();
        DataViewFilterPopup _filter = new();
        DataViewExporterPopup _exporter = new();
        DataViewImporterPopup _importer = new();
        
        static DataViewTab[] AllTabs => DataViewSharedData.Get().tabs;
        
        [MenuItem("TG/Design/Data View")]
        static void ShowWindow() {
            var window = GetWindow<DataView>();
            window.titleContent = new GUIContent("Data View");
        }

        void OnEnable() {
            wantsMouseMove = true;
            Undo.undoRedoPerformed += Refresh;
            DataViewStyle.Refresh();
            EditorRecipeCache.ResetCache();
        }

        void OnDisable() {
            ClearMetadata();
            Undo.undoRedoPerformed -= Refresh;
        }

        protected void OnGUI() {
            DataViewStyle.Validate();
            var rect = position;
            rect.x = 0;
            rect.y = 0;
            var rects = new PropertyDrawerRects(rect);
            rects.AllocateLeft(5);
            DrawToolbar(rects.AllocateLine());
            if (_tab == null) {
                _typeName = NoneContent;
                return;
            }

            rects.AllocateBottom(ScrollSize);
            
            var namesRects = new PropertyDrawerRects(rects.AllocateLeft(NamesWidth));
            rects.AllocateLeft(Padding * 2);

            bool drawPopup = _drawSort || _drawFilter || _drawExport || _drawImport;
            Rect popupRect = default;
            
            if (drawPopup) {
                popupRect = rects.AllocateRight(PopupWidth);
            }
            
            namesRects.AllocateBottom(ScrollSize);
            var horizontalScrollBarRect = rects.AllocateBottom(ScrollSize);
            horizontalScrollBarRect.width -= ScrollSize;
            var verticalScrollBarRect = rects.AllocateRight(ScrollSize);
            
            using (new EditorGUI.DisabledScope(drawPopup)) {
                DrawHeaders(namesRects.AllocateTop(HeadersHeight * EditorGUIUtility.singleLineHeight), rects.AllocateTop(HeadersHeight * EditorGUIUtility.singleLineHeight));
                
                _bulkEditor.Draw(
                    namesRects.AllocateTop(DataViewBulkEditor.Height), 
                    rects.AllocateTop(DataViewBulkEditor.Height), 
                    _headers, _filteredRows, 
                    _scrollXInt, ref _bulkLineName, ref _selectedObjectCount
                );
                namesRects.AllocateTop(Padding * 2);
                rects.AllocateTop(Padding * 2);

                _hoveredRowTemp = -1;
                DrawRows((Rect)namesRects, (Rect)rects);
                if (_hoveredRow != _hoveredRowTemp) {
                    _hoveredRow = _hoveredRowTemp;
                    Repaint();
                }
            }
            
            HandleScrolls(horizontalScrollBarRect, verticalScrollBarRect);
            
            if (drawPopup) {
                EditorGUI.DrawRect(popupRect, DataViewPreferences.Instance.PopupBackground);
                const float Margin = 10;
                popupRect.x += Margin;
                popupRect.y += Margin;
                popupRect.width -= Margin * 2;
                popupRect.height -= Margin * 2;
                if (_drawSort) {
                    _sorter.Draw(popupRect, out bool changed);
                    if (changed) {
                        _sorter.Sort(_filteredRows, _headers);
                    }
                } else if (_drawFilter) {
                    _filter.Draw(popupRect, _tab.Archetype.Headers, out bool changed);
                    if (changed) {
                        _filteredRows = _filter.Filter(_originalRows,  _tab.Archetype.Headers);
                        _sorter.Sort(_filteredRows, _headers);
                    }
                } else if (_drawExport) {
                    _drawExport = _exporter.Draw(popupRect, _filteredRows, _headers);
                } else if (_drawImport) {
                    _drawImport = _importer.Draw(popupRect, _headers);
                }
            }
        }

        void DrawToolbar(Rect rect) {
            EditorGUI.DrawRect(rect, DataViewPreferences.Instance.headerColor);
            var rects = new PropertyDrawerRects(rect);
            if (EditorGUI.DropdownButton(rects.AllocateLeft(PopupButtonWidth * 2), _typeName, FocusType.Keyboard)) {
                var menu = new GenericMenu();
                foreach (var tab in AllTabs) {
                    menu.AddItem(new GUIContent(tab.name), false, ChangeTypeDelegate, tab);
                }
                menu.AddSeparator("");
                menu.AddItem(NoneContent, false, ChangeTypeDelegate, null);
                menu.DropDown(new Rect(new Vector2(rect.xMin, rect.yMax + 5), Vector2.zero));
            }
            if (_tab == null) {
                return;
            }
            rects.AllocateWithRest(PopupWidth);
            if (GUI.Button(rects.AllocateLeft(PopupButtonWidth), "Data")) {
                var data = DataViewSharedData.Get();
                EditorGUIUtility.PingObject(data);
                Selection.activeObject = data;
            }
            if (GUI.Button(rects.AllocateLeft(PopupButtonWidth), "Refresh")) {
                Refresh();
                DataViewStyle.Validate();
            }
            using (new ColorGUIScope(!_drawSort, DataViewPreferences.Instance.NotActivePopupButtonBackground)) {
                if (GUI.Button(rects.AllocateLeft(PopupButtonWidth), "Sort")) {
                    _drawSort = !_drawSort;
                    _drawFilter = false;
                    _drawExport = false;
                    _drawImport = false;
                }
            }
            using (new ColorGUIScope(!_drawFilter, DataViewPreferences.Instance.NotActivePopupButtonBackground)) {
                if (GUI.Button(rects.AllocateLeft(PopupButtonWidth), "Filter")) {
                    _drawSort = false;
                    _drawFilter = !_drawFilter;
                    _drawExport = false;
                    _drawImport = false;
                }
            }
            using (new ColorGUIScope(!_drawExport, DataViewPreferences.Instance.NotActivePopupButtonBackground)) {
                if (GUI.Button(rects.AllocateLeft(PopupButtonWidth), "Export")) {
                    _drawSort = false;
                    _drawFilter = false;
                    _drawExport = !_drawExport;
                    _drawImport = false;
                }
            }
            using (new ColorGUIScope(!_drawImport, DataViewPreferences.Instance.NotActivePopupButtonBackground)) {
                if (GUI.Button(rects.AllocateLeft(PopupButtonWidth), "Import")) {
                    _drawSort = false;
                    _drawFilter = false;
                    _drawExport = false;
                    _drawImport = !_drawImport;
                }
            }
        }
        
        void DrawHeaders(Rect namesRect, Rect contentRect) {
            var contentRects = new PropertyDrawerRects(contentRect);
            
            var mousePosition = Event.current.mousePosition;
            int? hoveredHeader = null;
            Rect hoveredRect = Rect.zero;

            EditorGUI.LabelField(namesRect, "Object");
            for (int i = _scrollXInt; i < _headers.Length; i++) {
                var header = _headers[i];
                contentRects.AllocateLeft(Padding);
                if (contentRects.TryAllocateLeft(header.Width, out var headerRect)) {
                    if (headerRect.x < mousePosition.x && mousePosition.x < headerRect.x + headerRect.width) {
                        hoveredHeader = i;
                        hoveredRect = headerRect;
                        if (headerRect.y < mousePosition.y && mousePosition.y < headerRect.y + headerRect.height) {
                            var evt = Event.current;
                            if (evt.type is EventType.MouseDown && evt.button == 0) {
                                _sorter.ToggleSort(i);
                                _sorter.Sort(_filteredRows, _headers);
                                evt.Use();
                            }
                        }
                    } else {
                        EditorGUI.LabelField(headerRect, header.NiceName, HeaderStyle);
                    }
                    contentRects.AllocateLeft(Padding);
                } else {
                    break;
                }
            }

            if (hoveredHeader.HasValue) {
                var header = _headers[hoveredHeader.Value];
                float width = GUI.skin.label.CalcSize(header.NiceName).x;
                
                var labelRect = hoveredRect;
                labelRect.width = Math.Max(labelRect.width, width);
                labelRect.y -= (EditorGUIUtility.singleLineHeight - labelRect.height) / 2;
                labelRect.height = EditorGUIUtility.singleLineHeight;

                const int Margin = 2;
                var backgroundRect = labelRect;
                backgroundRect.height += Margin * 2;
                backgroundRect.width += Margin * 2;
                backgroundRect.x -= Margin;
                backgroundRect.y -= Margin;
                
                EditorGUI.DrawRect(backgroundRect, DataViewPreferences.Instance.bgHover.background);
                EditorGUI.LabelField(labelRect, header.NiceName);
            }
        }

        void DrawRows(Rect namesRect, Rect contentRect) {
            var previousBackground = GUI.backgroundColor;
            var mousePosition = Event.current.mousePosition;
            var namesRects = new PropertyDrawerRects(namesRect);
            var contentRects = new PropertyDrawerRects(contentRect);
            for (int i = _scrollYInt; i < _filteredRows.Length; i++) {
                namesRects.AllocateTop(Padding);
                contentRects.AllocateTop(Padding);
                if (namesRects.TryAllocateTop(_rowHeight, out var nameRect) &&
                    contentRects.TryAllocateTop(_rowHeight, out var rowRect)) {
                    Color background;
                    Color fieldColor;
                    if (nameRect.Expand(Padding).Contains(mousePosition) || rowRect.Expand(Padding).Contains(mousePosition)) {
                        _hoveredRowTemp = i;
                        background = DataViewPreferences.Instance.bgHover.background;
                        fieldColor = DataViewPreferences.Instance.bgHover.fieldColor;
                    } else if (_filteredRows[i].selected) {
                        background = DataViewPreferences.Instance.bgBulk.background[i];
                        fieldColor = DataViewPreferences.Instance.bgBulk.fieldColor[i];
                    } else {
                        background = DataViewPreferences.Instance.bgNormal.background[i];
                        fieldColor = DataViewPreferences.Instance.bgNormal.fieldColor[i];
                    }
                    EditorGUI.DrawRect(nameRect.Expand(Padding), background);
                    EditorGUI.DrawRect(rowRect.Expand(Padding), background);
                    GUI.backgroundColor = fieldColor;

                    ref readonly var row = ref _filteredRows[i];
                    DrawName(i, row, nameRect);
                    DrawRow(row, rowRect);
                    namesRects.AllocateTop(Padding);
                    contentRects.AllocateTop(Padding);
                } else {
                    break;
                }
            }
            GUI.backgroundColor = previousBackground;
        }

        void DrawName(int index, in DataViewRow row, Rect rect) {
            var rects = new PropertyDrawerRects(rect);
            
            bool wasSelected = _filteredRows[index].selected;
            bool isSelected = EditorGUI.Toggle(rects.AllocateLeft(BulkSelectionWidth), wasSelected);
            if (wasSelected != isSelected) {
                if (Event.current.shift && _lastSelectedObjectIndex != index && _lastSelectedObjectIndex != -1) {
                    (int start, int end) = index > _lastSelectedObjectIndex ? (_lastSelectedObjectIndex, index) : (index, _lastSelectedObjectIndex);
                    for (int i = start; i <= end; i++) {
                        if (!_filteredRows[i].selected) {
                            _selectedObjectCount++;
                            _filteredRows[i].selected = true;
                        }
                    }
                    _lastSelectedObjectIndex = index;
                } else {
                    _filteredRows[index].selected = isSelected;
                    _lastSelectedObjectIndex = index;
                    _selectedObjectCount += isSelected ? 1 : -1;
                }
                _bulkLineName = $"Bulk Edit ({_selectedObjectCount})";
            }
            
            EditorGUI.LabelField(rects.AllocateLeft(30), index.ToString());
            if (GUI.Button((Rect)rects, row.simpleName, NameButtonStyle)) {
                EditorGUIUtility.PingObject(row.source.UnityObject);
                Selection.activeObject = row.source.UnityObject;
            }
        }
        
        void DrawRow(in DataViewRow row, Rect rect) {
            var rects = new PropertyDrawerRects(rect);
            for (int i = _scrollXInt; i < _headers.Length; i++) {
                var header = _headers[i];
                rects.AllocateLeft(Padding);
                if (rects.TryAllocateLeft(header.Width, out var headerRect)) {
                    header.Draw(row[i], headerRect);
                    rects.AllocateLeft(Padding);
                } else {
                    break;
                }
            }
        }

        void HandleScrolls(in Rect horizontal, in Rect vertical) {
            const float VerticalScrollFactor = 1;
            const float HorizontalScrollFactor = 0.2f;
            
            _scrollX = Mathf.Clamp(_scrollX, 0, _headers.Length - 1);
            _scrollY = Mathf.Clamp(_scrollY, 0, _filteredRows.Length - 1);
            
            _scrollX = GUI.HorizontalScrollbar(horizontal, _scrollX, 1, 0, _headers.Length - 1);
            _scrollY = GUI.VerticalScrollbar(vertical, _scrollY, 1, 0, _filteredRows.Length - 1);

            var evt = Event.current;
            if (evt.type == EventType.ScrollWheel) {
                var delta = evt.delta;
                _scrollX += delta.x * HorizontalScrollFactor;
                _scrollY += delta.y * VerticalScrollFactor;
                evt.Use();
            }
            
            _scrollXInt = Mathf.Clamp(Mathf.RoundToInt(_scrollX), 0, _headers.Length - 1);
            _scrollYInt = Mathf.Clamp(Mathf.RoundToInt(_scrollY), 0, _filteredRows.Length - 1);
        }

        void Refresh() {
            DataViewStyle.Refresh();
            DataViewArchetype.RefreshHeaderCache();
            DataViewPropertyHeader.ClearCache();
            ForceChangeTab(_tab);
        }

        
        void ChangeType(DataViewTab tab) {
            if (_tab != tab) {
                ForceChangeTab(tab);
            }
        }

        void ForceChangeTab(DataViewTab tab) {
            bool bigChange = _tab != tab;
            _tab = tab;
            ClearMetadata();
            if (_tab != null) {
                var newHeaders = _tab.GetHeaders().ToArray();
                var newSources = _tab.GetFilteredObjects().ToArray();
                bigChange |= IsBigContentChange(newHeaders, newSources);
                _typeName = new GUIContent(_tab.name);
                _headers = newHeaders;
                _rowHeight = _tab.RowHeight;
                _originalRows = DataViewRow.Create(newSources, _headers);
            } else {
                bigChange = true;
                _typeName = NoneContent;
                _headers = Array.Empty<DataViewHeader>();
                _rowHeight = 0;
                _originalRows = Array.Empty<DataViewRow>();
            }
            
            DataViewPropertyHeader.ClearCache();
            _bulkEditor.Refresh(_headers);
            _sorter.Refresh(_headers);
            
            if (bigChange) {
                _filteredRows = ArrayUtils.CreateCopy(_originalRows);
                _sorter.Sort(_filteredRows, _headers);
                
                _sorter.Clear();
                _filter.Clear();
                _selectedObjectCount = 0;
                _lastSelectedObjectIndex = -1;
                
                _drawSort = false;
                _drawFilter = false;
                _drawExport = false;
                _drawImport = false;
                
                _scrollX = 0;
                _scrollY = 0;
                _scrollXInt = 0;
                _scrollYInt = 0;
            } else {
                var previousFilteredRows = _filteredRows;
                _filteredRows = _filter.Filter(_originalRows, _tab.Archetype.Headers);
                _sorter.Sort(_filteredRows, _headers);
                for (int i = 0; i < _filteredRows.Length; i++) {
                    _filteredRows[i].selected = previousFilteredRows[i].selected;
                }
            }
        }

        
        bool IsBigContentChange(DataViewHeader[] newHeaders, IDataViewSource[] newSources) {
            if (_headers.Length != newHeaders.Length) {
                return true;
            }
            for (int i = 0; i < newHeaders.Length; i++) {
                if (!_headers[i].Equals(newHeaders[i])) {
                    return true;
                }
            }
            if (newSources.Length != _originalRows.Length) {
                return true;
            }
            for (int i = 0; i < newSources.Length; i++) {
                if (_originalRows[i].source != newSources[i]) {
                    return true;
                }
            }
            return false;
        }
        
        void ClearMetadata() {
            for (int i = 0; i < _originalRows.Length; i++) {
                ref var row = ref _originalRows[i];
                for (int j = 0; j < _headers.Length; j++) {
                    _headers[j].FreeMetadata(ref row.headerMetadata[j]);
                    _headers[j].Type.FreeMetadata(ref row.typeMetadata[j]);
                }
            }
            _bulkEditor.Clear(_headers);
        }
    }
}