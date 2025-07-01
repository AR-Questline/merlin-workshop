using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Assets.Grouping {
    [Serializable]
    public class AssetGroup : ScriptableObject {
        public AssetGroupType type = AssetGroupType.Mixed;
        public List<string> elements = new List<string>();

        ARAddressableManager _manager;

        public ARAddressableManager Manager {
            get {
                if (_manager == null) {
                    _manager =
                        AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(this)) as ARAddressableManager;
                }
                return _manager;
            }
        }

        public AddressableAssetGroup GetGroup(bool create) {
            var group = Manager.Settings.FindGroup(g => g.Name == name);
            if (group == null && create) {
                group = Manager.Settings.CreateGroup(name, false, false, true,
                    Manager.Settings.DefaultGroup.Schemas);
            }
            return group;
        }

        public void Add(AssetEntry entry) {
            if (!Manager.data.ContainsKey(entry.guid)) return;
            if (elements.Contains(entry.guid)) return;
            elements.Add(entry.guid);
            entry.assetGroup = this;
        }

        public void Remove(AssetEntry entry) {
            elements.Remove(entry.guid);
            if (entry.assetGroup == this) {
                entry.assetGroup = null;
            }
        }

        // Splits group into smaller groups, each containing elements from one splitGroup.
        public void Split(HashSet<string>[] splitGroups) {
            if (splitGroups.Length < 2) return;
            elements = splitGroups[0].ToList();
            Refresh();
            for (int i = 1; i < splitGroups.Length; i++) {
                var group = AssetGroup.CreateInstance<AssetGroup>();
                group.elements = splitGroups[i].ToList();
                
                group.type = type;
                Manager.AddGroup(group);
                group.Refresh();
            }
        }

        public void Merge(AssetGroup other) {
            foreach (string guid in other.elements) {
                Add(Manager.data[guid]);
            }
            other.elements.Clear();
            Manager.RemoveGroup(other);
        }

        public void Refresh() {
            foreach (string guid in elements) {
                Manager.data[guid].assetGroup = this;
            }
        }

        [Button]
        public void SetGroupTypeFromFirstAsset() {
            string assetPath = AssetDatabase.GUIDToAssetPath(elements[0]);
            var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            this.type = GetGroupType(assetType, assetPath);
            if (type == AssetGroupType.Mixed) {
                Log.Important?.Warning($"Wrong type: {assetType?.Name} for: {assetPath} guid: {elements[0]}");
            }
        }

        public static AssetGroupType GetGroupType(Type type, string path) {
            if (type == typeof(GameObject)) {
                if (path.EndsWith(".fbx") || path.EndsWith(".obj")) {
                    return AssetGroupType.Meshes;
                } else if (path.Contains("BeardSet_")) {
                    return AssetGroupType.Beards;
                } else {
                    return AssetGroupType.Prefabs;
                }
            } else if (type == typeof(Texture2D)) {
                return AssetGroupType.Textures;
            } else if (type == typeof(Material)) {
                return AssetGroupType.Materials;
            } else if (type == typeof(Mesh)) {
                if (path.Contains("BeardSet_")) {
                    return AssetGroupType.Beards;
                } else {
                    return AssetGroupType.Meshes;
                }
            } else if (type == typeof(Shader)) {
                return AssetGroupType.Shaders;
            } else if (type == typeof(SceneAsset)) {
                return AssetGroupType.Scenes;
            } else {
                return AssetGroupType.Mixed;
            }
        }

        [Button]
        void PrintUsagesCount() {
            var data = Manager.data;
            var sorted = elements.OrderBy(guid => -data[guid].usages.Length);
            var msg = $"Group {this.name} usages count\n";
            foreach (string guid in sorted) {
                msg += $"{data[guid].usages.Length}: {AssetDatabase.GUIDToAssetPath(guid)}\n";
            }

            Log.Important?.Info(msg, this);
        }

        public enum AssetGroupType {
            Mixed,
            Prefabs,
            Materials,
            Textures,
            Meshes,
            Shaders,
            Scenes,
            Beards,
        }

#if UNITY_EDITOR
        // === Editor
        [ShowInInspector, BoxGroup("Debug", order: 2)]
        List<Object> _objects = new List<Object>();

        [Button, BoxGroup("Debug", order: 2)]
        void RefreshObjects() {
            _objects.Clear();
            foreach (string guid in elements) {
                _objects.Add(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid)));
            }
        }
#endif
    }
}