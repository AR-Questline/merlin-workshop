using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.EditorOnly.WorkflowTools;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Utility.Attributes.Tags;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Awaken.TG.Editor.WorkflowTools {
    public partial class AssetPicker : OdinEditorWindow {
        const string AssetPickerConfig = "b4ef8c79756ae3742b489f7e11fc7928";

        [MenuItem("ArtTools/Asset Picker", priority = -1000)]
        static void OpenWindow() {
            GetWindow<AssetPicker>().Show();
        }
        
        [PropertySpace(0, 20)]
        [Tags(TagsCategory.AssetPicker)]
        public string[] tagFilter = Array.Empty<string>();
        
        [InlineProperty, HideLabel, HideReferenceObjectPicker, ShowInInspector]
        AssetPickerSelection _selection = new();
        
        [CustomValueDrawer("@Awaken.TG.EditorOnly.OdinHelpers.Space(20)"), ShowInInspector] string _space_ODIN1;
        
        [ListDrawerSettings(ShowPaging = true, NumberOfItemsPerPage = 8, IsReadOnly = true), ShowInInspector]
        List<FilteredAsset> _assets = new();
        
        [InlineProperty, HideLabel, HideReferenceObjectPicker, PropertySpace]
        public AssetPickerShortcutConfig shortcutConfig = new();
        
        AssetPickerConfig _configFile;
        bool _requiresUpdate = true;
        string[] _previousTags = Array.Empty<string>();
        static int s_jumpToIndex = -1;
        
        static AssetPickerConfig LoadConfig() => AssetDatabase.LoadAssetAtPath<AssetPickerConfig>(AssetDatabase.GUIDToAssetPath(AssetPickerConfig));

        protected override void OnImGUI() {
            base.OnImGUI();

            RefreshIfRequired(out var invalidState);
            if (invalidState) return;

            GUILayout.BeginHorizontal();
            // change asset to next
            if (GUILayout.Button("<< (1)")) {
                --_selection;
            }
            // Apply and spawn new asset
            if (GUILayout.Button("Spawn New (Space)")) {
                _selection.SpawnAsset();
            }
            // change asset to previous
            if (GUILayout.Button("(3) >>")) {
                ++_selection;
            }
            GUILayout.EndHorizontal();
        }

        [Shortcut("AssetPicker/Previous", KeyCode.Alpha1)]
        static void Previous(ShortcutArguments args) {
            if (!HasOpenInstances<AssetPicker>()) return;
            GetWindow<AssetPicker>()._selection--;
        }

        [Shortcut("AssetPicker/Next", KeyCode.Alpha3)]
        static void Next(ShortcutArguments args) {
            if (!HasOpenInstances<AssetPicker>()) return;
            GetWindow<AssetPicker>()._selection++;
        }
        
        [Shortcut("AssetPicker/Spawn", KeyCode.Space)]
        static void SpawnNext(ShortcutArguments args) {
            if (!HasOpenInstances<AssetPicker>()) return;
            GetWindow<AssetPicker>()._selection.SpawnAsset();
        }

        void RefreshIfRequired(out bool invalidState) {
            invalidState = false;
            
            if (_configFile == null) {
                _configFile = LoadConfig();
                if (_configFile == null) {
                    GUILayout.Label("No AssetPickerConfig found !!!");
                    invalidState = true;
                    return;
                }
                _requiresUpdate = true;
            }
            
            if (!tagFilter.SequenceEqual(_previousTags)) {
                _previousTags = tagFilter;
                _requiresUpdate = true;
            }
            
            if (_requiresUpdate) {
                UpdateAssets();
                _requiresUpdate = false;
            }
            
            if (s_jumpToIndex != -1) {
                _selection.Index = s_jumpToIndex;
                s_jumpToIndex = -1;
            }
        }

        protected override void OnEnable() {
            base.OnEnable();
            _selection.AssignAssets(_assets);
            shortcutConfig.Init();
        }

        void UpdateAssets() {
            _assets.Clear();
            
            const int WasAssetSelected = -1; // try find or replace with new set
            const int NoAssetSelected = -2; // keep it unselected
            
            int newIndex = _selection.SelectedAsset != null ? WasAssetSelected : NoAssetSelected;
            
            for (var i = 0; i < _configFile.assetConfigs.Count; i++) {
                AssetPickerConfig.AssetConfig config = _configFile.assetConfigs[i];
                // does config contain all tags in filter?
                if (TagUtils.HasRequiredTags(config.tags, tagFilter)) {
                    _assets.Add(new FilteredAsset {
                        asset = config.asset,
                        index = _assets.Count
                    });
                    if (newIndex != NoAssetSelected && config.asset == _selection.SelectedAsset) {
                        newIndex = i;
                    }
                }
            }

            _selection.AssetsChanged();
            if (newIndex != NoAssetSelected) {
                if (newIndex == WasAssetSelected) {
                    newIndex = 0;
                }
                _selection.Index = newIndex;
            }
        }
    }
}