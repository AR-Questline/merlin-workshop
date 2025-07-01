using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.EditorOnly.WorkflowTools {
    [ExecuteAlways]
    public class AssetPickerConfig : ScriptableObject {
        [Title("Add Config")]
        [InlineButton(nameof(AddConfig)), ShowInInspector, AssetsOnly]
        public GameObject[] assetsToAdd = new GameObject[0];
        [ShowInInspector, HideLabel, Tags(TagsCategory.AssetPicker)]
        public string[] tagsForAssetsToAdd = new string[0];
        
        [Title("Configs")]
        [ListDrawerSettings(DefaultExpandedState = false, NumberOfItemsPerPage = 5, HideAddButton = true), InlineButton(nameof(SortConfigs))]
        public List<AssetConfig> assetConfigs = new();

        [Title("Config Tools")]
        [CustomValueDrawer("@Awaken.TG.EditorOnly.OdinHelpers.Space(5)"), ShowInInspector] string _space_ODIN1;
        
        [OnValueChanged(nameof(UpdateEditedConfig)), AssetsOnly, FoldoutGroup("Edit Config")]
        public GameObject assetToModify;

        [HideIf(nameof(CannotModifyConfig)), ShowInInspector, HideLabel, FoldoutGroup("Edit Config")]
        public EditingAssetConfig editConfig;
        
        [CustomValueDrawer("@Awaken.TG.EditorOnly.OdinHelpers.Space(10)"), ShowInInspector] string _space_ODIN2;
        
        [InlineButton(nameof(AppendTags)), ShowInInspector, AssetsOnly, FoldoutGroup("Append Tags")]
        public GameObject[] assetsToAppendTagsTo = new GameObject[0];
        [ShowInInspector, HideLabel, Tags(TagsCategory.AssetPicker), FoldoutGroup("Append Tags")]
        public string[] tagsToAppend = new string[0];
        
        [CustomValueDrawer("@Awaken.TG.EditorOnly.OdinHelpers.Space(10)"), ShowInInspector] string _space_ODIN3;
        
        [Tags(TagsCategory.AssetPicker), ShowInInspector, HideLabel, FoldoutGroup("Remove Tags")]
        public string[] tagsToRemove = new string[0];

        [ShowInInspector, ShowIf("@assetsNoLongerInConfig.Count > 0"), FoldoutGroup("Remove Tags")]
        public readonly List<GameObject> assetsNoLongerInConfig = new();
        
        [Tags(TagsCategory.AssetPicker), ShowInInspector, HideLabel, FoldoutGroup("Rename Tag")]
        public string tagToRename = "";
        [FoldoutGroup("Rename Tag"), HorizontalGroup("Rename Tag/NewTagName"), EnableIf("@string.IsNullOrEmpty(newTagValue)"), NonSerialized, ShowInInspector]
        public string newTagKind = "";
        [FoldoutGroup("Rename Tag"), HorizontalGroup("Rename Tag/NewTagName"), EnableIf("@string.IsNullOrEmpty(newTagKind)"), NonSerialized, ShowInInspector]
        public string newTagValue = "";

        [CustomValueDrawer("@Awaken.TG.EditorOnly.OdinHelpers.Space(10)"), ShowInInspector] string _space_ODIN4;
        
        [FolderPath, FoldoutGroup("Find not configured assets"), PropertyOrder(4)]
        public string directory = "Assets/3DAssets/Props/Sets/";
        
        [ShowInInspector, ShowIf("@notConfiguredAssets.Count > 0"), FoldoutGroup("Find not configured assets"), PropertyOrder(10)]
        public List<EditingAssetConfig> notConfiguredAssets = new();
        
        bool CannotModifyConfig() => assetToModify == null || !assetConfigs.Exists(c => c.asset == assetToModify);
        
        void OnEnable() {
            UpdateEditedConfig();
#if UNITY_EDITOR
            FindNotConfiguredAssetsInDirectory();
#endif
        }

        void UpdateEditedConfig() {
            if (CannotModifyConfig()) {
                editConfig = default;
                return;
            }
            editConfig = new(assetConfigs.Find(c => c.asset == assetToModify), this);
        }
        
        void SortConfigs() {
            assetConfigs.RemoveAll(c => c.asset == null);
            assetConfigs.Sort((a, b) => string.Compare(a.asset.name, b.asset.name, StringComparison.Ordinal));
            SetDirty(this);
        }
        
        void AppendTags() {
            for (int i = 0; i < assetsToAppendTagsTo.Length; i++) {
                var asset = assetsToAppendTagsTo[i];
                var tags = tagsToAppend;
                var configIndex = assetConfigs.FindIndex(c => c.asset == asset);
                if (configIndex == -1) {
                    Log.Important?.Error("Asset not found in config, skipping: " + asset.name, asset);
                    continue;
                }
                var config = assetConfigs[configIndex];
                config.tags = config.tags.Union(tags).ToArray();
                assetConfigs[configIndex] = config;
            }
            
            assetsToAppendTagsTo = new GameObject[0];
            
            SetDirty(this);
        }

        [Button(ButtonSizes.Small), DisableIf(nameof(CannotModifyConfig)), FoldoutGroup("Edit Config")]
        void ApplyEdit() {
            assetConfigs[assetConfigs.FindIndex(c => c.asset == assetToModify)] = editConfig.ToAssetConfig();
            assetToModify = null;
            editConfig = default;
            
            SetDirty(this);
        }

        [Button, EnableIf("@tagsToRemove.Length > 0"), FoldoutGroup("Remove Tags"), GUIColor(.8f, .2f, .2f)]
        void RemoveTagsCompletely() {
            for (int i = 0; i < assetConfigs.Count; i++) {
                var assetConfig = assetConfigs[i];
                assetConfig.tags = assetConfig.tags.Where(t => !tagsToRemove.Contains(t)).ToArray();
                assetConfigs[i] = assetConfig;
            }

            assetConfigs.RemoveAll(c => {
                bool assetNoLongerConfigured = c.tags.Length == 0;
                if (assetNoLongerConfigured) {
                    assetsNoLongerInConfig.Add(c.asset);
                }

                return assetNoLongerConfigured;
            });

            for (int i = 0; i < tagsToRemove.Length; i++) {
                TagUtils.EDITOR_SAFE_RemoveTag(tagsToRemove[i], TagsCategory.AssetPicker);
            }

            tagsToRemove = Array.Empty<string>();

            SetDirty(this);
        }

        [Button(name: "@" + nameof(RenameText) + "()"), FoldoutGroup("Rename Tag"), EnableIf("@!string.IsNullOrEmpty(tagToRename) && (!string.IsNullOrEmpty(newTagKind) || !string.IsNullOrEmpty(newTagValue))")]
        void RenameTag() {
            // Modify tags in configs
            for (int i = 0; i < assetConfigs.Count; i++) {
                var assetConfig = assetConfigs[i];
                
                for (int j = 0; j < assetConfig.tags.Length; j++) {
                    var configTag = assetConfig.tags[j];
                    if (string.IsNullOrEmpty(newTagValue)) {
                        if (TagUtils.TagKind(configTag) == TagUtils.TagKind(tagToRename)) {
                            assetConfig.tags[j] = newTagKind + ":" + TagUtils.TagValue(configTag);
                        }
                    } else if (configTag == tagToRename) {
                        assetConfig.tags[j] = TagUtils.TagKind(configTag) + ":" + newTagValue;
                    }
                }
                assetConfigs[i] = assetConfig;
            }
            
            if (string.IsNullOrEmpty(newTagValue)) {
                TagUtils.EDITOR_SAFE_RenameTagKind(tagToRename, newTagKind, TagsCategory.AssetPicker);
                newTagKind = "";
            } else {
                TagUtils.EDITOR_SAFE_RenameTagValue(tagToRename, newTagValue, TagsCategory.AssetPicker);
                newTagValue = "";
            }
            
            tagToRename = "";
            SetDirty(this);
        }
        
        string RenameText() {
            if (string.IsNullOrEmpty(tagToRename)) return "Select Tag to Rename";
            
            if (string.IsNullOrEmpty(newTagValue)) {
                if (string.IsNullOrEmpty(newTagKind)) {
                    return "Enter value to change";
                }
                return "Rename Kind '" + TagUtils.TagKind(tagToRename) + "' to '" + newTagKind + "'";
            } else {
                return "Rename Value '" + TagUtils.TagValue(tagToRename) + "' to '" + newTagValue + "'";
            }
        }


#if UNITY_EDITOR
        [Button("Search"), FoldoutGroup("Find not configured assets"), HorizontalGroup("Find not configured assets/Buttons"), PropertyOrder(5)]
        void FindNotConfiguredAssetsInDirectory() {
            notConfiguredAssets.Clear();
            var assets = UnityEditor.AssetDatabase.FindAssets("t:GameObject", new[] {directory});
            foreach (var asset in assets) {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(asset);
                var obj = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (assetConfigs.All(c => c.asset != obj)) {
                    notConfiguredAssets.Add(new(obj, this));
                }
            }
        }
        [Button("Select Found"), HorizontalGroup("Find not configured assets/Buttons"), PropertyOrder(6)]
        void SelectNotConfiguredAssets() {
            UnityEditor.Selection.objects = notConfiguredAssets.Select(c => c.asset).ToArray();
        }
#endif

        void AddConfig() {
            foreach (var asset in assetsToAdd) {
                if (asset == null || assetConfigs.Any(c => c.asset == asset)) continue;
                
                var assetConfig = new AssetConfig {
                    asset = asset,
                    tags = tagsForAssetsToAdd.ToArray()
                };

                assetConfigs.Add(assetConfig);
            }
            
            assetsToAdd = new GameObject[0];
            
            SetDirty(this);
        }
        
        static void SetDirty(Object target) {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(target);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(target);
#endif
        }
        
        [Serializable]
        public struct AssetConfig {
            [AssetsOnly, HideInInspector]
            public GameObject asset;
            [Tags(TagsCategory.AssetPicker)]
            public string[] tags;
            
            [ShowInInspector, PropertyOrder(-1)]
            public GameObject Asset => asset;
        }
        
        [Serializable]
        public struct EditingAssetConfig {
            [AssetsOnly, HideInInspector]
            public GameObject asset;
            [Tags(TagsCategory.AssetPicker)]
            public string[] tags;
            
            [HideInInspector]
            public AssetPickerConfig parent;
            
            [ShowInInspector, PropertyOrder(-1), InlineButton(nameof(Apply), ShowIf = nameof(CanApply)), EnableGUI]
            public GameObject Asset => asset;

            public EditingAssetConfig(GameObject asset, AssetPickerConfig parent) {
                this.asset = asset;
                this.parent = parent;
                tags = new string[0];
            }
            
            public EditingAssetConfig(AssetConfig sourceConfig, AssetPickerConfig parent) {
                asset = sourceConfig.asset;
                tags = sourceConfig.tags.ToArray();
                this.parent = parent;
            }
            
            public AssetConfig ToAssetConfig() {
                return new() {
                    asset = asset,
                    tags = tags
                };
            }

            bool CanApply() {
                return parent.notConfiguredAssets.Any(Self);
            }
            
            void Apply() {
                parent.assetConfigs.Add(this.ToAssetConfig());
                parent.notConfiguredAssets.RemoveAll(Self);
                SetDirty(parent);
            }
            
            bool Self(EditingAssetConfig c) {
                return c.asset == asset;
            }
        }
    }
}