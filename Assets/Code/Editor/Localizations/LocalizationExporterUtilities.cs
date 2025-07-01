using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Awaken.TG.Main.Localization;
using JetBrains.Annotations;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using static Awaken.TG.Editor.Localizations.LocalizationTools;

namespace Awaken.TG.Editor.Localizations {
    public static class LocalizationExporterUtilities {
        // === Static Fields & Properties
        static Locale[] Locales => LocalizationTools.Locales;
        public static Locale EnglishLocale => Locales.FirstOrDefault(l => l.Identifier == "en");
        public static IEnumerable GetAllLanguages() => LocalizationTools.Locales.Select(l => l.LocaleName);

        #region Exporting
        public static List<DataToExport> GetDataForExportGrouped(List<DataToExport> baseData) {
            List<DataToExport> groupedData = new();
            var groupedDataGroup = baseData.GroupBy(d => d.Source.Trim())
                .SelectMany(group => {
                    var tempGroup = group.GroupBy(d => d.translation.Trim()).ToList();
                    if (tempGroup.Count == 2 && tempGroup.Any(g => g.Key.IsNullOrWhitespace())) {
                        string translation = tempGroup.First(g => !g.Key.IsNullOrWhitespace()).Key.Trim();
                        return group.GroupBy(_ => translation);
                    }

                    return tempGroup;
                });
            foreach (var group in groupedDataGroup) {
                DataToExport firstData = group.First();
                DataToExport dataToExport = new(firstData);
                dataToExport.groupedTermsData = new List<LocTermData> { dataToExport.termData };
                bool gestureSet = !string.IsNullOrWhiteSpace(dataToExport.gesture);
                bool actorSet = !string.IsNullOrWhiteSpace(dataToExport.actor);
                foreach (var d in group) {
                    if (d == firstData) continue;
                    dataToExport.groupedTermsData.Add(d.termData);
                    if (!gestureSet && !string.IsNullOrWhiteSpace(d.gesture)) {
                        dataToExport.gesture = d.gesture;
                        gestureSet = true;
                    }

                    if (!actorSet && !string.IsNullOrWhiteSpace(d.actor)) {
                        dataToExport.actor = d.actor;
                        actorSet = true;
                    }
                }

                groupedData.Add(dataToExport);
            }
            return groupedData;
        }
        
        public static List<DataToExport> GetDataForExportUngrouped(List<DataToExport> baseData) {
            List<DataToExport> result = new();
            foreach (var data in baseData) {
                DataToExport entryToExport = new(data);
                entryToExport.groupedTermsData = new List<LocTermData> { data.termData };
                result.Add(entryToExport);
            }
            return result;
        }

        public static void ExportCsv(string path, string name, List<DataToExport> data, bool isStory) {
            using var stream = File.OpenWrite(Path.Combine(path, $"{name}.csv"));
            using var writer = new StreamWriter(stream);
            // using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            // var dataToExportMap = new DataToExportMap(isStory);
            // csv.Context.RegisterClassMap(dataToExportMap);
            // csv.WriteRecords(data);
        }
        #endregion

        #region Importing

        public static void ImportCSV(bool importAsRedacted, bool importAsProofread, string path) {
            if (!string.IsNullOrWhiteSpace(path) && Path.GetExtension(path) == ".csv") {
                IEnumerable<DataToExport> data = ImportDataFrom(path);
                foreach (var d in data.Where(d => d.ID != null && !string.IsNullOrWhiteSpace(d.translation))) {
                    foreach (var termData in d.groupedTermsData) {
                        ImportEntry(importAsRedacted, importAsProofread, termData, d);
                    }
                }

                LocalizationTools.UpdateAllSources();
            }
        }

        static void ImportEntry(bool importAsRedacted, bool importAsProofread, LocTermData termData, DataToExport d) {
            var locale = d.CurrentLocale;
            StringTableEntry tableEntry = termData.EntryFor(locale);
            
            if (tableEntry != null) {
                bool mismatch = d.originalSource.Trim() != d.Source.Trim();

                if (!mismatch) {
                    bool isEnglish = locale == LocalizationExporterUtilities.EnglishLocale;
                    int hash = isEnglish ? d.translation.Trim().GetHashCode() : d.originalSource.Trim().GetHashCode();
                    
                    ApplyTermStatus(tableEntry, hash, importAsRedacted, importAsProofread);
                }

                if (!mismatch || locale != EnglishLocale) {
                    LocalizationUtils.ChangeTextTranslation(termData.id, d.translation, tableEntry.Table as StringTable, true);
                }
                d.ApplyGesture(termData);
            }
        }
        
        static void ApplyTermStatus(StringTableEntry tableEntry, int hash, bool redacted, bool proofRead) {
            TermStatusMeta meta = tableEntry.GetOrCreateMetadata<TermStatusMeta>();

            if (redacted) {
                meta.TranslationHash = hash;
            }
            if (proofRead) {
                meta.ProofreadHash = hash;
            }
            
            EditorUtility.SetDirty(LocalizationExporterWindow.CurrentSource);
            EditorUtility.SetDirty(tableEntry.Table);
        }

