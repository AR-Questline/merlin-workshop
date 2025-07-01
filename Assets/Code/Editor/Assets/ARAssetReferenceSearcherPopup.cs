using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Awaken.Utility.Collections;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Awaken.TG.Editor.Assets {
    public class ARAssetReferenceSearcherPopup : EditorWindow {

        static GUIStyle s_addressButtonStyle;
        static GUIStyle AddressButtonStyle {
            get {
                if (s_addressButtonStyle == null) {
                    s_addressButtonStyle = new GUIStyle(EditorStyles.toolbarButton);
                    s_addressButtonStyle.alignment = TextAnchor.MiddleLeft;
                }

                return s_addressButtonStyle;
            }
        }
        
        SearchField _searchField;
        Action _onClose;
        Action<string, string> _onSelect;
        string _searchText = "";
        Vector2 _scroll;
        List<AddressableAssetEntry> _assets = new List<AddressableAssetEntry>();
        Dictionary<AddressableAssetEntry, List<AddressableAssetEntry>> _parents = new Dictionary<AddressableAssetEntry, List<AddressableAssetEntry>>();
        Dictionary<AddressableAssetEntry, string> _names = new Dictionary<AddressableAssetEntry, string>();
        List<AddressableAssetEntry> _firstLevelEntries = new List<AddressableAssetEntry>();
        AddressableAssetEntry _chosen;

        public static void Show(Rect displayPosition, 
            Action<string, string> onSelect,
            IEnumerable<Func<AddressableAssetGroup, bool>> groupFilter, 
            IEnumerable<Func<AddressableAssetEntry, bool>> assetFilter, 
            Action onClose = null )
        {
            var window = CreateInstance<ARAssetReferenceSearcherPopup>();
            window._onSelect = onSelect;
            window._onClose = onClose;
            window._searchField = new SearchField();
            window.position = displayPosition;
            var groupFiltersList = groupFilter.Append(BuildInGroupFilter).ToList();
            var assetFiltersList = assetFilter.ToList();
            
            AddressableAssetSettingsDefaultObject
                .GetSettings(true)
                .GetAllAssets( window._assets, true, g => groupFiltersList.All(gf => gf(g)), a => assetFiltersList.All(af => af(a)) );
            window.BuildSearchTree();
            window.ShowPopup();
            window.Focus();
        }

        void BuildSearchTree() {
            foreach (AddressableAssetEntry assetEntry in _assets.Where(assetEntry => assetEntry.IsSubAsset)) {
                if (!_parents.TryGetValue(assetEntry.ParentEntry, out var children)) {
                    children = new List<AddressableAssetEntry>();
                    _parents.Add(assetEntry.ParentEntry, children);
                }
                children.Add(assetEntry);
            }

            var entriesWithoutChildren = _assets.Where(assetEntry => !assetEntry.IsSubAsset && !_parents.ContainsKey(assetEntry));

            var singleChildEntries = _parents.Where(p => p.Value.Count == 1).Select(p => p.Key).ToList();
            foreach (var singleChildEntry in singleChildEntries) {
                _parents.Remove(singleChildEntry);
            }

            _firstLevelEntries = entriesWithoutChildren
                .Union(singleChildEntries)
                .Union(_parents.Keys)
                .OrderBy(e => e.TargetAsset.name).ToList();
            
            StringBuilder nameBuilder = new StringBuilder(128);
            foreach (AddressableAssetEntry assetEntry in _assets) {
                nameBuilder.Clear();
                nameBuilder.Append(assetEntry.TargetAsset.name);
                nameBuilder.Append(" | ");
                nameBuilder.Append(assetEntry.parentGroup.Name);
                nameBuilder.Append(" | ");
                nameBuilder.Append(assetEntry.address);
                nameBuilder.Length = 64;
                _names.Add(assetEntry, nameBuilder.ToString());
            }
        }

        void OnGUI()
        {
            if (Event.current.isKey && Event.current.keyCode == KeyCode.Escape) {
                Close();
                return;
            }

            List<AddressableAssetEntry> currentAssets = null;
            if (_chosen == null) {
                currentAssets = _firstLevelEntries;
            } else {
                currentAssets = _parents[_chosen];
            }

            EditorGUILayout.BeginHorizontal();
            var oldEnabled = GUI.enabled;
            GUI.enabled = oldEnabled && _chosen != null;
            if (GUILayout.Button("<", GUILayout.Width(20))) {
                _chosen = null;
                return;
            }
            GUI.enabled = oldEnabled;
            
            GUILayoutUtility.GetRect(10, EditorGUIUtility.singleLineHeight);
            var searchBoxRect = GUILayoutUtility.GetRect(position.width-50, EditorGUIUtility.singleLineHeight);
            _searchText = _searchField.OnGUI( searchBoxRect, _searchText );
            EditorGUILayout.EndHorizontal();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            
            bool forceChosen = false;
            if (_chosen != null) {
                EditorGUILayout.BeginHorizontal();
                var thumbnail = AssetPreview.GetMiniThumbnail(_chosen.TargetAsset);
                    
                EditorGUILayout.LabelField(new GUIContent(thumbnail), GUILayout.Width(25));

                if (GUILayout.Button(_names[_chosen], AddressButtonStyle)) {
                    forceChosen = true;
                }
                EditorGUILayout.EndHorizontal();
            }
            
            foreach (var assetEntry in currentAssets) {
                EditorGUILayout.BeginHorizontal();
                if (!forceChosen && 
                    (string.IsNullOrWhiteSpace(_searchText) || assetEntry.address.IndexOf(_searchText, StringComparison.InvariantCultureIgnoreCase) >= 0)) {
                    var thumbnail = AssetPreview.GetMiniThumbnail(assetEntry.TargetAsset);
                    
                    EditorGUILayout.LabelField(new GUIContent(thumbnail), GUILayout.Width(25));

                    if (GUILayout.Button(_names[assetEntry], AddressButtonStyle)) {
                        _chosen = assetEntry;
                        break;
                    }
                    
                    if (_parents.ContainsKey(assetEntry)) {
                        var buttonRect = GUILayoutUtility.GetLastRect();
                        buttonRect.x += (buttonRect.width - 20);
                        buttonRect.width = 20;
                        GUI.Label(buttonRect, ">");
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            if (_chosen != null) {
                if (_parents.ContainsKey(_chosen) && !forceChosen) {
                    return;
                }

                var guid = _chosen.guid;
                if (string.IsNullOrWhiteSpace(guid)) {
                    guid = _chosen.ParentEntry.guid;
                }
                
                _onSelect.Invoke(guid, _chosen.IsSubAsset ? _chosen.TargetAsset.name : null);
                Close();
            }
        }
        
        // === Closing

        void OnLostFocus() => Close();
        
        void OnDestroy()
        {
            _onClose?.Invoke();
        }
        
        // === Utils
        
        static bool BuildInGroupFilter(AddressableAssetGroup group) {
            return !group.ReadOnly;
        }
    }
}