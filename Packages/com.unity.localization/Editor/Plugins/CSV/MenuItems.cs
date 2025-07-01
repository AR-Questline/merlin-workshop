using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor.Localization.Plugins.CSV.Columns;
using UnityEditor.Localization.Reporting;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Localization.Tables;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace UnityEditor.Localization.Plugins.CSV
{
    static class MenuItems
    {
        const string k_PrefFile = "Localization-csv-file";

        public static string PreviousDirectory
        {
            get
            {
                var file = EditorPrefs.GetString(k_PrefFile, "Assets/");
                return Path.GetDirectoryName(file);
            }
            set => EditorPrefs.SetString(k_PrefFile, value);
        }

        [LocalizationImportMenu]
        public static void PopulateImportMenu(LocalizationTableCollection collection, DropdownMenu menu)
        {
            if (collection is StringTableCollection stringTableCollection)
            {
                using (ListPool<CsvExtension>.Get(out var extensions))
                {
                    foreach (var e in stringTableCollection.Extensions)
                    {
                        if (e is CsvExtension csv)
                            extensions.Add(csv);
                    }

                    menu.AppendAction(L10n.Tr("CSV..."), _ => ImportCollection(new MenuCommand(collection)));
                    menu.AppendAction(L10n.Tr("CSV(Merge)..."), _ => ImportCollectionMerge(new MenuCommand(collection)));

                    for (int i = 0 ; i < extensions.Count; i++)
                    {
                        var extension = extensions[i];
                        menu.AppendAction(L10n.Tr($"CSV(Extension #{i + 1} settings)..."), _ => ImportIntoCollection(stringTableCollection, extension.Columns, true));
                        menu.AppendAction(L10n.Tr($"CSV(Merge) (Extension #{i + 1} settings)..."), _ => ImportIntoCollection(stringTableCollection, extension.Columns, false));
                    }
                }
            }
        }

        [LocalizationExportMenu]
        public static void PopulateExportMenu(LocalizationTableCollection collection, DropdownMenu menu)
        {
            if (collection is StringTableCollection stringTableCollection)
            {
                // Check for extensions
                using (ListPool<CsvExtension>.Get(out var extensions))
                {
                    foreach (var e in stringTableCollection.Extensions)
                    {
                        if (e is CsvExtension csv)
                            extensions.Add(csv);
                    }

                    menu.AppendAction(L10n.Tr("CSV..."), _ => ExportCollection(new MenuCommand(collection)));
                    menu.AppendAction(L10n.Tr("CSV(With Comments)..."), _ => ExportCollectionWithComments(new MenuCommand(collection)));
                    for (int i = 0; i < extensions.Count; i++)
                    {
                        var extension = extensions[i];
                        menu.AppendAction(L10n.Tr($"CSV(Extension #{i + 1} settings)..."), _ => Export(extension.Columns, stringTableCollection));
                    }
                }
            }
        }

        [MenuItem("CONTEXT/StringTableCollection/Export/CSV...")]
        public static void ExportCollection(MenuCommand command)
        {
            var collection = command.context as StringTableCollection;
            Assert.IsTrue(collection != null, "Expected StringTableCollection");
            Export(ColumnMapping.CreateDefaultMapping(false), collection);
        }

        [MenuItem("CONTEXT/StringTableCollection/Export/CSV(With Comments)...")]
        public static void ExportCollectionWithComments(MenuCommand command)
        {
            var collection = command.context as StringTableCollection;
            Assert.IsTrue(collection != null, "Expected StringTableCollection");
            Export(ColumnMapping.CreateDefaultMapping(true), collection);
        }

        [MenuItem("CONTEXT/StringTable/Export/CSV...")]
        static void ExportStringTable(MenuCommand command)
        {
            var table = command.context as StringTable;
            Assert.IsTrue(table != null, "Expected StringTable");
            ExportTable(table, false);
        }

        [MenuItem("CONTEXT/StringTable/Export/CSV(With Comments)...")]
        static void ExportStringTableWithComments(MenuCommand command)
        {
            var table = command.context as StringTable;
            Assert.IsTrue(table != null, "Expected StringTable");
            ExportTable(table, true);
        }

        static void ExportTable(StringTable table, bool includeComments)
        {
            var collection = LocalizationEditorSettings.GetCollectionFromTable(table) as StringTableCollection;
            if (collection == null)
            {
                Debug.LogError("String Table must belong to a StringTableCollection.");
                return;
            }

            var cellMappings = new CsvColumns[] { new KeyIdColumns { IncludeSharedComments = includeComments }, new LocaleColumns { LocaleIdentifier = table.LocaleIdentifier, IncludeComments = includeComments } };
            Export(cellMappings, collection);
        }

        static void Export(IList<CsvColumns> cellMappings, StringTableCollection collection)
        {
            var path = EditorUtility.SaveFilePanel($"Export {collection.TableCollectionName} to CSV", PreviousDirectory, collection.TableCollectionName, "csv");
            if (string.IsNullOrEmpty(path))
                return;

            PreviousDirectory = path;

            // Use FileShare.ReadWrite to avoid IOException: Sharing violation (LOC-348)
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                var stream = new StreamWriter(fs, new UTF8Encoding(false));
                var reporter = TaskReporter.CreateDefaultReporter();
                reporter.Start("Exporting " + path, string.Empty);
                Csv.Export(stream, collection, cellMappings, reporter);
            }

            EditorUtility.RevealInFinder(path);
        }

        [MenuItem("CONTEXT/StringTableCollection/Import/CSV...")]
        public static void ImportCollection(MenuCommand command)
        {
            var collection = command.context as StringTableCollection;
            ImportIntoCollection(collection, null, true);
        }

        [MenuItem("CONTEXT/StringTableCollection/Import/CSV(Merge)...")]
        public static void ImportCollectionMerge(MenuCommand command)
        {
            var collection = command.context as StringTableCollection;
            ImportIntoCollection(collection, null, false);
        }

        static void ImportIntoCollection(StringTableCollection collection, IList<CsvColumns> columns, bool removeMissingEntries)
        {
            Assert.IsNotNull(collection, "Expected StringTableCollection");

            var path = EditorUtility.OpenFilePanel($"Import CSV into {collection.TableCollectionName}", PreviousDirectory, "csv");
            if (string.IsNullOrEmpty(path))
                return;

            PreviousDirectory = path;

            // Use FileShare.ReadWrite to avoid IOException: Sharing violation (LOC-348)
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var stream = new StreamReader(fs);
                var reporter = TaskReporter.CreateDefaultReporter();
                reporter.Start("Importing " + path, string.Empty);

                if (columns == null)
                    Csv.ImportInto(stream, collection, true, reporter, removeMissingEntries);
                else
                    Csv.ImportInto(stream, collection, columns, true, reporter, removeMissingEntries);
            }
        }

        [MenuItem("CONTEXT/StringTable/Import/CSV...")]
        static void ImportStringTable(MenuCommand command)
        {
            var table = command.context as StringTable;
            ImportIntoStringTable(table, true);
        }

        [MenuItem("CONTEXT/StringTable/Import/CSV(Merge)...")]
        static void ImportStringTableMerge(MenuCommand command)
        {
            var table = command.context as StringTable;
            ImportIntoStringTable(table, false);
        }

        static void ImportIntoStringTable(StringTable table, bool removeMissingEntries)
        {
            Assert.IsNotNull(table, "Expected StringTable");

            var path = EditorUtility.OpenFilePanel($"Import CSV into {table.TableData}({table.LocaleIdentifier})", PreviousDirectory, "csv");
            if (string.IsNullOrEmpty(path))
                return;

            PreviousDirectory = path;

            var collection = LocalizationEditorSettings.GetCollectionFromTable(table) as StringTableCollection;
            if (collection == null)
            {
                Debug.LogError("String Table must belong to a StringTableCollection.");
                return;
            }

            var cellMappings = new CsvColumns[] { new KeyIdColumns(), new LocaleColumns { LocaleIdentifier = table.LocaleIdentifier } };

            // Use FileShare.ReadWrite to avoid IOException: Sharing violation (LOC-348)
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var stream = new StreamReader(fs);
                var reporter = TaskReporter.CreateDefaultReporter();
                reporter.Start("Importing " + path, string.Empty);
                Csv.ImportInto(stream, collection, cellMappings, true, reporter, removeMissingEntries);
            }
        }
    }
}
