using System.Collections.Generic;
using Awaken.TG.Main.Localization;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace Awaken.Babel.Editor {
    public static class BabelBakerUtils {
        [UnityEditor.MenuItem("TG/Localization/Bake All Localization")]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void BakeAllLocalization() {
#if SCENES_PROCESSED
            return;
#endif
            if (Application.isBatchMode) {
                var initOperation = LocalizationSettings.InitializationOperation;
                if (!initOperation.IsDone) {
                    initOperation.WaitForCompletion();
                }
            }
            
            var allIds = LoadAllIds();
            
            var baker = new BabelBaker(allIds);
            
            var locales = LocalizationSettings.AvailableLocales.Locales;
            
            foreach (var locale in locales) {
                var inputData = new BabelLanguageInputDatum[allIds.Count];

                var index = 0u;
                if (locale == LocalizationSettings.ProjectLocale) {
                    var metaData = new BabelMetaInputDatum[allIds.Count];

                    foreach (var id in allIds.Keys) {
                        var entryResult = LocalizationHelper.EditorOnly_GetTableEntry(id, locale);
                        string translation = LocalizationHelper.EditorOnly_Translate(id, locale, entryResult, true);
                        bool hasSmartTag = entryResult.entry?.IsSmart ?? false;

                        var gestureMeta = entryResult.entry?.SharedEntry?.Metadata.GetMetadata<GestureMetadata>();
                        metaData[index] = new BabelMetaInputDatum(
                            id,
                            gestureMeta?.GestureKey ?? string.Empty
                            );

                        inputData[index++] = new BabelLanguageInputDatum(
                            id,
                            translation,
                            hasSmartTag
                            );
                    }

                    baker.BakeMeta(metaData);
                } else {
                    foreach (var id in allIds.Keys) {
                        var entryResult = LocalizationHelper.EditorOnly_GetTableEntry(id, locale);
                        string translation = LocalizationHelper.EditorOnly_Translate(id, locale, entryResult, true);
                        bool hasSmartTag = entryResult.entry?.IsSmart ?? false;

                        inputData[index++] = new BabelLanguageInputDatum(
                            id,
                            translation,
                            hasSmartTag
                            );
                    }
                }
                
                baker.BakeLocale(locale.Identifier, inputData);
            }
        }
        
        static Dictionary<string, uint> LoadAllIds() {
            var allIds = new Dictionary<string, uint>();
            var index = 0u;
            foreach (var tableId in LocalizationHelper.StringTables) {
                var tableCollection = LocalizationEditorSettings.GetStringTableCollection(tableId);
                foreach (var entry in tableCollection.SharedData.Entries) {
                    if (allIds.TryAdd(entry.Key, index)) {
                        ++index;
                    }
                }
            }
            return allIds;
        }
    }
}