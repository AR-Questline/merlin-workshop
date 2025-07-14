using System;
using System.Collections.Generic;
using Awaken.Utility.Collections;
using Awaken.Utility.LowLevel.Collections;
using UnityEngine;

namespace Awaken.Utility.UI {
    public struct ImguiTable<T> {
        ColumnDefinition[] _columns;
        float[] _lastColumnWidths;
        UnsafeBitmask _visibleColumns;
        float[] _totals;
        float _cellHeight;

        string _fullSearchContext;
        SearchPattern _searchContext;

        Comparison<T> _sorter;
        Comparison<T> _invertedSorter;

        List<Comparison<T>> _composedSorterParts;
        Comparison<T> _composedSorter;
        bool _composingSorting;

        Func<T, SearchPattern, bool> _searchPrediction;

        public bool ShowToolbar { get; set; }
        public bool ShowHeader { get; set; }
        public bool ShowFooter { get; set; }

        public float Padding { get; set; }
        public float Margin { get; set; }

        public event Action OnSearchChanged;

        public Comparison<T> Sorter => _composedSorter ?? _sorter;
        public UnsafeBitmask VisibleColumns => _visibleColumns;
        public SearchPattern SearchContext => _searchContext;
        public float CellHeight => _cellHeight;

        public ImguiTable(Func<T, SearchPattern, bool> searchPrediction, params ColumnDefinition[] columns) :
            this(searchPrediction, ImguiTableUtils.CellHeight, columns) {
        }

        public ImguiTable(Func<T, SearchPattern, bool> searchPrediction, float cellHeight, params ColumnDefinition[] columns) {
            _searchPrediction = searchPrediction;
            _columns = columns;
            _visibleColumns = new UnsafeBitmask((uint)columns.Length, ARAlloc.Domain);
            _visibleColumns.All();
            _sorter = _columns[0].sortAsc;
            _invertedSorter = _columns[0].sortDsc;

            _lastColumnWidths = new float[_columns.Length];
            _totals = new float[_columns.Length];

            _fullSearchContext = string.Empty;
            _searchContext = SearchPattern.Empty;
            _composedSorterParts = new List<Comparison<T>>();
            _composedSorter = null;
            _composingSorting = false;

            _cellHeight = cellHeight;

            ShowToolbar = true;
            ShowFooter = true;
            ShowHeader = true;

            Padding = 0;
            Margin = 0;

            OnSearchChanged = null;
        }

        public void Dispose() {
            _composedSorterParts.Clear();
            _visibleColumns.Dispose();
            OnSearchChanged = null;
        }

        public bool Draw(IReadOnlyList<T> elements, float viewportHeight, float viewportY, float preferredWidth) {
            var fullWidth = 0f;
            for (var i = 0; i < _columns.Length; i++) {
                if (_visibleColumns[(uint)i]) {
                    var width = _columns[i].width.GetWidth(preferredWidth);
                    _lastColumnWidths[i] = width;
                    fullWidth += width;
                }
            }

            float fullHeight = CalculateFullHeight(elements, true);

            var viewportRect = new Rect(0, viewportY, fullWidth, viewportHeight);

            var drawRect = (PropertyDrawerRects)GUILayoutUtility.GetRect(fullWidth, fullHeight);

            var clearedSorting = false;
            var sortingChanged = false;

            var oldColor = GUI.color;

            if (ShowToolbar) {
                DrawToolbar(ref drawRect, viewportRect, ref clearedSorting);
            }

            if (ShowHeader) {
                DrawHeaders(ref drawRect, viewportRect, ref sortingChanged);
            }

            if (ShowFooter) {
                DrawContentWithTotals(elements, ref drawRect, viewportRect);
            } else {
                DrawContentNoTotals(elements, ref drawRect, viewportRect);
            }

            if (ShowFooter) {
                DrawFooter(ref drawRect, viewportRect);
            }

            GUI.color = oldColor;

            if (sortingChanged & _composingSorting) {
                _composedSorterParts.Remove(_sorter);
                _composedSorterParts.Remove(_invertedSorter);

                _composedSorterParts.Add(_sorter);

                var composedSorterParts = _composedSorterParts;
                _composedSorter = (l, r) => {
                    foreach (var part in composedSorterParts) {
                        var result = part(l, r);
                        if (result != 0) {
                            return result;
                        }
                    }
                    return 0;
                };
            }

            return sortingChanged | clearedSorting;
        }

