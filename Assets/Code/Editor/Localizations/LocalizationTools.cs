using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.Kandra;
using Awaken.Kandra.VFXs;
using Awaken.TG.Fights.NPCs;
using Awaken.TG.Graphics;
using Awaken.TG.Graphics.DayNightSystem;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Graphics.VFX.Binders;
using Awaken.TG.Main.Animations.IK;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Elevator;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Steps;
using Awaken.Utility;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Awaken.Utility.GameObjects;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.Google;
using UnityEditor.Localization.Reporting;
using UnityEditor.Localization.UI;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.PropertyVariants;
using UnityEngine.Localization.PropertyVariants.TrackedProperties;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.VFX.Utility;
using XNode;
using static Awaken.TG.Editor.Localizations.LocalizationUtils;
using static Awaken.TG.Editor.Utility.StoryGraphs.Converter.GraphConverterUtils;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Localizations {
    public static class LocalizationTools {
        public static readonly StringTable PrefabSource = LocalizationSettings.StringDatabase.GetTable("Prefabs", LocalizationSettings.ProjectLocale);
        public static readonly StringTable OverridesSource = LocalizationSettings.StringDatabase.GetTable("Overrides", LocalizationSettings.ProjectLocale);
        public static readonly StringTable LocTermsSource = LocalizationSettings.StringDatabase.GetTable("LocTerms", LocalizationSettings.ProjectLocale);
        public static readonly StringTable StorySource = LocalizationSettings.StringDatabase.GetTable("Story", LocalizationSettings.ProjectLocale);
        public static readonly StringTable KeyBindingsSource = LocalizationSettings.StringDatabase.GetTable("KeyBindings", LocalizationSettings.ProjectLocale);
        public static readonly StringTable OldLocalizationsSource = LocalizationSettings.StringDatabase.GetTable("OldLocalization", LocalizationSettings.ProjectLocale);
        
        public static readonly StringTable[] Sources = { PrefabSource, OverridesSource, LocTermsSource, StorySource, KeyBindingsSource };
        
        public static readonly StringTableCollection PrefabCollection = LocalizationEditorSettings.GetStringTableCollection(PrefabSource.TableCollectionName);
        public static readonly StringTableCollection OverridesCollection = LocalizationEditorSettings.GetStringTableCollection(OverridesSource.TableCollectionName);
        public static readonly StringTableCollection LocTermsCollection = LocalizationEditorSettings.GetStringTableCollection(LocTermsSource.TableCollectionName);
        public static readonly StringTableCollection StoryCollection = LocalizationEditorSettings.GetStringTableCollection(StorySource.TableCollectionName);
        public static readonly StringTableCollection KeyBindingsCollection = LocalizationEditorSettings.GetStringTableCollection(KeyBindingsSource.TableCollectionName);
        public static readonly StringTableCollection OldLocalizationsCollection = LocalizationEditorSettings.GetStringTableCollection(OldLocalizationsSource.TableCollectionName);
        
        public static readonly StringTableCollection[] Collections = { PrefabCollection, OverridesCollection, LocTermsCollection, StoryCollection, KeyBindingsCollection };
        
        public static Locale[] Locales => LocalizationSettings.AvailableLocales.Locales.ToArray();

        [MenuItem("Window/Asset Management/Load Scene Names", priority = 1070)]
        public static void GenerateAndCleanSceneNames() {
            var sceneGUIDs = AssetDatabase.FindAssets("t:Scene");
            List<string> allScenes = new();
            List<string> selectedSceneKeys = new();
            
            foreach (var scene in sceneGUIDs) {
                allScenes.Add(AssetDatabase.GUIDToAssetPath(scene));
            }

            var selectedScenes = allScenes.Where(sceneName => !sceneName.Contains("Subscenes") 
                                                                      && !sceneName.Contains("Dev_Scenes") 
                                                                      && !sceneName.Contains("Plugins")
                                                                      && !sceneName.Contains("3DAssets")
                                                                      && !sceneName.Contains("Shaders")
                                                                      && !sceneName.Contains("Vendor")
                                                                      && !sceneName.Contains("Packages"));
            
            foreach (var scene in selectedScenes) {
                selectedSceneKeys.Add($"{LocTerms.ScenePrefix}{Path.GetFileNameWithoutExtension(scene)}");
            }
            
            LocTermsCollection.SharedData.Entries.Select(entry => entry.Key).ToArray().ForEach(key => {
                if (!selectedSceneKeys.Contains(key) && key.StartsWith(LocTerms.ScenePrefix)) {
                    LocTermsCollection.RemoveEntry(key);
                }
            });

            UpdatePositionsInTable(selectedSceneKeys);
        }
        
        [MenuItem("Window/Asset Management/Load LocTerms", priority = 1070)]
        public static void GenerateAndCleanLocTermsTables() {
            Type type = typeof(LocTerms);
            FieldInfo[] staticReadonlyStrings = type.GetFields(BindingFlags.Static | BindingFlags.Public);
            var readonlyStrings = staticReadonlyStrings
                .Where(field => field.Name != nameof(LocTerms.ScenePrefix))
                .Select(s => s.GetValue(null) as string)
                .WhereNotNull()
                .ToArray();

            // Remove removed LocTerms from table
            LocTermsCollection.SharedData.Entries.Select(x => x.Key).ToArray().ForEach(key => {
                if (!readonlyStrings.Contains(key) && !key.StartsWith(LocTerms.ScenePrefix)) {
                    LocTermsCollection.RemoveEntry(key);
                }
            });
            
            UpdatePositionsInTable(readonlyStrings);
            LocalizationTablesWindow.ShowWindow();
        }

        static void UpdatePositionsInTable(IEnumerable<string> termsCollection) {
            // Add missing LocTerms to table
            foreach (string term in termsCollection) {
                int indexOf = LocTermsSource.IndexOf(entry => entry.Value.Key == term);
                if (indexOf < 0) {
                    LocTermsSource.AddEntry(term, term);
                }

                var foundInOverrides = OverridesSource.Values.FirstOrDefault(entry => entry.Key == term);
                if (foundInOverrides != null) {
                    MoveToDifferentCollection(foundInOverrides, LocTermsCollection);
                }
            }

            SortTable(LocTermsCollection);
            DeDuplicateTable(LocTermsCollection);
            SetDirty(LocTermsSource, true);
            SetDirty(OverridesSource, true);
        }

        static void DeDuplicateTable(StringTableCollection collection) {
            var possibleDuplicates = collection.SharedData.Entries.GroupBy(e => e.Key);
            bool needsUserAction = false;
            foreach (IGrouping<string, SharedTableData.SharedTableEntry> sharedTableEntries in possibleDuplicates) {
                var list = sharedTableEntries.ToList();
                int count = list.Count;
                if (count > 1) {
                    var values = AllExistingValuesAtID(collection, list[0].Id).ToList();
                    for (int i = 1; i < list.Count; i++) {
                        if (values.SequenceEqual(AllExistingValuesAtID(collection, list[i].Id))) {
                            collection.RemoveEntry(list[i].Id);
                            Log.Important?.Info("Removed identical duplicate of '" + sharedTableEntries.Key + "'");
                            count--;
                        }
                    }

                    if (count <= 1) {
                        Log.Important?.Info(" >> Removed all duplicates of Key: '" + sharedTableEntries.Key + "'");
                    } else {
                        Log.Important?.Error("ACTION REQUIRED:".ColoredText(Color.red) + " Manual Deduplication \n "
                            + "Key: '" + sharedTableEntries.Key.ColoredText(Color.white)
                            + "' has '" + count.ToString().ColoredText(Color.white) + "' instances, this is not allowed!"
                            + " You need to find the key in the '" + collection.name.ColoredText(Color.white) + "' collection and remove the duplicate!");
                        needsUserAction = true;
                    }
                }
            }

            if (needsUserAction) {
                PopupWindow.DisplayDialog("User Action Required!",
                    "There are duplicate keys in the '" + collection.name.ColoredText(Color.white) + "' collection that cannot be removed automatically, \nplease resolve the issue! Check console for more info",
                    "Confirm");
            }
        }

        static IEnumerable<string> AllExistingValuesAtID(StringTableCollection collection, long id) =>
            collection.StringTables.Where(st => st.ContainsKey(id)).Select(st => st.GetEntry(id).Value);

        static void MoveToDifferentCollection(TableEntry entryToMove, StringTableCollection targetCollection) {
            SharedTableData.SharedTableEntry sharedEntryToMove = entryToMove.SharedEntry; //Old entry to be removed
            SharedTableData.SharedTableEntry sharedLocTermEntry = targetCollection.SharedData.GetEntry(sharedEntryToMove.Key); //New entry to be filled

            var toMoveSourceCollection = LocalizationEditorSettings.GetStringTableCollection(entryToMove.Table.TableCollectionName).StringTables;

            foreach (var st in toMoveSourceCollection) {
                // For each translated field of old entry copy to new entry
                StringTableEntry stringTableEntry = st.GetEntry(entryToMove.KeyId);
                if (stringTableEntry == null) continue;
                StringTable stringTable = targetCollection.StringTables
                    .First(ltc =>
                        ltc.LocaleIdentifier ==
                        st.LocaleIdentifier); // Find the same localization language table in new entry collection
                stringTable.AddEntry(sharedLocTermEntry.Id, stringTableEntry.Value);
            }

            PrefabCollection.RemoveEntry(entryToMove.KeyId);
        }


        [MenuItem("Window/Asset Management/Cleanup All Collections", priority = 1070)]
        static void SortAll() {
            foreach (var collection in Collections) {
                SortTable(collection);
                DeDuplicateTable(collection);
                SetDirty(collection);
            }
        }

        [MenuItem("Window/Asset Management/Sort Specific/LocTerms", priority = 1070)]
        static void SortLocTermTable() {
            SortTable(LocTermsCollection);
            DeDuplicateTable(LocTermsCollection);
            SetDirty(LocTermsCollection);
        }

        [MenuItem("Window/Asset Management/Sort Specific/Prefab", priority = 1070)]
        static void SortPrefabTable() {
            SortTable(PrefabCollection);
            DeDuplicateTable(PrefabCollection);
            SetDirty(PrefabCollection);
        }

        [MenuItem("Window/Asset Management/Sort Specific/Overrides", priority = 1070)]
        static void SortOverridesTable() {
            SortTable(OverridesCollection);
            DeDuplicateTable(OverridesCollection);
            SetDirty(OverridesCollection);
        }

        [MenuItem("Window/Asset Management/Sort Specific/KeyBindings", priority = 1070)]
        static void SortKeyBindingsTable() {
            SortTable(KeyBindingsCollection);
            DeDuplicateTable(KeyBindingsCollection);
            SetDirty(KeyBindingsCollection);
        }

        static void SortTable(StringTableCollection tableCollection) {
            var saved = tableCollection.SharedData.Entries;
            tableCollection.SharedData.Entries = saved.OrderBy(x => x.Key).ToList();
        }

        [MenuItem("CONTEXT/StringTableCollection/Pull All")]
        static void ImportStringTable(MenuCommand command) {
            var tables = command.context as StringTableCollection;
            if (tables == null) return;
            var ext = tables.Extensions.First(x => x is GoogleSheetsExtension) as GoogleSheetsExtension;
            if (ext == null) throw new ArgumentNullException(nameof(ext));

            var google = GetGoogleSheets(ext);
            StringTableCollection stringTableCollection = ext.TargetCollection as StringTableCollection;
            google.PullIntoStringTableCollection(ext.SheetId, stringTableCollection, ext.Columns,
                ext.RemoveMissingPulledKeys, new ProgressReporter(), true);


            GoogleSheets GetGoogleSheets(GoogleSheetsExtension extension) {
                var google = new GoogleSheets(extension.SheetsServiceProvider) {
                    SpreadSheetId = extension.SpreadsheetId
                };
                return google;
            }
        }

        public static void SetDirty(StringTable tableToDirty, bool saveChanges = false) {
            EditorUtility.SetDirty(tableToDirty);
            StringTableCollection parentCollection = LocalizationEditorSettings.GetStringTableCollection(tableToDirty.TableCollectionName);
            SetDirty(parentCollection);
            if (saveChanges) {
                AssetDatabase.SaveAssetIfDirty(tableToDirty);
            }
        }

        public static void SetDirty(StringTableCollection collectionToDirty, bool saveChanges = false) {
            EditorUtility.SetDirty(collectionToDirty);
            EditorUtility.SetDirty(collectionToDirty.SharedData);
            if (saveChanges) {
                AssetDatabase.SaveAssetIfDirty(collectionToDirty);
                AssetDatabase.SaveAssetIfDirty(collectionToDirty.SharedData);
            }
        }

        public static List<ActorTermMap> CollectActorsForTerms(string[] guids, StringTable stringTable) {
            var correctTerms = new List<ActorTermMap>();

            // Debug translations slows down this method significantly, so we disable it
            var wasDebugTranslations = SafeEditorPrefs.GetBool("debugTranslations");
            SafeEditorPrefs.SetInt("debugTranslations", 0);

            if (stringTable == StorySource) {
                ActorsRegister actorsRegister = AssetDatabase.LoadAssetAtPath<GameObject>(ActorsRegister.Path)?.GetComponent<ActorsRegister>();
                var nodes = AllNodes<StoryNode>().Select(g => g.node).ToList();
                foreach (var node in nodes) {
                    var locFields = GetLocalizedProperties(node, false);
                    foreach (var locField in locFields) {
                        if (locField?.LocProperty?.ID == null) {
                            continue;
                        }

                        if (guids.Any(guid => guid != null && locField.LocProperty.ID.Contains(guid))) {
                            StringTableEntry tableEntry = stringTable.GetEntry(locField.LocProperty.ID);
                            if (tableEntry == null) {
                                LocalizationUtils.RemoveTableEntry(locField.LocProperty.ID, stringTable);
                            } else {
                                correctTerms.Add(new ActorTermMap(string.Empty, tableEntry.Key));
                            }
                        }
                    }

                    foreach (var element in node.elements) {
                        locFields = GetLocalizedProperties(element, false);
                        string actor = string.Empty;
                        if (actorsRegister != null) {
                            if (element is SEditorText text) {
                                // split the path
                                string actorPath = actorsRegister.Editor_GetPathFromGUID(text.actorRef.guid);
                                string[] splitPath =  actorPath.Split('/');
                                // find correct spec
                                ActorSpec spec = GameObjects.TryGrabChild<ActorSpec>(actorsRegister.gameObject, splitPath);
                                if (spec != null) {
                                    string specDisplayName = spec.displayName;
                                    actor = string.IsNullOrWhiteSpace(specDisplayName) ? spec.gameObject.name : specDisplayName;
                                }
                            } else if (element is SEditorChoice) {
                                actor = "Player";
                            }
                        }

                        foreach (var locField in locFields) {
                            if (locField?.LocProperty?.ID == null) {
                                continue;
                            }

                            if (guids.Any(guid => guid != null && locField.LocProperty.ID.Contains(guid))) {
                                StringTableEntry tableEntry = stringTable.GetEntry(locField.LocProperty.ID);
                                if (tableEntry == null) {
                                    LocalizationUtils.RemoveTableEntry(locField.LocProperty.ID, stringTable);
                                } else {
                                    correctTerms.Add(new ActorTermMap(actor, tableEntry.Key));
                                }
                            }
                        }
                    }
                }

                foreach (var graph in AllGraphs().Where(g => g is StoryGraph)) {
                    var locFields = GetLocalizedProperties(graph);
                    foreach (var locField in locFields.Where(l => l != null)) {
                        if (locField?.LocProperty?.ID == null) {
                            continue;
                        }

                        if (guids.Any(guid => locField.LocProperty.ID.Contains(guid))) {
                            StringTableEntry tableEntry = stringTable.GetEntry(locField.LocProperty.ID);
                            if (tableEntry == null) {
                                LocalizationUtils.RemoveTableEntry(locField.LocProperty.ID, stringTable);
                            } else {
                                correctTerms.Add(new ActorTermMap(string.Empty, tableEntry.Key));
                            }
                        }
                    }
                }

                List<string> termsToRemove = new();
                foreach (string guid in guids) {
                    termsToRemove.AddRange(stringTable.Values.Where(t => t.Key != null && t.Key.Contains(guid)).Select(t => t.Key).Except(correctTerms.Select(a => a.term)));
                }

                foreach (var term in termsToRemove) {
                    LocalizationUtils.RemoveTableEntry(term, stringTable);
                }
            }
            
            SafeEditorPrefs.SetInt("debugTranslations", wasDebugTranslations ? 1 : 0);

            return correctTerms;
        }

        public readonly struct ActorTermMap {
            public readonly string actor;
            public readonly string term;

            public ActorTermMap(string actor, string term) {
                this.actor = actor;
                this.term = term;
            }
        }
        
        static readonly Type[] ForbiddenTypesForLocalization = {
            typeof(RenderersMarkers),
            typeof(KandraRenderer),
            typeof(VFXBodyMarker),
            typeof(KandraTrisCullee),
            typeof(RepellerCullingDistanceMultiplier),
            typeof(WyrdnightSplineRepeller),
            typeof(WaterSurface),
            typeof(SalsaWithKandraBridge),
            typeof(VCFeetIK),
            typeof(VFXPropertyBinder),
            typeof(VFXBodyMarkerBinder),
            typeof(VFXKandraRendererBinder),
            typeof(LightWithOverride),
            typeof(DrakeAnimatedPropertiesOverrideController),
            typeof(DayNightSystem),
            typeof(TopDownDepthTexturesLoadingManager),
            typeof(VFXTransformBinder),
            typeof(VFXBinderBase),
            typeof(PrecipitationController),
            typeof(CustomPassVolume),
            typeof(HealingShrineAttachment),
            typeof(NpcWyrdConversionMarker),
            typeof(ElevatorChains),
            typeof(VHeroCombatSlots)
        }; 
        
        public static bool CanHaveLocalization(Component component) {
            if (component == null) {
                return false;
            }
            
            return !ForbiddenTypesForLocalization.Any(t => t.IsAssignableFrom(component.GetType()));
        }

        public static IEnumerable<string> GetLocalizationIDs(Object o) {
            IReadOnlyCollection<LocalizedField> properties;
            try {
                properties = GetLocalizedProperties(o);
            } catch (StackOverflowException) {
                Log.Important?.Error($"Stack Overflow when trying to get localized properties of {o}. Consider putting its type ({o.GetType()}) to {nameof(CanHaveLocalization)} check");
                yield break;
            }

            foreach (var localized in properties) {
                yield return localized.LocProperty.ID;
                if (!localized.LocProperty.IdOverride.IsNullOrWhitespace()) {
                    yield return localized.LocProperty.IdOverride;
                }
            }
        }

        [MenuItem("TG/Localization/Move from Prefabs to Overrides")]
        static void PrefabsToOverrides() {
            StringTable prefabsTable = LocalizationSettings.StringDatabase.GetTable(LocalizationHelper.DefaultTable, LocalizationSettings.ProjectLocale);
            StringTableEntry[] prefabKeys = prefabsTable.Values.ToArray();
            StringTable overridesTable = LocalizationSettings.StringDatabase.GetTable(LocalizationHelper.OverridesTable, LocalizationSettings.ProjectLocale);
            StringTableEntry[] overrideKeys = overridesTable.Values.ToArray();
            string[] duplicates = prefabKeys.Where(t => overrideKeys.Any(m => m.Key == t.Key)).Select(t => t.Key).ToArray();
            
            foreach (var o in OverridesCollection.Tables) {
                StringTable prefabs = (StringTable) PrefabCollection.GetTable(o.asset.LocaleIdentifier);
                StringTable overrides = (StringTable) o.asset;
                foreach (string duplicatedID in duplicates) {
                    if (duplicatedID.IsNullOrWhitespace()) {
                        continue;
                    }
                    if (prefabs[duplicatedID] == null) {
                        continue;
                    }
                    if (overrides[duplicatedID] != null && prefabs[duplicatedID].Value == overrides[duplicatedID].Value) {
                        continue;
                    }
                    LocalizationUtils.ChangeTextTranslation(duplicatedID, prefabs[duplicatedID].Value,
                        overrides, true);
                }
            }

            foreach (string duplicatedID in duplicates) {
                LocalizationUtils.RemoveTableEntry(duplicatedID, prefabsTable);
            }
        }

        [MenuItem("TG/Localization/Copy translated to non translated source duplicates")]
        static void CopyTranslatedToNotTranslatedSourceDuplicates() {
            CopyTranslatedToNotTranslatedSourceDuplicates(false);
        }
        
        [MenuItem("TG/Localization/Copy translated to non translated source duplicates without story")]
        static void CopyTranslatedToNotTranslatedSourceDuplicatesWithoutStory() {
            CopyTranslatedToNotTranslatedSourceDuplicates(true);
        }
        
        static void CopyTranslatedToNotTranslatedSourceDuplicates(bool skipStory) {
            Dictionary<string, List<StringTableEntry>> entriesBySourceValue = new();
            foreach (var tableId in LocalizationHelper.StringTables) {
                if (skipStory && tableId == "Story") continue;
                StringTable stringTable = LocalizationSettings.StringDatabase.GetTable(tableId, LocalizationSettings.ProjectLocale);
                foreach (var entry in stringTable.Values) {
                    if (string.IsNullOrEmpty(entry.Key) || string.IsNullOrEmpty(entry.Value)) {
                        continue;
                    }
                    entriesBySourceValue.TryAdd(entry.Value, new List<StringTableEntry>());
                    entriesBySourceValue[entry.Value].Add(entry);
                }
            }

            foreach (var pair in entriesBySourceValue) {
                foreach (var curEntry in pair.Value) {
                    foreach (var locale in LocalizationSettings.AvailableLocales.Locales) {
                        if (locale == LocalizationSettings.ProjectLocale) { continue; }
                        
                        var localizedTable = LocalizationSettings.StringDatabase.GetTable(curEntry.Table.TableCollectionName, locale);
                        var localizedCurEntry = localizedTable.GetEntry(curEntry.Key);
                        if (string.IsNullOrEmpty(localizedCurEntry?.LocalizedValue)) {
                            var bestExistingTranslation = FindBestExistingTranslation(pair.Value, locale);
                            if (!string.IsNullOrEmpty(bestExistingTranslation)) {
                                LocalizationUtils.ChangeTextTranslation(curEntry.Key, bestExistingTranslation, localizedTable, true);
                            }
                        }
                    }
                }
            }

            LocalizationTools.UpdateAllSources();
            return;
            
            static string FindBestExistingTranslation(List<StringTableEntry> entries, Locale locale) {
                foreach(var entry in entries) {
                    var locTermData = TryGetTermData(entry.Key);
                    var tableEntry = locTermData.EntryFor(locale, false);
                    if (tableEntry != null && !string.IsNullOrWhiteSpace(tableEntry.LocalizedValue)) {
                        return tableEntry.LocalizedValue;
                    }
                }
                return string.Empty;
            }
        }
        
        [MenuItem("TG/Localization/Mark all redacted terms as proofread")]
        static void MarkAllRedactedTermsAsProofread() {
            foreach (var tableId in LocalizationHelper.StringTables) {
                StringTable stringTable = LocalizationSettings.StringDatabase.GetTable(tableId, LocalizationSettings.ProjectLocale);
                foreach (var entry in stringTable.Values) {
                    if (string.IsNullOrEmpty(entry.Key)) {
                        continue;
                    }
                    var locTermData = TryGetTermData(entry.Key);
                    if (locTermData.IsEmpty) {
                        continue;
                    }
                    
                    foreach (var locale in Locales) {
                        var localizedEntry = locTermData.EntryFor(locale);
                        if (localizedEntry != null) {
                            var meta = localizedEntry.GetMetadata<TermStatusMeta>();
                            if (meta == null) {
                                continue;
                            }
                            
                            if (meta.TranslationHash == locTermData.englishEntry?.Value?.Trim().GetHashCode()) { // is translated and redacted?
                                meta.ProofreadHash = meta.TranslationHash;
                            }
                            var tableCollection = LocalizationEditorSettings.GetStringTableCollection(localizedEntry.Table.TableCollectionName);
                            EditorUtility.SetDirty(tableCollection);
                            EditorUtility.SetDirty(localizedEntry.Table);
                            EditorUtility.SetDirty(localizedEntry.Table.SharedData);
                        }
                    }
                }
                UpdateAllSources();
            }
        }
        
        public static LocTermData TryGetTermData(string id) {
            StringTableEntry tableEntry = GetTableEntryForLocale(id, LocalizationSettings.ProjectLocale);
            if (tableEntry != null) {
                return new LocTermData(id);
            } else {
                return new LocTermData();
            }
        }

        public static StringTableEntry GetTableEntryForLocale(string id, Locale locale) {
            if (id == null) return null;
            
            foreach (var tableId in LocalizationHelper.StringTables) {
                StringTable stringTable = LocalizationSettings.StringDatabase.GetTable(tableId, locale);
                StringTableEntry tableEntry = stringTable.GetEntry(id);
                if (tableEntry != null) {
                    return tableEntry;
                }
            }
            return null;
        }

        public static string TryGetTranslation(string id, Locale locale) {
            string translation = string.Empty;
            if (id == null) return translation;
            
            foreach (var tableId in LocalizationHelper.StringTables) {
                StringTable stringTable = LocalizationSettings.StringDatabase.GetTable(tableId, locale);
                var tableEntry = stringTable.GetEntry(id);
                if (tableEntry != null && !string.IsNullOrWhiteSpace(tableEntry.LocalizedValue)) {
                    return tableEntry.LocalizedValue;
                }
            }
            return translation;
        }

        public static void UpdateAllSources() {
            EditorUtility.SetDirty(PrefabSource);
            EditorUtility.SetDirty(StorySource);
            EditorUtility.SetDirty(OverridesSource);
            AssetDatabase.SaveAssets();
        }
        
        public static void RemoveAllStringTableEntriesFromNode(Node nodeToBeRemoved) {
            if (nodeToBeRemoved is StoryNode storyNode) {
                var locFields = GetLocalizedProperties(storyNode, false);
                foreach (var locField in locFields) {
                    if (locField?.LocProperty?.ID == null) {
                        continue;
                    }

                    LocalizationUtils.RemoveTableEntry(locField.LocProperty.ID, StorySource);
                }

                foreach (var element in storyNode.elements) {
                    locFields = GetLocalizedProperties(element, false);
                    foreach (var locField in locFields) {
                        if (locField?.LocProperty?.ID == null) {
                            continue;
                        }

                        LocalizationUtils.RemoveTableEntry(locField.LocProperty.ID, StorySource);
                    }
                }
            }
        }

        public static void RemoveAllStringTableEntriesFromPrefab(string prefabPath) {
            GameObject prefab = null;
            try {
                prefab = PrefabUtility.LoadPrefabContents(prefabPath);
                Component[] allComponents = prefab.GetComponentsInChildren<Component>(true);
                List<GameObjectLocalizer> localizes = allComponents.Where(c => c is GameObjectLocalizer).Cast<GameObjectLocalizer>().ToList();
                foreach (var localize in localizes) {
                    var tmp = localize.GetComponent<TextMeshProUGUI>();
                    if (tmp != null) {
                        var trackedObject = localize.GetTrackedObject(tmp);
                        foreach (var prop in trackedObject.TrackedProperties) {
                            if (prop is LocalizedStringProperty stringProp) {
                                var collection = LocalizationEditorSettings.GetStringTableCollection(stringProp.LocalizedString.TableReference);
                                var entry = collection.SharedData.GetEntryFromReference(stringProp.LocalizedString.TableEntryReference);
                                LocalizationUtils.RemoveTableEntry(entry.Key, PrefabSource);
                            }
                        }
                    }
                }
                IEnumerable<Component> components = allComponents.Except(localizes);
                foreach (var component in components) {
                    var locFields = GetLocalizedProperties(component);
                    foreach (var locField in locFields) {
                        if (!string.IsNullOrWhiteSpace(locField?.LocProperty?.ID)) {
                            LocalizationUtils.RemoveTableEntry(locField.LocProperty.ID, PrefabSource);
                        }
                    }
                }
            } catch (Exception e) {
                Log.Important?.Error(e.ToString());
            }
            if (prefab != null) {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        public static T GetOrCreateMetadata<T>(this StringTableEntry entry) where T : class, IMetadata, new() {
            T meta = entry.GetMetadata<T>();
            if (meta == null) {
                meta = new T();
                entry.AddMetadata(meta);
            }
            return meta;
        }
        
        [MenuItem("TG/Build/Baking/Localizations - Prepare for Build")]
        public static void PrepareForBuild() {
            AssetDatabase.StartAssetEditing();
            
            // Remove old localizations
            OldLocalizationsCollection.ClearAllEntries();
            foreach (var table in OldLocalizationsCollection.StringTables) {
                EditorUtility.SetDirty(table);
            }
            EditorUtility.SetDirty(OldLocalizationsCollection.SharedData);
            
            // Remove metadata & empty entries
            foreach (var collection in Collections) {
                foreach (var stringTable in collection.StringTables) {
                    // Remove invalid entries
                    foreach (var entry in stringTable.Values.ToList()) {
                        if (string.IsNullOrWhiteSpace(entry.Value) && entry.Key != null) {
                            stringTable.RemoveEntry(entry.Key);
                        }
                    }
                    
                    // Remove metadata
                    foreach (var entry in stringTable.Values) {
                        for (int i = entry.MetadataEntries.Count - 1; i >= 0; i--) {
                            var metadata = entry.MetadataEntries[i];
                            if (metadata is not SmartFormatTag) {
                                entry.RemoveMetadata(metadata);
                            }
                        }
                    }
                    EditorUtility.SetDirty(stringTable);
                }
                
                // Remove from shared data
                foreach (var entry in collection.SharedData.Entries) {
                    for (int i = entry.Metadata.MetadataEntries.Count - 1; i >= 0; i--) {
                        var metadata = entry.Metadata.MetadataEntries[i];
                        if (metadata is not GestureMetadata) {
                            entry.Metadata.RemoveMetadata(metadata);
                        }
                    }
                }
                
                EditorUtility.SetDirty(collection.SharedData);
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.StopAssetEditing();
        }
    }

    public readonly struct LocTermData {
        public readonly string id;
        public readonly StringTableEntry englishEntry;
        
        public StringTableEntry EntryFor(Locale locale, bool allowCreate = true) {
            StringTableEntry entry = LocalizationTools.GetTableEntryForLocale(id, locale);
            if (allowCreate && entry == null && englishEntry != null) {
                var tableId = englishEntry.Table.TableCollectionName;
                StringTable stringTable = LocalizationSettings.StringDatabase.GetTable(tableId, locale);
                entry = stringTable.AddEntry(id, "");
            }
            return entry;
        }

        public bool IsSet => id != null;
        public bool IsEmpty => englishEntry == null;

        public LocTermData(string id) {
            this.id = id;
            englishEntry = LocalizationTools.GetTableEntryForLocale(id, LocalizationSettings.ProjectLocale);
        }
    }
}