using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Editor.Utility.Localization {
    public class DuplicateLocalizationsScannerEditor : EditorWindow {
        const string WindowTitle = "Duplicate Localization Scanner";
        const float HeaderPadding = 10f;
        const float ButtonHeight = 30f;
        const int EnLocalizationColumnWidth = 400;
        const int TableColumnWidth = 80;

        [SerializeField] bool excludeStory = true;

        VisualElement _foldersListContainer;
        VisualElement _root;
        Button _scanButton;
        ScrollView _resultsScrollView;
        VisualElement _resultsContainer;

        List<DuplicateLocResult> _scanResults = new();

        [MenuItem("TG/Localization/Duplicate Localization Scanner")]
        public static void ShowWindow() {
            var window = GetWindow<DuplicateLocalizationsScannerEditor>();
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

            var optionsContainer = new VisualElement {
                style = {
                    flexDirection = FlexDirection.Row,
                    marginBottom = HeaderPadding
                }
            };

            var excludeStoryToggle = new Toggle("Exclude Store Table") {
                value = excludeStory
            };
            excludeStoryToggle.RegisterValueChangedCallback(evt => excludeStory = evt.newValue);
            optionsContainer.Add(excludeStoryToggle);

            _scanButton = new Button(StartScan) {
                text = "Scan Tables",
                style = {
                    height = ButtonHeight
                }
            };

            headerContainer.Add(optionsContainer);
            headerContainer.Add(_scanButton);

            _root.Add(headerContainer);
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
            _resultsContainer.Clear();
            _scanResults = DuplicateLocalizationsScanner.GetAllDuplicates(!excludeStory);
            DisplayResults();
        }

        // === Results Display
        void DisplayResults() {
            _resultsContainer.Clear();

            if (_scanResults.Count == 0) {
                var noResultsLabel = new Label("No duplicate locs found!") {
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

            foreach (var locResult in _scanResults) {
                var row = new VisualElement {
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
                
                // TextField allows for copying text easily
                var locLabel = new TextField() {
                    value = locResult.locString,
                    isReadOnly = true,
                    multiline = true,
                    style = {
                        fontSize = 12,
                        unityFontStyleAndWeight = FontStyle.Bold,
                        marginTop = HeaderPadding,
                        marginBottom = 5,
                        backgroundColor = new Color(0.3f, 0.3f, 0.4f, 0.5f),
                        flexGrow = 1,
                        textOverflow = TextOverflow.Ellipsis,
                    }
                };
                
                var enLabel = new Label(locResult.enText) {
                    style = {
                        fontSize = 10,
                        width = EnLocalizationColumnWidth,
                        textOverflow = TextOverflow.Ellipsis,
                    }
                };
                
                row.Add(locLabel);
                row.Add(enLabel);

                for (int i = 0; i < locResult.results.Length; i++) {
                    bool result = locResult.results[i];
                    var resultRow = new Label(result ? DuplicateLocResult.LocTables[i] : "-") {
                        style = {
                            width = TableColumnWidth,
                            unityFontStyleAndWeight = FontStyle.Bold,
                            unityTextAlign = TextAnchor.MiddleCenter,
                            backgroundColor = result 
                            ? new Color(0.7f, 0.7f, 0.7f, 0.5f)
                            : new Color(0.3f, 0.3f, 0.4f ,0.5f),
                        }
                    };
                    row.Add(resultRow);
                }
                _resultsContainer.Add(row);
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

            var locCol = new Label("Loc String") {
                style = {
                    flexGrow = 1,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };

            var enCol = new Label("EN") {
                style = {
                    width = EnLocalizationColumnWidth,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };
            
            header.Add(locCol);
            header.Add(enCol);

            foreach (var table in DuplicateLocResult.LocTables) {
                var tableCol = new Label(table) {
                    style = {
                        width = TableColumnWidth,
                        unityFontStyleAndWeight = FontStyle.Bold,
                        unityTextAlign = TextAnchor.MiddleCenter
                    }
                };
                header.Add(tableCol);
            }

            return header;
        }
    }
}
