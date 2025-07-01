using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Awaken.TG.Editor.SimpleTools;
using Awaken.TG.Editor.Utility.StoryGraphs.Converter;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Specs;
using Awaken.TG.Main.Utility.Video.Subtitles;
using Awaken.TG.MVC;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.PropertyVariants;
using UnityEngine.Localization.PropertyVariants.TrackedProperties;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;
using XNode;
using static Awaken.TG.Editor.Localizations.LocalizationTools;
using static Awaken.TG.Editor.Utility.StoryGraphs.Converter.GraphConverterUtils;

namespace Awaken.TG.Editor.Localizations {
    public static class LocalizationCleanupTools {
        static readonly string[] PathBlacklist = {
            "IGNORE_LOCALIZATION",
            "Debug",
            "_OLD",
            "Obsolete",
            "OBSOLETE",
            "Stories_Design",
            "WIP"
        };
        
        static readonly Regex GuidRegex = new("[a-z0-9]{32}", RegexOptions.Singleline | RegexOptions.Compiled);

        public static string ExtractGuid(StringTableEntry entry) {
            if (entry?.Key == null) {
                return null;
            }
            var match = GuidRegex.Match(entry.Key);
            return match.Success ? match.Value : null;
        }
        
        static void RemoveNonExistingFromLocalizations(StringTable table) {
            List<StringTableEntry> tableEntries = new(table.Values);
            foreach (var data in tableEntries.Where(e => e.Key != null)) {
                string guid = ExtractGuid(data);
                if (guid != null && string.IsNullOrWhiteSpace(AssetDatabase.GUIDToAssetPath(guid))) {
                    LocalizationUtils.RemoveTableEntry(data.Key, table);
                }
            }

            UpdateAllSources();
        }

        [MenuItem("Window/Asset Management/Remove Old Localizations SAFE")]
        static void RemoveOldLocalizationsSafe() {
            RemoveNonExistingFromLocalizations(PrefabSource);
            RemoveNonExistingFromLocalizations(StorySource);
            RemoveNonExistingFromLocalizations(OverridesSource);
            RemoveEntriesMissingInSharedData();
            UpdateAllSources();
        }

        static void RemoveEntriesMissingInSharedData() {
            foreach (var source in LocalizationHelper.StringTables) {
                var sourceAsset = LocalizationEditorSettings.GetStringTableCollection(source);
                foreach (var locale in Locales) {
                    var table = (StringTable)sourceAsset.GetTable(locale.Identifier);
                    table.CheckForMissingSharedTableDataEntries(MissingEntryAction.RemoveEntriesFromTable);
                    EditorUtility.SetDirty(table);
                    EditorUtility.SetDirty(table.SharedData);
                }
            }
        }

        [MenuItem("Window/Asset Management/Remove Old Localizations")]
        static void RemoveOldLocalizations() {
            var watch = new Stopwatch();
            watch.Start();

            using var bar = ProgressBar.Create("Remove Old Localizations");
            bar.Display(0);

            var existingIDs = new HashSet<string>(ExistingLocalization(bar.TakePart(0.88F, "Collecting localizations")));

            RemoveOldFrom(PrefabCollection, existingIDs, bar.TakePart(0.04F, "Remove from Prefabs"));
            RemoveOldFrom(StoryCollection, existingIDs, bar.TakePart(0.04F, "Remove from Story"));
            RemoveOldFrom(OverridesCollection, existingIDs, bar.TakePart(0.04F, "Remove from Overrides"));

            SetDirty(PrefabSource);
            SetDirty(StorySource);
            SetDirty(OverridesSource);
            SetDirty(OldLocalizationsSource);
            
            watch.Stop();
            Log.Important?.Info($"Completed in: {watch.Elapsed}");
        }