        public void SetSort(int column, bool ascending) {
            _sorter = ascending ? _columns[column].sortAsc : _columns[column].sortDsc;
            _invertedSorter = ascending ? _columns[column].sortDsc : _columns[column].sortAsc;
        }

        public float Frame(List<T> elements, int selectedIndex, float viewportHeight, bool withFilter) {
            var elementAtHeight = ImguiTableUtils.HeaderHeight;

            if (ShowToolbar) {
                elementAtHeight += ImguiTableUtils.ToolbarHeight;
            }
            if (ShowFooter) {
                elementAtHeight += ImguiTableUtils.FooterHeight;
            }

            for (var i = 0; i < selectedIndex; i++) {
                if (!withFilter || _searchPrediction(elements[i], _searchContext)) {
                    elementAtHeight += _cellHeight;
                }
            }

            return elementAtHeight - (viewportHeight - _cellHeight) / 2;
        }

        void DrawToolbar(ref PropertyDrawerRects drawRect, in Rect viewportRect, ref bool clearedSorting) {
            var toolbarRect = (PropertyDrawerRects)drawRect.AllocateTop(ImguiTableUtils.ToolbarHeight);
            var isVisible = viewportRect.Overlaps((Rect)toolbarRect);
            if (!isVisible) {
                return;
            }
            // Search
            var searchLabelRect = toolbarRect.AllocateLeft(82);
            var searchFieldRect = toolbarRect.AllocateLeftWithPadding(160, 4);
            GUI.Label(searchLabelRect, "Search:");
            var change = new TGGUILayout.CheckChangeScope();
#if UNITY_EDITOR
            _fullSearchContext = UnityEditor.EditorGUI.DelayedTextField(searchFieldRect, _fullSearchContext);
#else
            _fullSearchContext = GUI.TextField(searchFieldRect, _fullSearchContext);
#endif
            if (change) {
                var newSearch = _searchContext.Update(_fullSearchContext);
                if (newSearch != _searchContext) {
                    _searchContext = newSearch;
                    OnSearchChanged?.Invoke();
                }
            }
            change.Dispose();

            // Sorting composing
            var stopStartRect = toolbarRect.AllocateLeftWithPadding(120, 4);
            if (_composingSorting) {
                if (GUI.Button(stopStartRect, "Stop composing")) {
                    _composingSorting = false;
                }
            } else {
                if (GUI.Button(stopStartRect, "Start composing")) {
                    _composingSorting = true;
                    _composedSorter = null;
                    _composedSorterParts.Clear();
                }
            }
            if (_composedSorter != null) {
                var clearRect = toolbarRect.AllocateLeftWithPadding(120, 4);
                if (GUI.Button(clearRect, "Clear composed")) {
                    _composedSorter = null;
                    _composedSorterParts.Clear();
                    clearedSorting = true;
                }
            }
        }

        void DrawHeaders(ref PropertyDrawerRects drawRect, in Rect viewportRect, ref bool sortingChanged) {
            var headersRect = (PropertyDrawerRects)drawRect.AllocateTop(ImguiTableUtils.HeaderHeight);
            var isHeaderVisible = viewportRect.Overlaps((Rect)headersRect);
            for (var i = 0; i < _columns.Length; i++) {
                if (_visibleColumns[(uint)i] == false) {
                    continue;
                }
                _totals[i] = 0;

                if (isHeaderVisible) {
                    var column = _columns[i];
                    var columnHeaderRect = headersRect.AllocateLeft(_lastColumnWidths[i]);
                    DrawHeader(column, columnHeaderRect, ref sortingChanged);
                }
            }
        }

