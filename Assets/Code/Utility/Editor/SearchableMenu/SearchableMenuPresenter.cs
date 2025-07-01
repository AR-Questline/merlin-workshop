using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.UIToolkit;
using Awaken.Utility.Editor.UTK;
using Awaken.Utility.Editor.WindowPositioning;
using Unity.Mathematics;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.Utility.Editor.SearchableMenu {
    public class SearchableMenuPresenter : EditorWindowPresenter<SearchableMenuPresenter> {
        const string SearchFieldLabel = "toolbar-search-field";
        const string ListViewLabel = "list-view";
        const string SeparatorLabel = "separator";
        
        const int WindowMinimalWidth = 200;
        const float ListElementHeight = 26;

        static readonly Color BackgroundColor = ARColor.MainBlack;
        static SearchableMenuPresenter s_mainWindow;

        ListView _listView;
        VisualElement _separator;
        ToolbarSearchField _toolbarSearchField;

        Entry _rootEntry = new();
        List<Entry> _filteredEntries = new();

        Vector2 _windowPosition;
        Vector2 _windowSize;
        int _nestLevel;
        bool _isDescendentWindow;
        Entry _hoveredEntry;
        SearchableMenuPresenter _subWindow;

        ListElement _highlightedElement;
        ListElement _selectedElement;

        bool IsFiltering => _toolbarSearchField != null && !string.IsNullOrWhiteSpace(_toolbarSearchField.value);
        
        public void AddEntry(string label, Action action) {
            _rootEntry.AddEntry(label, action);
        }

        public void AddSeparator() {
            _rootEntry.AddEntry("----------------------------------------------------------", null);
        }

        public void ShowAtCursorPosition() {
            ShowAt(EditorWindowPlacementUtility.GetTrueMousePosition());
        }

        public void ShowAt(Vector2 position) {
            var size = CalculateWindowSize(_rootEntry.Children, false);
            s_mainWindow = this;
            _nestLevel = 0;
            SetWindowContent(_rootEntry.Children, false);
            ShowWindow(position, size, false);
        }

        public override void CreateGUI() {
            base.CreateGUI();
            InitSeparator();
            InitListView();
            InitSearchField();
        }

        protected override void CacheVisualElements(VisualElement windowRoot) {
            _separator = windowRoot.Q<VisualElement>(SeparatorLabel);
            _toolbarSearchField = windowRoot.Q<ToolbarSearchField>(SearchFieldLabel);
            _listView = windowRoot.Q<ListView>(ListViewLabel);
        }

        void ShowDescendentWindow(Vector2 windowPosition, Entry sourceEntry) {
            var newWindowSize = CalculateWindowSize(sourceEntry.Children, true);
            var desiredWindowRect = new Rect(windowPosition, newWindowSize);
            var newWindowPos = EditorWindowPlacementUtility.CalculatePositionForSubWindow(position, desiredWindowRect);
            if (_subWindow != null) {
                RefreshSubWindow(sourceEntry, newWindowPos, newWindowSize);
                return;
            }

            InitSubWindow(sourceEntry, newWindowPos, newWindowSize);
        }

        void RefreshSubWindow(Entry sourceEntry, Vector2 pos, Vector2 size) {
            _subWindow.CloseAllDescendentWindows();
            _subWindow.SetWindowContent(sourceEntry.Children);
            _subWindow.SetWindowRect(pos, size);
        }

        void InitSubWindow(Entry sourceEntry, Vector2 pos, Vector2 size) {
            _subWindow = CreateInstance<SearchableMenuPresenter>();
            _subWindow._nestLevel = _nestLevel + 1;
            _subWindow._rootEntry = sourceEntry;
            _subWindow.SetWindowContent(sourceEntry.Children, false);
            _subWindow.ShowWindow(pos, size, true);
        }

        void ShowWindow(Vector2 windowPosition, Vector2 size, bool isDescendentWindow) {
            var backgroundColor = BackgroundColor * (_nestLevel > 1 ? math.log(10 * math.pow(_nestLevel, 6)) : 1);
            rootVisualElement.style.backgroundColor = backgroundColor;
            rootVisualElement.style.minHeight = 0;
            _windowPosition = windowPosition;
            _windowSize = size;
            _isDescendentWindow = isDescendentWindow;

            var fakeButtonRect = new Rect {
                // ShowAsDropDown needs a 'button rect' to calculate the position of the window, so we fake it.
                position = _windowPosition,
                size = Vector2.zero,
            };
            
            //base.Show(); // Uncomment this line, and comment the next one to view the window in the UTK Debugger
            ShowAsDropDown(fakeButtonRect, _windowSize);
            SetWindowRect(_windowPosition, _windowSize);
        }

        Vector2 CalculateWindowSize(List<Entry> entries, bool isDescendentWindow) {
            int x = WindowMinimalWidth;
            if (entries.Any()) {
                var longestEntry = entries.OrderByDescending(EntryLength).First();
                int nameWithPathLength = longestEntry.Name.Length + PathLength(longestEntry);
                x += nameWithPathLength * 3;
            }

            float y = entries.Count * ListElementHeight + (!isDescendentWindow ? 57 : 0);
            y = Math.Min(y, Screen.currentResolution.height - 100);

            return new Vector2(x, y);

            int EntryLength(Entry s) => s.Name.Length + PathLength(s);
            int PathLength(Entry s) => IsFiltering && s.IsLeaf && !string.IsNullOrEmpty(s.Path) ? s.Path.Length : 0;
        }

        void InitSeparator() {
            _separator.SetActiveOptimized(!_isDescendentWindow);
        }

        void InitListView() {
            if (_isDescendentWindow) {
                _listView.Focus();
            }

            _listView.itemsSource = _filteredEntries;
            _listView.makeItem = () => new ListElement();
            _listView.bindItem = BindItem;
            _listView.unbindItem = UnBindItem;

            return;

            void BindItem(VisualElement element, int index) {
                var listElement = (ListElement)element;
                var entry = _filteredEntries[index];
                listElement.IsLeaf = entry.IsLeaf;
                listElement.userData = entry;
                listElement.Text = entry.Name;
                listElement.Path = IsFiltering ? entry.Path : string.Empty;

                if (entry.Action == null) {
                    // make it unable to click or hover if nothing gonna happen. It probably is just a separator.
                    listElement.SetEnabled(false);
                    return;
                }

                listElement.SetEnabled(true);

                listElement.RegisterCallback<KeyDownEvent>(KeyDown);
                listElement.RegisterCallback<PointerOverEvent>(PointerOver);
                listElement.RegisterCallback<PointerDownEvent>(PointerDown);
            }

            void UnBindItem(VisualElement listElement, int index) {
                listElement.UnregisterCallback<KeyDownEvent>(KeyDown);
                listElement.UnregisterCallback<PointerOverEvent>(PointerOver);
                listElement.UnregisterCallback<PointerDownEvent>(PointerDown);
            }

            void PointerOver(PointerOverEvent evt) {
                var visualElement = ((VisualElement)evt.target).parent;
                if (visualElement is ListElement listElement) {
                    SetHighlighted(listElement);
                }
            }

            void PointerDown(PointerDownEvent evt) {
                var visualElement = ((VisualElement)evt.target).parent;
                if (visualElement is ListElement listElement) {
                    SetSelected(listElement);
                }
            }

            void KeyDown(KeyDownEvent evt) {
                if (evt.keyCode != KeyCode.Space 
                    && evt.keyCode != KeyCode.Return) {
                    return;
                }
                var visualElement = ((VisualElement)evt.target).parent;
                if (visualElement is ListElement listElement) {
                    SetSelected(listElement);
                }
            }
        }
        
        void SetHighlighted(ListElement listElement) {
            _highlightedElement?.ToggleHighlight();
            _highlightedElement = listElement;
            _highlightedElement.ToggleHighlight();

            if (listElement.userData is not Entry entry) {
                return;
            }
                    
            if (_selectedElement != null || _hoveredEntry == entry) {
                return;
            }
            
            _hoveredEntry = entry;
            if (!_hoveredEntry.IsLeaf) {
                var pos = position.position + listElement.worldBound.position;
                ShowDescendentWindow(pos, _hoveredEntry);
            } else {
                CloseAllDescendentWindows();
            }
        }

        void SetSelected(ListElement listElement) {
            if (_selectedElement != null) {
                _selectedElement.ToggleSelect();
                _selectedElement.focusable = true;
            }

            _selectedElement = listElement;
            _selectedElement.focusable = false;
            _selectedElement.ToggleSelect();

            if (listElement.userData is not Entry entry) {
                return;
            }
                
            if (entry.IsLeaf) {
                entry.Action?.Invoke();
                CloseWindow(s_mainWindow);
                return;
            }

            var pos = position.position + listElement.worldBound.position;
            ShowDescendentWindow(pos, entry);
        }

        void InitSearchField() {
            _toolbarSearchField.SetActiveOptimized(!_isDescendentWindow);
            if (_isDescendentWindow) {
                return;
            }

            _toolbarSearchField.Q("unity-cancel").focusable = false; // Disable the cancel button in the search field
            _toolbarSearchField.Focus();
            _toolbarSearchField.RegisterCallback<ChangeEvent<string>>(OnSearchFieldChange);
        }

        void OnSearchFieldChange(ChangeEvent<string> evt) {
            if (IsFiltering) {
                RefreshFilteredView();
            } else {
                RefreshDefaultView();
            }
        }

        void RefreshDefaultView() {
            var newSize = CalculateWindowSize(_rootEntry.Children, _isDescendentWindow);
            SetWindowRect(_windowPosition, newSize);
            SetWindowContent(_rootEntry.Children);
        }

        void RefreshFilteredView() {
            var entries = GetFilteredEntry(new List<Entry>(), _rootEntry);
            var newSize = CalculateWindowSize(entries, _isDescendentWindow);
            SetWindowRect(_windowPosition, newSize);
            SetWindowContent(entries);
            return;

            List<Entry> GetFilteredEntry(List<Entry> entries, Entry entry, string path = "") {
                entry.Path = path;
                if (entry.IsLeaf) {
                    if (entry.Name.ToLower().Contains(_toolbarSearchField.value.ToLower())) {
                        entries.Add(entry);
                    }

                    return entries;
                }

                foreach (var child in entry.Children) {
                    entries = GetFilteredEntry(entries, child, string.IsNullOrEmpty(path) ? entry.Name : $"{path}/{entry.Name}");
                }

                return entries;
            }
        }

        void CloseWindow(SearchableMenuPresenter window) {
            if (window == null) return;
            window.CloseAllDescendentWindows();
            window.Close();
        }

        void CloseAllDescendentWindows() {
            if (_subWindow == null) return;

            _subWindow.CloseAllDescendentWindows();
            _subWindow.Close();
            _subWindow = null;
        }

        void SetWindowContent(IEnumerable<Entry> entries, bool refreshListView = true) {
            _filteredEntries.Clear();
            _filteredEntries.AddRange(entries);
            if (refreshListView) {
                _listView.RefreshItems();
            }
        }

        void SetWindowRect(Vector2 pos, Vector2 size) {
            _windowPosition = pos;
            _windowSize = size;
            minSize = maxSize = size;
            position = new Rect(pos, size);
        }
    }
}