        static IEnumerable<DataToExport> ImportDataFrom(string path) {
            using var stream = File.OpenRead(Path.Combine(path));
            using var writer = new StreamReader(stream);
            // using var csv = new CsvReader(writer, CultureInfo.InvariantCulture);
            // if (csv.Read() && csv.ReadHeader()) {
            //     var dataMap = new DataToImportMap(csv.HeaderRecord);
            //     csv.Context.RegisterClassMap(dataMap);
            //     return csv.GetRecords<DataToExport>().ToList();
            // } else {
            //     throw new NullReferenceException("No header found in CSV file.");
            // }
            throw new NotImplementedException();
        }

        #endregion

        // === Helper Classes
        const string IdColumn = "ID";
        const string SourceColumn = "Source";
        const string CategoryColumn = "Category";
        const string GestureColumn = "Gesture";
        const string ActorColumn = "Actor";
        const string TranslationColumn = "Translation";
        const string PreviousLineColumn = "PreviousLine";
        const string LanguageColumn = "Language";
        
        // [UsedImplicitly]
        // internal sealed class DataToExportMap : ClassMap<DataToExport> {
        //     public DataToExportMap(bool isStory) {
        //         int index = 0;
        //         Map(m => m.groupedTermsData).Index(index).TypeConverter<TermDataGroupedConverter>().Name(IdColumn);
        //         Map(m => m.Source).Index(++index).Name(SourceColumn);
        //         if (!isStory) {
        //             Map(m => m.category).Index(++index).Name(CategoryColumn);
        //         } else {
        //             Map(m => m.gesture).Index(++index).Name(GestureColumn);
        //             Map(m => m.actor).Index(++index).Name(ActorColumn);
        //             Map(m => m.previousLine).Index(++index).Name(PreviousLineColumn);
        //         }
        //         Map(m => m.translation).Index(6).Name(TranslationColumn);
        //         Map(m => m.CurrentLocale).Index(7).TypeConverter<TermDataConverter>().Name(LanguageColumn);
        //     }
        // }

        // /// <summary>
        // /// Almost identical to DataToExportMap, yet we need other class for importing because Source isn't stored,
        // /// and we need to check whether the Source changed since last export.
        // /// </summary>
        // [UsedImplicitly]
        // internal sealed class DataToImportMap : ClassMap<DataToExport> {
        //     public DataToImportMap(string[] header) {
        //         int idIndex = 0;
        //         int sourceIndex = 1;
        //         int categoryIndex = Array.IndexOf(header, CategoryColumn);
        //         int gestureIndex = Array.IndexOf(header, GestureColumn);
        //         int actorIndex = Array.IndexOf(header, ActorColumn);
        //         int translationIndex = Array.IndexOf(header, TranslationColumn);
        //         int languageIndex = Array.IndexOf(header, LanguageColumn);
        //         
        //         Map(m => m.termData).Index(idIndex).TypeConverter<TermDataConverter>().Name(IdColumn);
        //         Map(m => m.groupedTermsData).Index(idIndex).TypeConverter<TermDataGroupedConverter>().Name(IdColumn);
        //         Map(m => m.originalSource).Index(sourceIndex).Name(SourceColumn);
        //         if (categoryIndex != -1) {
        //             Map(m => m.category).Index(categoryIndex).Name(CategoryColumn);
        //         }
        //         if (gestureIndex != -1) {
        //             Map(m => m.gesture).Index(gestureIndex).Name(GestureColumn);
        //         }
        //         if (actorIndex != -1) {
        //             Map(m => m.actor).Index(actorIndex).Name(ActorColumn);
        //         }
        //         Map(m => m.translation).Index(translationIndex).Name(TranslationColumn);
        //         Map(m => m.CurrentLocale).Index(languageIndex).TypeConverter<TermDataConverter>().Name(LanguageColumn);
        //     }
        // }

        // [UsedImplicitly]
        // public class TermDataConverter : DefaultTypeConverter {
        //     public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData) {
        //         if (memberMapData.Type == typeof(Locale)) {
        //             return Locales.FirstOrDefault(l => l.LocaleName == text);
        //         } else {
        //             var term = text.Split(';').First();
        //             return TryGetTermData(term);
        //         }
        //     }
        //
        //     public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData) {
        //         if (value is LocTermData termData) {
        //             return termData.id;
        //         } else if (value is Locale locale) {
        //             return locale.ToString();
        //         }
        //
        //         return string.Empty;
        //     }
        // }

        // [UsedImplicitly]
        // public class TermDataGroupedConverter : DefaultTypeConverter {
        //     public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData) {
        //         string[] ids = text.Split(';');
        //         return ids.Where(id => !string.IsNullOrWhiteSpace(id)).Select(TryGetTermData).ToList();
        //     }
        //
        //     public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData) {
        //         if (value is List<LocTermData> groupedTableEntries) {
        //             StringBuilder stringBuilder = new StringBuilder();
        //             foreach (var termData in groupedTableEntries) {
        //                 stringBuilder.Append(termData.id);
        //                 if (!Equals(groupedTableEntries.Last(), termData)) {
        //                     stringBuilder.Append(';');
        //                 }
        //             }
        //
        //             return stringBuilder.ToString();
        //         }
        //
        //         return string.Empty;
        //     }
        // }
    }
}