        static void RemoveOldFrom(StringTableCollection collection, ICollection<string> existing, ProgressBar bar) {
            var entries = collection.SharedData.Entries.ToArray();
            for (int i = 0; i < entries.Length; i++) {
                bar.Display((float)i / entries.Length);
                if (!existing.Contains(entries[i].Key)) {
                    LocalizationUtils.MoveEntry(entries[i], collection, OldLocalizationsCollection);
                }
            }
        }

        static IEnumerable<string> ExistingLocalization(ProgressBar bar) {
            using (var part = bar.TakePart(0.01F, "Collecting from xNode")) {
                var graphs = AllGraphs().ToList();
                for (int i = 0; i < graphs.Count; i++) {
                    part.Display((float)i / graphs.Count);

                    foreach (var id in GetLocalizationIDs(graphs[i])) {
                        yield return id;
                    }

                    foreach (var node in GraphConverterUtils.ExtractNodes<Node>(graphs[i]).Select(g => g.node)) {
                        foreach (var id in GetLocalizationIDs(node)) {
                            yield return id;
                        }

                        if (node is StoryNode storyNode) {
                            foreach (var id in storyNode.elements.SelectMany(GetLocalizationIDs)) {
                                yield return id;
                            }
                        }
                    }
                }
            }

            using (var part = bar.TakePart(0.1F, "Collecting from Scriptable Templates")) {
                var guids = AssetDatabase.FindAssets("t:ScriptableObject");
                for (int i = 0; i < guids.Length; i++) {
                    if (IsGuidPathBlacklisted(guids[i])) {
                        continue;
                    }
                    part.Display((float)i / guids.Length);
                    var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guids[i]));
                    if (so is StoryGraph) {
                        continue;
                    }

                    if (so is ITemplate or ILocalizedSO) {
                        foreach (var id in GetLocalizationIDs(so)) {
                            yield return id;
                        }
                    }
                }
            }

            using (var part = bar.TakePart(0.01F, "Collecting from Subtitles")) {
                part.Display(0.5f);
                var keys = ExistingLocalizationFromSubtitles().ToList();
                Log.Important?.Info("Keys from Subtitles: " + keys.Count);
                foreach (var key in keys) {
                    yield return key;
                }
            }

            using (var part = bar.TakePart(0.2F, "Collecting from Prefabs")) {
                var keys = ExistingLocalizationFromPrefabs(part).ToList();
                Log.Important?.Info("Keys from Prefabs: " + keys.Count);
                foreach (var key in keys) {
                    yield return key;
                }
            }

