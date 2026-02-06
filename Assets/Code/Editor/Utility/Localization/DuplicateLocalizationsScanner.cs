using System.Collections.Generic;
using Awaken.Utility;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Awaken.TG.Editor.Utility.Localization {
    public struct DuplicateLocResult {
        public static string[] LocTables => LocalizationHelper.StringTables;

        public DuplicateLocResult(string locString, StringTableEntry entry, int index) {
            this.locString = locString;
            enText = entry.Value;
            results = new bool[LocTables.Length];
            results[index] = true;
        }
        
        public string locString;
        public string enText;
        public bool[] results;
    }
    
    public static class DuplicateLocalizationsScanner {
        public static List<DuplicateLocResult> GetAllDuplicates(bool searchInStory) {
            var duplicates = new List<DuplicateLocResult>();
            foreach (var duplicateLocResult in GetAllLocs(searchInStory)) {
                int useCount = 0;
                for (int j = 0; j < duplicateLocResult.results.Length; j++) {
                    if (duplicateLocResult.results[j]) {
                        useCount++;
                        if (useCount >= 2) {
                            duplicates.Add(duplicateLocResult);
                            break;
                        }
                    }
                }
            }
            return duplicates;
        }
        
        static IEnumerable<DuplicateLocResult> GetAllLocs(bool searchInStory) {
            Dictionary<string, DuplicateLocResult> allLocs = new();
            int index = 0;
            if (DuplicateLocResult.LocTables[index] == LocalizationHelper.StoryTable && !searchInStory) {
                index++;
            }
            var table = GetTable(index, LocalizationHelper.SelectedLocale);
            foreach (var pair in table) {
                var key = GetKey(table, pair.Value);
                var value = new DuplicateLocResult(key, pair.Value, index);
                allLocs.Add(key, value);
            }

            index++;
            for (; index < DuplicateLocResult.LocTables.Length; index++) {
                if (DuplicateLocResult.LocTables[index] == LocalizationHelper.StoryTable && !searchInStory) {
                    continue;
                }
                table = GetTable(index, LocalizationHelper.SelectedLocale);
                foreach (var pair in table) {
                    var key = GetKey(table, pair.Value);
                    if (allLocs.TryGetValue(key, out var existing)) {
                        existing.results[index] = true;
                        //results[pair.Value.Key] = existing;
                    } else {
                        allLocs.Add(key, new DuplicateLocResult(key, pair.Value, index));
                    }
                }
            }

            return allLocs.Values;
        }
        
        static DetailedLocalizationTable<StringTableEntry> GetTable(int index, Locale locale) {
            var table = LocalizationSettings.StringDatabase.GetTable(DuplicateLocResult.LocTables[index], locale);
            return table;
        }
        
        static string GetKey(DetailedLocalizationTable<StringTableEntry> table, StringTableEntry entry) {
            var key = entry.Key;
            if (key == null) {
                key = $"INVALID KEY ID: {entry.KeyId}";
                Debug.LogError($"[{table.TableCollectionName}] Found null localization key with ID: {entry.KeyId} and value {entry.Value}");
            }
            return key;
        }
    }
}