        void DrawContentNoTotals(IReadOnlyList<T> elements, ref PropertyDrawerRects drawRect, in Rect viewportRect) {
            var fullMargin = Margin * 2;
            var fullPadding = Padding * 2;

            for (var i = 0; i < elements.Count; i++) {
                var element = elements[i];
                if (!_searchPrediction(element, _searchContext)) {
                    continue;
                }

                var isEven = GUI.color == ImguiTableUtils.OddColor;
                GUI.color = isEven ? ImguiTableUtils.EvenColor : ImguiTableUtils.OddColor;

                var fullRowRect = drawRect.AllocateTop(_cellHeight + fullMargin);
                fullRowRect.y += Margin;
                fullRowRect.height -= fullMargin;

                var isRowVisible = viewportRect.Overlaps(fullRowRect);
                if (isRowVisible == false) {
                    continue;
                }

                Color oldRowColor = GUI.color;
                GUI.color = isEven ? ImguiTableUtils.EvenRowColor : ImguiTableUtils.OddRowColor;
                GUI.DrawTexture(fullRowRect, Texture2D.whiteTexture);
                GUI.color = oldRowColor;

                fullRowRect.y += Padding;
                fullRowRect.height -= fullPadding;

                var rowRect = (PropertyDrawerRects)fullRowRect;

                for (var j = 0; j < _columns.Length; j++) {
                    if (_visibleColumns[(uint)j] == false) {
                        continue;
                    }
                    var column = _columns[j];

                    var cellRect = rowRect.AllocateLeft(_lastColumnWidths[j]);
                    column.drawer(cellRect, element);
                }
            }
        }

        void DrawContentWithTotals(IReadOnlyList<T> elements, ref PropertyDrawerRects drawRect, in Rect viewportRect) {
            var fullMargin = Margin * 2;
            var fullPadding = Padding * 2;

            for (var i = 0; i < elements.Count; i++) {
                var element = elements[i];
                if (!_searchPrediction(element, _searchContext)) {
                    continue;
                }

                var isEven = GUI.color == ImguiTableUtils.OddColor;
                GUI.color = isEven ? ImguiTableUtils.EvenColor : ImguiTableUtils.OddColor;

                var fullRowRect = drawRect.AllocateTop(_cellHeight + fullMargin);
                fullRowRect.y += Margin;
                fullRowRect.height -= fullMargin;
                var isRowVisible = viewportRect.Overlaps(fullRowRect);
                if (isRowVisible) {
                    Color oldRowColor = GUI.color;
                    GUI.color = isEven ? ImguiTableUtils.EvenRowColor : ImguiTableUtils.OddRowColor;
                    GUI.DrawTexture(fullRowRect, Texture2D.whiteTexture);
                    GUI.color = oldRowColor;
                }

                fullRowRect.y += Padding;
                fullRowRect.height -= fullPadding;

                var rowRect = (PropertyDrawerRects)fullRowRect;

                for (var j = 0; j < _columns.Length; j++) {
                    if (_visibleColumns[(uint)i] == false) {
                        continue;
                    }
                    var column = _columns[j];

                    var value = column.toTotalExtractor(element);
                    _totals[j] += value;

                    if (isRowVisible) {
                        var cellRect = rowRect.AllocateLeft(_lastColumnWidths[j]);
                        column.drawer(cellRect, element);
                    }
                }
            }
        }

        void DrawFooter(ref PropertyDrawerRects drawRect, in Rect viewportRect) {
            GUI.color = Color.white;
            var totalsRect = (PropertyDrawerRects)drawRect.AllocateTop(ImguiTableUtils.FooterHeight);
            var isFooterVisible = viewportRect.Overlaps((Rect)totalsRect);
            if (isFooterVisible) {
                for (var i = 0; i < _columns.Length; i++) {
                    if (_visibleColumns[(uint)i] == false) {
                        continue;
                    }
                    var column = _columns[i];
                    var totalRect = totalsRect.AllocateLeft(_lastColumnWidths[i]);
                    column.totalDrawer(totalRect, _totals[i]);
                }
            }
        }

