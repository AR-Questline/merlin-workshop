using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Awaken.TG.Editor.WorkflowTools {
    public partial class AssetPicker {
        [Serializable]
        public class AssetPickerShortcutConfig {
            const string PrefsKey = "AssetPickerKeyConfigs";
            [Serializable]
            public class KeyConfig {
                [HideInInspector]
                public KeyCode key;
                [Tags(TagsCategory.AssetPicker)]
                public string[] tags;
                
                [ShowInInspector, DisplayAsString, PropertyOrder(-1), HideLabel, EnableGUI, GUIColor(1.8f, 1.8f, 1.8f)]
                public string KeyLabel => key.ToString().Replace("Alpha", "Key: ");
            }

            [SerializeField, OnValueChanged(nameof(OnConfigPresetChanged))]
            int loadedConfig;
            [SerializeField, HideInInspector]
            int previouslyLoadedConfig;
            
            [Space]
            [SerializeField, FoldoutGroup("Config Settings"), PropertyOrder(2)]
            List<KeyConfig> keyConfigs;

            [SerializeField, OnValueChanged(nameof(SetConfigNames), true), FoldoutGroup("Config Settings"), PropertyOrder(4)]
            List<string> configLabels = new() {"Default"};

            // List<ValueDropdownItem<int>> GetConfigPresetsDropdownValues() {
            //     int iter = 0;
            //     return configLabels.Select(preset => new ValueDropdownItem<int>(preset, iter++)).ToList();
            // }
            
            List<string> GetConfigNames() => EditorPrefs.GetString(PrefsKey + ":Names", "").Split(';', options: StringSplitOptions.RemoveEmptyEntries).ToList();
            void SetConfigNames() => EditorPrefs.SetString(PrefsKey + ":Names", string.Join(";", configLabels));
            
            List<KeyConfig> GetConfigs(int index) {
                string s = EditorPrefs.GetString(PrefsKey + ":" + index, "");
                if (string.IsNullOrEmpty(s)) {
                    return EmptyConfigSet();
                }
                return s
                       .Split(';')
                       .Select(configSet => configSet.Split(','))
                       .Select(config => new KeyConfig {
                           key = (KeyCode) Enum.Parse(typeof(KeyCode), config[0]),
                           tags = config.Skip(1).ToArray()
                       }).ToList();
            }
            
            List<KeyConfig> GetLegacyConfigs() {
                string s = EditorPrefs.GetString(PrefsKey, "");
                if (string.IsNullOrEmpty(s)) return new List<KeyConfig>();
                return s
                       .Split(';')
                       .Select(configSet => configSet.Split(','))
                       .Select(config => new KeyConfig {
                           key = (KeyCode) Enum.Parse(typeof(KeyCode), config[0]),
                           tags = config.Skip(1).ToArray()
                       }).ToList();
            }

            void SetConfigs(List<KeyConfig> configs, int index) => EditorPrefs.SetString(PrefsKey + ":" + index, string.Join(";", configs.Select(config => $"{config.key},{string.Join(",", config.tags)}")));
            
            public void Init() {
                configLabels = GetConfigNames();
                if (configLabels.IsNullOrEmpty()) {
                    configLabels.Add("Default");
                    SetConfigNames();
                }
                
                List<KeyConfig> savedConfigs = GetConfigs(loadedConfig);
                previouslyLoadedConfig = loadedConfig;
                
                this.keyConfigs = savedConfigs;
            }
            
            [Button("Remember to Save", ButtonSizes.Small, Icon = SdfIconType.Exclamation), GUIColor(0.8f, 0.2f, 0.2f), FoldoutGroup("Config Settings"), PropertyOrder(1)]
            void Save() {
                SetConfigs(keyConfigs, loadedConfig);
            }
            
            void OnConfigPresetChanged() {
                SetConfigs(keyConfigs, previouslyLoadedConfig);
                keyConfigs = GetConfigs(loadedConfig);
                previouslyLoadedConfig = loadedConfig;
            }

            static List<KeyConfig> EmptyConfigSet() {
                List<KeyConfig> newSet = new();
                newSet.Add(new KeyConfig {
                    key = KeyCode.Alpha4,
                    tags = new string[0]
                });
                newSet.Add(new KeyConfig {
                    key = KeyCode.Alpha5,
                    tags = new string[0]
                });
                newSet.Add(new KeyConfig {
                    key = KeyCode.Alpha6,
                    tags = new string[0]
                });
                newSet.Add(new KeyConfig {
                    key = KeyCode.Alpha7,
                    tags = new string[0]
                });
                newSet.Add(new KeyConfig {
                    key = KeyCode.Alpha8,
                    tags = new string[0]
                });
                newSet.Add(new KeyConfig {
                    key = KeyCode.Alpha9,
                    tags = new string[0]
                });
                newSet.Add(new KeyConfig {
                    key = KeyCode.Alpha0,
                    tags = new string[0]
                });
                return newSet;
            }

            static void ApplyTagsForShortcut(KeyCode key) {
                if (!HasOpenInstances<AssetPicker>()) return;
                var assetPicker = GetWindow<AssetPicker>(true, "Asset Picker", false);
                var config = assetPicker.shortcutConfig.keyConfigs.FirstOrDefault(c => c.key == key);
                if (config == null) return;
                assetPicker.tagFilter = config.tags.ToArray();
                assetPicker.Repaint();
            }
            
            [Shortcut("AssetPicker/Key4", KeyCode.Alpha4)]
            static void Alpha4(ShortcutArguments args) => ApplyTagsForShortcut(KeyCode.Alpha4);

            [Shortcut("AssetPicker/Key5", KeyCode.Alpha5)]
            static void Alpha5(ShortcutArguments args) => ApplyTagsForShortcut(KeyCode.Alpha5);

            [Shortcut("AssetPicker/Key6", KeyCode.Alpha6)]
            static void Alpha6(ShortcutArguments args) => ApplyTagsForShortcut(KeyCode.Alpha6);

            [Shortcut("AssetPicker/Key7", KeyCode.Alpha7)]
            static void Alpha7(ShortcutArguments args) => ApplyTagsForShortcut(KeyCode.Alpha7);

            [Shortcut("AssetPicker/Key8", KeyCode.Alpha8)]
            static void Alpha8(ShortcutArguments args) => ApplyTagsForShortcut(KeyCode.Alpha8);

            [Shortcut("AssetPicker/Key9", KeyCode.Alpha9)]
            static void Alpha9(ShortcutArguments args) => ApplyTagsForShortcut(KeyCode.Alpha9);

            [Shortcut("AssetPicker/Key0", KeyCode.Alpha0)]
            static void Alpha0(ShortcutArguments args) => ApplyTagsForShortcut(KeyCode.Alpha0);
        }
    }
}