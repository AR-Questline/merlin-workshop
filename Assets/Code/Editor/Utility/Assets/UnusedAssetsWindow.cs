using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.TG.Editor.Utility.Paths;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.Assets {
    public class UnusedAssetsWindow : EditorWindow {

        const int PathSize = 815;
        const int ItemsPerPage = 30;

        Vector2 _scroll;
        string _searchText;
        int _selected = -1;
        int _page;
        int _maxPages;
        List<string> _assets;
        List<string> _originalAssets;

        UnityEditor.Editor _editor;
        string _selectedPath;
        
        [MenuItem("TG/Assets/Find Unused Assets Window", priority = -100)]
        static void StartWindow() {
            // Get existing open window or if none, make a new one:
            UnusedAssetsWindow window = (UnusedAssetsWindow)EditorWindow.GetWindow(typeof(UnusedAssetsWindow));
            window.Show();
        }

        void OnEnable() {
            string unusedAssetsPath = $"{Application.dataPath}/{FindUnusedAssets.UnusedAssetsFileName}";
            if (!File.Exists(unusedAssetsPath)) {
                return;
            }
            
            _originalAssets = File.ReadAllLines(unusedAssetsPath).ToList();
            _assets = _originalAssets;
            _maxPages = _assets.Count / ItemsPerPage;
        }

        void OnGUI() {
            // Regenerate button
            if (GUILayout.Button("Regenerate unused assets data")) {
                FindUnusedAssets.FindUnused();
                OnEnable();
                return;
            }

            if ((_originalAssets?.Count ?? 0) < 1) {
                return;
            }
            
            // Search bar
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(85));
            EditorGUI.BeginChangeCheck();
            _searchText = EditorGUILayout.DelayedTextField(_searchText);
            if (EditorGUI.EndChangeCheck()) {
                if (string.IsNullOrWhiteSpace(_searchText)) {
                    _assets = _originalAssets;
                } else {
                    _assets = _originalAssets.Where(p => p.IndexOf(_searchText, StringComparison.InvariantCulture) >= 0).ToList();
                }
                _maxPages = _assets.Count / ItemsPerPage;
            }
            
            EditorGUILayout.Separator();
            if (GUILayout.Button("<", GUILayout.Width(25))) {
                --_page;
            }
            _page = EditorGUILayout.IntField("Page:", _page);
            if (GUILayout.Button(">", GUILayout.Width(25))) {
                ++_page;
            }

            _page = Mathf.Clamp(_page, 0, _maxPages);
            
            EditorGUILayout.EndHorizontal();
            
            // Show assets list
            int deleted = -1;
            
            EditorGUILayout.BeginHorizontal();
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MinWidth(PathSize));
            var pageStart = _page * ItemsPerPage;
            var pageEnd = Mathf.Min((_page + 1) * ItemsPerPage, _assets.Count);
            for (int index = pageStart; index < pageEnd; index++) {
                string assetPath = _assets[index];
                var assetRelativePath = PathUtils.FilesystemToAssetPath(assetPath);
                var oldColor = GUI.color;
                if (_selected == index) {
                    GUI.color = Color.blue;
                }
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(assetRelativePath, EditorStyles.label, GUILayout.ExpandWidth(true))) {
                    _selected = index;
                }
                if (GUILayout.Button("Ping", GUILayout.Width(55))) {
                    EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(assetRelativePath));
                }
                if (GUILayout.Button("X", GUILayout.Width(35))) {
                    if (EditorUtility.DisplayDialog("Delete", $"You are sure you want delete {assetRelativePath}", "Yes", "No")) {
                        AssetDatabase.DeleteAsset(assetRelativePath);
                        deleted = index;
                    }
                }
                EditorGUILayout.EndHorizontal();
                GUI.color = oldColor;
            }
            EditorGUILayout.EndScrollView();
            
            // Preview
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width - PathSize));
            if (_selected >= 0 && _selected < _assets.Count && deleted != _selected) {
                if (_selectedPath != _assets[_selected]) {
                    _selectedPath = _assets[_selected];
                    var assetRelativePath = PathUtils.FilesystemToAssetPath(_selectedPath);
                    var asset = AssetDatabase.LoadMainAssetAtPath(assetRelativePath);
                    try {
                        _editor = UnityEditor.Editor.CreateEditor(asset);
                    } catch { }
                }

                if (_editor != null) {
                    _editor.DrawPreview(GUILayoutUtility.GetRect(position.width - PathSize, position.width - PathSize));
                }
                
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            if (deleted != -1) {
                var deletedPath = _assets[deleted];
                _assets.RemoveAt(deleted);
                _originalAssets.Remove(deletedPath);
            }
        }
    }
}