        void DrawHeader(in ColumnDefinition column, in Rect rect, ref bool sortingChanged) {
            GUI.color = ImguiTableUtils.HeaderColor;
            if (GUI.Button(rect, column.name, GUI.skin.label)) {
                if (_sorter == column.sortAsc) {
                    _sorter = column.sortDsc;
                    _invertedSorter = column.sortAsc;
                } else {
                    _sorter = column.sortAsc;
                    _invertedSorter = column.sortDsc;
                }
                sortingChanged = true;
            }
        }

        float CalculateFullHeight(IReadOnlyList<T> elements, bool withFilter) {
            var fullHeight = 0f;
            if (ShowHeader) {
                fullHeight += ImguiTableUtils.HeaderHeight;
            }
            if (ShowToolbar) {
                fullHeight += ImguiTableUtils.ToolbarHeight;
            }
            if (ShowFooter) {
                fullHeight += ImguiTableUtils.FooterHeight;
            }
            var fullMargin = Margin * 2;
            for (var i = 0; i < elements.Count; i++) {
                if (!withFilter || _searchPrediction(elements[i], _searchContext)) {
                    fullHeight += _cellHeight + fullMargin;
                }
            }
            return fullHeight;
        }

        public delegate void Drawer<in TU>(in Rect rect, TU element);

        public readonly struct ColumnDefinition {
            public readonly string name;
            public readonly Width width;

            public readonly Func<T, float> toTotalExtractor;
            public readonly Drawer<T> drawer;
            public readonly Drawer<float> totalDrawer;
            public readonly Comparison<T> sortAsc;
            public readonly Comparison<T> sortDsc;

            public static ColumnDefinition Create<U>(string name, in Width width, Drawer<T> drawer, Func<T, float> toTotalExtractor, Drawer<float> totalDrawer, Func<T, U> sortExtractor) where U : IComparable<U> {
                Comparison<T> sortAsc = (l, r) => sortExtractor(l).CompareTo(sortExtractor(r));
                Comparison<T> sortDsc = (l, r) => sortExtractor(r).CompareTo(sortExtractor(l));
                return new ColumnDefinition(name, width, drawer, toTotalExtractor, totalDrawer, sortAsc, sortDsc);
            }

            public static ColumnDefinition Create<U>(string name, Width width, Drawer<T> drawer, Func<T, U> sortExtractor) where U : IComparable<U> {
                Comparison<T> sortAsc = (l, r) => sortExtractor(l).CompareTo(sortExtractor(r));
                Comparison<T> sortDsc = (l, r) => sortExtractor(r).CompareTo(sortExtractor(l));
                return new ColumnDefinition(name, width, drawer, _ => 0, TotalDrawer, sortAsc, sortDsc);
            }

            public static ColumnDefinition CreateNumeric(string name, Width width, Drawer<float> drawer, Func<T, float> toTotalExtractor) {
                Comparison<T> sortAsc = (l, r) => toTotalExtractor(l).CompareTo(toTotalExtractor(r));
                Comparison<T> sortDsc = (l, r) => toTotalExtractor(r).CompareTo(toTotalExtractor(l));
                return new ColumnDefinition(name, width, ElementDrawer, toTotalExtractor, drawer, sortAsc, sortDsc);

                void ElementDrawer(in Rect rect, T e) => drawer(rect, toTotalExtractor(e));
            }


            public ColumnDefinition(string name, Width width, Drawer<T> drawer, Func<T, float> toTotalExtractor, Drawer<float> totalDrawer, Comparison<T> sortAsc, Comparison<T> sortDsc) {
                this.name = name;
                this.width = width;
                this.toTotalExtractor = toTotalExtractor;
                this.drawer = drawer;
                this.totalDrawer = totalDrawer;
                this.sortAsc = sortAsc;
                this.sortDsc = sortDsc;
            }

            static void TotalDrawer(in Rect rect, float _) {
                GUI.Label(rect, "Total");
            }
        }
    }
}
