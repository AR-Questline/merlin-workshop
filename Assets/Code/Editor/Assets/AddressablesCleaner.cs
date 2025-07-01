using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Debugging.GUIDSearching;
using Awaken.TG.Editor.Graphics.Statues;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Heroes.CharacterCreators.Data;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.UIElements;
using ProgressBar = Awaken.TG.Editor.SimpleTools.ProgressBar;

namespace Awaken.TG.Editor.Assets {
    public class AddressablesCleaner : OdinEditorWindow {
        Cleaner _cleaner;
        
        [ShowInInspector] List<Object> UnusedAssetsSimple => _cleaner.unusedAssetsSimple;
        [ShowInInspector] List<Object> UnusedAssetsRecursive => _cleaner.unusedAssetsRecursive;
        
        [MenuItem("TG/Addressables/Addressable Cleaner", priority = 2500)]
        static void OpenWindow() {
            var window = GetWindow<AddressablesCleaner>();
            window.Show();
            window._cleaner = new();
        }

        [Button]
        void FindUnusedAddressables() {
            _cleaner.FindUnusedAddressables();
        }

        [Button]
        void Delete() {
            _cleaner.Delete();
        }

        public class Cleaner {
            public List<Object> unusedAssetsSimple = new();
            public List<Object> unusedAssetsRecursive = new();

            [MenuItem("TG/Build/Baking/Remove Unused Addressables (based on GUID Cache)")]
            public static void PerformBuildCleaning() {
                var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
                foreach (var group in settings.groups) {
                    foreach (var entry in group.entries.ToArray()) {
                        if (GUIDCache.Instance.IsUnused(entry.guid) && !entry.labels.Contains("Preserve")) {
                            settings.RemoveAssetEntry(entry.guid);
                        }
                    }
                }
                
                AssetDatabase.SaveAssets();
            }

            public void FindUnusedAddressables() {
                unusedAssetsSimple.Clear();
                unusedAssetsRecursive.Clear();

                var addressableSettings = AddressableAssetSettingsDefaultObject.GetSettings(true);
                if (GUIDCache.Instance == null) {
                    GUIDCache.Load();
                }

                int countAll = 0;
                int countSimple = 0;
                int countRecursive = 0;

                List<(string path, Object obj)> usages = new(100);
                HashSet<Object> visited = new(100);

                using var progressBar = ProgressBar.Create("Analyzing Addressables");
                int entriesCount = addressableSettings.groups.Sum(static g => g.entries.Count);
                bool cancelled = false;

                foreach (AddressableAssetGroup group in addressableSettings.groups) {
                    foreach (var entry in group.entries) {
                        if (cancelled) {
                            break;
                        }

                        countAll++;
                        cancelled = progressBar.DisplayCancellable((float)countAll / entriesCount, $"Group: {group.Name}");

                        usages.Clear();
                        visited.Clear();

                        var mainAsset = entry.MainAsset;
                        var mainAssetPath = entry.AssetPath;

                        if (IsAssetUsedDirectlyInBuild(mainAssetPath, mainAsset)) {
                            continue;
                        }

                        if (GUIDCache.Instance.GetDependent(mainAsset, true).Any() == false) {
                            unusedAssetsSimple.Add(mainAsset);
                            countSimple++;
                        } else {
                            usages.Add((mainAssetPath, mainAsset));
                            visited.Add(mainAsset);
                            bool foundRealUsage = false;
                            
                            while (!foundRealUsage && usages.Any()) {
                                var nextAsset = usages[0];

                                if (IsAssetUsedDirectlyInBuild(nextAsset.path, nextAsset.obj)) {
                                    foundRealUsage = true;
                                    continue;
                                }

                                usages.RemoveAt(0);

                                if (nextAsset.obj is SceneAsset) {
                                    continue;
                                }
                                
                                foreach (var path in GUIDCache.Instance.GetDependent(nextAsset.obj, true)) {
                                    var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                                    if (visited.Add(asset)) {
                                        usages.Add((path, asset));
                                    }
                                }
                            }

                            if (!foundRealUsage) {
                                unusedAssetsRecursive.Add(mainAsset);
                                countRecursive++;
                            }
                        }
                    }
                }

                Log.Important?.Error(
                    $"Found {countSimple} simple and {countRecursive} recursive unused addressable assets. Total count of addressable entries: {countAll}");
            }

            public void Delete() {
                var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
                foreach (var obj in unusedAssetsSimple) {
                    Remove(obj);
                }

                foreach (var obj in unusedAssetsRecursive) {
                    Remove(obj);
                }

                void Remove(Object obj) {
                    string path = AssetDatabase.GetAssetPath(obj);
                    string guid = AssetDatabase.AssetPathToGUID(path);
                    settings.RemoveAssetEntry(guid);
                }
            }

            static bool IsAssetUsedDirectlyInBuild(string path, Object obj) {
                if (obj is MonoScript or Locale or LocalizationTable or SharedTableData or CharacterCreatorTemplate
                    or TalentTreeTemplate or VisualTreeAsset or StyleSheet) {
                    return true;
                }

                if (obj is SceneAsset) {
                    string pathForAddressables = path.Replace("\\", "/");
                    bool includedInAddressables = AddressableHelper.FindGroup(SceneService.ScenesGroup).entries.Any(e => e.AssetPath == pathForAddressables);
                    bool isBuiltIn = path.Contains("ApplicationScene");
                    return includedInAddressables || isBuiltIn;
                }

                if (obj is GameObject go) {
                    if (go.HasComponent<View>() || go.HasComponent<ItemTemplate>() || go.HasComponent<FactionTemplate>()) {
                        return true;
                    }
                }

                if (path.Contains(StatueEditor.StatuesMeshesFolder)) {
                    // Statues are not referenced by guid :(
                    return true;
                }

                if (obj is Shader) {
                    // It's too hard to check them correctly
                    return true;
                }

                return false;
            }
        }
    }
}