            using (var part = bar.TakeRest("Collecting from Scenes")) {
                var keys = ExistingLocalizationFromScenes(part).ToList();
                Log.Important?.Info("Keys from Scenes: " + keys.Count);
                foreach (var key in keys) {
                    yield return key;
                }
            }
        }

        static IEnumerable<string> ExistingLocalizationFromSubtitles() {
            string[] subtitles = Directory.GetFiles("Assets/Videos/Subtitles", "*.asset", SearchOption.AllDirectories);
            for (int i = 0; i < subtitles.Length; i++) {
                SubtitlesData data;
                try {
                    data = AssetDatabase.LoadAssetAtPath<SubtitlesData>(subtitles[i]);
                } catch {
                    continue;
                }

                if (data != null) {
                    foreach (var id in GetLocalizationIDs(data)) {
                        yield return id;
                    }
                }
            }
        }

        static IEnumerable<string> ExistingLocalizationFromPrefabs(ProgressBar bar) {
            var guids = AssetDatabase.FindAssets("t:Prefab");
            for (int i = 0; i < guids.Length; i++) {
                bar.Display((float)i / guids.Length);
                if (IsGuidPathBlacklisted(guids[i])) {
                    continue;
                }
                GameObject go;
                try {
                    go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[i]));
                } catch {
                    continue;
                }
                
                if (HasComponentsWithLocalizations(go, true)) {
                    foreach (var component in go.GetComponentsInChildren<Component>(true)
                                 .Where(CanHaveLocalization)) {
                        if (component is GameObjectLocalizer localizer) {
                            foreach (var property in localizer.TrackedObjects.SelectMany(o =>
                                         o.TrackedProperties)) {
                                if (property is LocalizedStringProperty prop) {
                                    yield return prop.LocalizedString.TableEntryReference.Key;
                                    foreach (var key in prop.LocalizedString.Keys) {
                                        yield return key;
                                    }
                                }
                            }
                        } else {
                            foreach (var id in GetLocalizationIDs(component)) {
                                yield return id;
                            }
                        }
                    }
                }
            }
        }

        static IEnumerable<string> ExistingLocalizationFromScenes(ProgressBar bar) {
            string[] scenesInFolder = Directory.GetFiles("Assets/Scenes", "*.unity", SearchOption.AllDirectories);

            for (int i = 0; i < scenesInFolder.Length; i++) {
                string scenePath = scenesInFolder[i];
                bar.Display((float)i / scenesInFolder.Length, $"Collecting from Scene: {scenePath}");
                if (!scenePath.Contains("Dev_Scenes") && !scenePath.Contains("OLD_Scenes")) {
                    Scene loadedScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    IEnumerable<string> keys = SearchSceneForLocalizations(loadedScene);
                    foreach (var key in keys) {
                        yield return key;
                    }
                }
            }
        }

        static IEnumerable<string> SearchSceneForLocalizations(Scene scene) {
            List<GameObject> rootObjects = new List<GameObject>();
            scene.GetRootGameObjects(rootObjects);

            foreach (GameObject go in rootObjects) {
                IEnumerable<string> keys = CheckChildrenForLocalizations(go.transform, scene.name);
                foreach (var key in keys) {
                    yield return key;
                }
            }
        }

        static IEnumerable<string> CheckChildrenForLocalizations(Transform transform, string sceneName) {
            GameObject go = transform.gameObject;
            if (HasComponentsWithLocalizations(go)) {
                foreach (var component in go.GetComponents<Component>().Where(CanHaveLocalization)) {
                    if (component is GameObjectLocalizer localizer) {
                        foreach (var property in localizer.TrackedObjects.SelectMany(o =>
                                     o.TrackedProperties)) {
                            if (property is LocalizedStringProperty prop) {
                                yield return prop.LocalizedString.TableEntryReference.Key;
                                foreach (var key in prop.LocalizedString.Keys) {
                                    yield return key;
                                }
                            }
                        }
                    } else {
                        foreach (var id in GetLocalizationIDs(component)) {
                            yield return id;
                        }
                    }
                }
            }

            for (int i = 0; i < transform.childCount; i++) {
                IEnumerable<string> keys = CheckChildrenForLocalizations(transform.GetChild(i), sceneName);
                foreach (var key in keys) {
                    yield return key;
                }
            }
        }

        static bool HasComponentsWithLocalizations(GameObject go, bool searchChildren = false) {
            if (searchChildren) {
                return go.GetComponentInChildren<ISpec>() != null || go.GetComponentInChildren<IView>() != null
                                                                  || go.GetComponentInChildren<ITemplate>() != null
                                                                  || go.GetComponentInChildren<ActorSpec>() != null
                                                                  || go.GetComponentInChildren<IAttachmentGroup>() != null
                                                                  || go.GetComponentInChildren<IService>() != null;
            } else {
                return go.GetComponent<ISpec>() != null || go.GetComponent<IView>() != null
                                                        || go.GetComponent<ITemplate>() != null
                                                        || go.GetComponent<ActorSpec>() != null
                                                        || go.GetComponent<IAttachmentGroup>() != null
                                                        || go.GetComponent<IService>() != null;
            }
        }

        static bool IsGuidPathBlacklisted(string guid) {
            return PathBlacklist.Any(AssetDatabase.GUIDToAssetPath(guid).Contains);
        }
    }
}