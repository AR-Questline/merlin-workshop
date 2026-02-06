using System;
using System.Collections.Generic;
using System.IO;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Editor.Utility {
    public class DuplicateComponentScannerEditor : EditorWindow {
        const string WindowTitle = "Duplicate Component Scanner";
        const float HeaderPadding = 10f;
        const float ButtonHeight = 30f;
        const int ComponentColumnWidth = 200;
        const int CountColumnWidth = 60;
        const int ActionsColumnWidth = 220;

        [SerializeField] List<string> selectedFolderPaths = new() { "Assets/Scenes" };
        [SerializeField] bool excludeColliders = true;

        List<string> _currentScenePaths;
        int _currentSceneIndex;
        bool _isScanning;
        float _scanProgress;
        string _scanStatusMessage = string.Empty;

        VisualElement _foldersListContainer;
        VisualElement _root;
        Button _scanButton;
        Label _statusLabel;
        ScrollView _resultsScrollView;
        VisualElement _resultsContainer;

        readonly List<DuplicateResult> _allResults = new();
        readonly List<DuplicateResult> _scanResults = new();
        readonly DuplicateComponentScanner _scanner = new();

        [MenuItem("TG/Project Scanner/Duplicate Component Scanner")]
        public static void ShowWindow() {
            var window = GetWindow<DuplicateComponentScannerEditor>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(600, 400);
        }

        void CreateGUI() {
            _root = rootVisualElement;
            _root.style.paddingTop = HeaderPadding;
            _root.style.paddingBottom = HeaderPadding;
            _root.style.paddingLeft = HeaderPadding;
            _root.style.paddingRight = HeaderPadding;

            CreateHeaderSection();
            CreateResultsSection();
        }
        
        void CreateHeaderSection() {
            var headerContainer = new VisualElement {
                style = {
                    marginBottom = HeaderPadding
                }
            };

            var foldersLabel = new Label("Scenes Folders (subfolders included):") {
                style = {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 5
                }
            };
            headerContainer.Add(foldersLabel);

            _foldersListContainer = new VisualElement {
                style = {
                    marginBottom = HeaderPadding
                }
            };
            headerContainer.Add(_foldersListContainer);

            RefreshFoldersList();

            var addFolderButton = new Button(AddNewFolder) {
                text = "+ Add Folder",
                style = {
                    marginBottom = HeaderPadding
                }
            };
            headerContainer.Add(addFolderButton);

            var optionsContainer = new VisualElement {
                style = {
                    flexDirection = FlexDirection.Row,
                    marginBottom = HeaderPadding
                }
            };

            var excludeCollidersToggle = new Toggle("Exclude Collider Components") {
                value = excludeColliders
            };
            excludeCollidersToggle.RegisterValueChangedCallback(evt => excludeColliders = evt.newValue);
            optionsContainer.Add(excludeCollidersToggle);

            _scanButton = new Button(StartScan) {
                text = "Scan Scenes",
                style = {
                    height = ButtonHeight
                }
            };

            _statusLabel = new Label("Ready to scan") {
                style = {
                    marginTop = 5,
                    unityTextAlign = TextAnchor.MiddleLeft
                }
            };

            headerContainer.Add(optionsContainer);
            headerContainer.Add(_scanButton);
            headerContainer.Add(_statusLabel);

            _root.Add(headerContainer);
        }

        // === Folder Management
        void RefreshFoldersList() {
            _foldersListContainer.Clear();

            for (int i = 0; i < selectedFolderPaths.Count; i++) {
                var index = i;
                var folderRow = new VisualElement {
                    style = {
                        flexDirection = FlexDirection.Row,
                        marginBottom = 5
                    }
                };

                var folderField = new TextField {
                    value = selectedFolderPaths[index],
                    style = {
                        flexGrow = 1
                    }
                };
                folderField.RegisterValueChangedCallback(evt => {
                    if (index < selectedFolderPaths.Count) {
                        selectedFolderPaths[index] = evt.newValue;
                    }
                });

                var browseButton = new Button(() => BrowseFolder(index)) {
                    text = "Browse",
                    style = {
                        minWidth = 80,
                        marginLeft = 2
                    }
                };

                var removeButton = new Button(() => RemoveFolder(index)) {
                    text = "Remove",
                    style = {
                        minWidth = 80,
                        marginLeft = 2
                    }
                };

                folderRow.Add(folderField);
                folderRow.Add(browseButton);
                folderRow.Add(removeButton);

                _foldersListContainer.Add(folderRow);
            }
        }

        void AddNewFolder() {
            selectedFolderPaths.Add("Assets");
            RefreshFoldersList();
        }

        void RemoveFolder(int index) {
            if (selectedFolderPaths.Count > 1 && index < selectedFolderPaths.Count) {
                selectedFolderPaths.RemoveAt(index);
                RefreshFoldersList();
            }
        }

        void BrowseFolder(int index) {
            var currentPath = index < selectedFolderPaths.Count ? selectedFolderPaths[index] : "Assets";
            var path = EditorUtility.OpenFolderPanel("Select Scenes Folder", currentPath, "");
            if (!string.IsNullOrEmpty(path)) {
                // Convert absolute path to relative Assets path for Unity
                if (path.StartsWith(Application.dataPath)) {
                    path = "Assets" + path[Application.dataPath.Length..];
                }
                if (index < selectedFolderPaths.Count) {
                    selectedFolderPaths[index] = path;
                    RefreshFoldersList();
                }
            }
        }

        void CreateResultsSection() {
            var resultsHeader = new Label("Scan Results") {
                style = {
                    fontSize = 14,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginTop = HeaderPadding,
                    marginBottom = 5
                }
            };

            _resultsScrollView = new ScrollView {
                style = {
                    flexGrow = 1,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderTopColor = new Color(0.2f, 0.2f, 0.2f),
                    borderBottomColor = new Color(0.2f, 0.2f, 0.2f),
                    borderLeftColor = new Color(0.2f, 0.2f, 0.2f),
                    borderRightColor = new Color(0.2f, 0.2f, 0.2f),
                    paddingTop = 5,
                    paddingBottom = 5
                }
            };

            _resultsContainer = new VisualElement();
            _resultsScrollView.Add(_resultsContainer);

            _root.Add(resultsHeader);
            _root.Add(_resultsScrollView);
        }

        // === Scanning Operations
        void StartScan() {
            if (_isScanning) {
                return;
            }

            _scanResults.Clear();
            _resultsContainer.Clear();
            _isScanning = true;
            _scanButton.SetEnabled(false);

            var scenePaths = _scanner.FindScenesInMultipleFolders(selectedFolderPaths);
            if (scenePaths.Count == 0) {
                _scanStatusMessage = "No scenes found in selected folders";
                _isScanning = false;
                _scanButton.SetEnabled(true);
                return;
            }

            _scanStatusMessage = $"Found {scenePaths.Count} scenes. Starting scan...";
            _scanProgress = 0f;
            _currentScenePaths = scenePaths;
            _currentSceneIndex = 0;
            _allResults.Clear();

            EditorApplication.update += UpdateScan;
        }

        void UpdateScan() {
            if (!_isScanning) {
                return;
            }

            try {
                if (_currentSceneIndex < _currentScenePaths.Count) {
                    var scenePath = _currentScenePaths[_currentSceneIndex];
                    _scanProgress = (float)_currentSceneIndex / _currentScenePaths.Count;
                    _scanStatusMessage = $"Scanning scene {_currentSceneIndex + 1}/{_currentScenePaths.Count}: {Path.GetFileNameWithoutExtension(scenePath)}";

                    if (_statusLabel != null) {
                        _statusLabel.text = $"{_scanStatusMessage} ({_scanProgress * 100:F0}%)";
                    }

                    var sceneResults = _scanner.ScanScene(scenePath, excludeColliders);
                    _allResults.AddRange(sceneResults);

                    _currentSceneIndex++;
                } else {
                    _scanResults.AddRange(_allResults);
                    _scanProgress = 1f;
                    _scanStatusMessage = $"Scan complete. Found {_scanResults.Count} objects with duplicate components.";

                    if (_statusLabel != null) {
                        _statusLabel.text = _scanStatusMessage;
                    }

                    DisplayResults();

                    _isScanning = false;
                    _scanButton.SetEnabled(true);
                    EditorApplication.update -= UpdateScan;
                }
            } catch (Exception ex) {
                Log.Important?.Error($"Error during scan: {ex.Message}");
                _scanStatusMessage = $"Scan failed: {ex.Message}";

                if (_statusLabel != null) {
                    _statusLabel.text = _scanStatusMessage;
                }

                _isScanning = false;
                _scanButton.SetEnabled(true);
                EditorApplication.update -= UpdateScan;
            }
        }

        // === Results Display
        void DisplayResults() {
            _resultsContainer.Clear();

            if (_scanResults.Count == 0) {
                var noResultsLabel = new Label("No duplicate components found!") {
                    style = {
                        marginTop = HeaderPadding,
                        unityTextAlign = TextAnchor.MiddleCenter
                    }
                };
                _resultsContainer.Add(noResultsLabel);
                return;
            }

            var headerRow = CreateTableHeader();
            _resultsContainer.Add(headerRow);

            var groupedResults = new Dictionary<string, List<DuplicateResult>>();
            foreach (var result in _scanResults) {
                if (!groupedResults.ContainsKey(result.scenePath)) {
                    groupedResults[result.scenePath] = new List<DuplicateResult>();
                }
                groupedResults[result.scenePath].Add(result);
            }

            var sortedScenePaths = new List<string>(groupedResults.Keys);
            sortedScenePaths.Sort();

            foreach (var scenePath in sortedScenePaths) {
                var sceneLabel = new Label($"Scene: {Path.GetFileNameWithoutExtension(scenePath)}") {
                    style = {
                        fontSize = 12,
                        unityFontStyleAndWeight = FontStyle.Bold,
                        marginTop = HeaderPadding,
                        marginBottom = 5,
                        backgroundColor = new Color(0.3f, 0.3f, 0.4f, 0.5f),
                        paddingTop = 5,
                        paddingBottom = 5,
                        paddingLeft = 5
                    }
                };
                _resultsContainer.Add(sceneLabel);

                foreach (var result in groupedResults[scenePath]) {
                    var resultRow = CreateResultRow(result);
                    _resultsContainer.Add(resultRow);
                }
            }
        }

        static VisualElement CreateTableHeader() {
            var header = new VisualElement {
                style = {
                    flexDirection = FlexDirection.Row,
                    backgroundColor = new Color(0.2f, 0.2f, 0.2f),
                    paddingTop = 5,
                    paddingBottom = 5,
                    paddingLeft = 5,
                    paddingRight = 5,
                    marginBottom = 2
                }
            };

            var gameObjectCol = new Label("GameObject Path") {
                style = {
                    flexGrow = 1,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };

            var componentCol = new Label("Component") {
                style = {
                    width = ComponentColumnWidth,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };

            var countCol = new Label("Count") {
                style = {
                    width = CountColumnWidth,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    unityTextAlign = TextAnchor.MiddleCenter
                }
            };

            var actionsCol = new Label("Actions") {
                style = {
                    width = ActionsColumnWidth,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    unityTextAlign = TextAnchor.MiddleCenter
                }
            };

            header.Add(gameObjectCol);
            header.Add(componentCol);
            header.Add(countCol);
            header.Add(actionsCol);

            return header;
        }

        VisualElement CreateResultRow(DuplicateResult result) {
            var row = new VisualElement {
                style = {
                    flexDirection = FlexDirection.Row,
                    backgroundColor = new Color(0.25f, 0.25f, 0.25f),
                    paddingTop = 3,
                    paddingBottom = 3,
                    paddingLeft = 5,
                    paddingRight = 5,
                    marginBottom = 1
                }
            };

            var pathLabel = new Label(result.gameObjectPath) {
                style = {
                    flexGrow = 1,
                    overflow = Overflow.Hidden,
                    textOverflow = TextOverflow.Ellipsis
                }
            };

            var componentLabel = new Label(result.componentType) {
                style = {
                    width = ComponentColumnWidth
                }
            };

            var countLabel = new Label(result.duplicateCount.ToString()) {
                style = {
                    width = CountColumnWidth,
                    unityTextAlign = TextAnchor.MiddleCenter
                }
            };

            var actionsContainer = new VisualElement {
                style = {
                    flexDirection = FlexDirection.Row,
                    width = ActionsColumnWidth,
                    justifyContent = Justify.SpaceBetween
                }
            };

            var selectSceneButton = new Button(() => _scanner.SelectScene(result.scenePath)) {
                text = "Open Scene",
                style = {
                    fontSize = 10,
                    paddingTop = 2,
                    paddingBottom = 2,
                    marginRight = 2
                }
            };

            var selectObjectButton = new Button(() => _scanner.SelectGameObject(result)) {
                text = "Select Object",
                style = {
                    fontSize = 10,
                    paddingTop = 2,
                    paddingBottom = 2
                }
            };

            actionsContainer.Add(selectSceneButton);
            actionsContainer.Add(selectObjectButton);

            row.Add(pathLabel);
            row.Add(componentLabel);
            row.Add(countLabel);
            row.Add(actionsContainer);

            return row;
        }
    }
}
