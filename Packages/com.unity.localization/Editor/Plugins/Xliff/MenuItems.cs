using UnityEditor.Localization.Reporting;
using UnityEngine;
using UnityEngine.Localization.Tables;
using UnityEngine.UIElements;

namespace UnityEditor.Localization.Plugins.XLIFF
{
    static class MenuItems
    {
        const string k_PrefXliffDirectory = "Localization-xliff-import-directory";
        const string k_PrefXliffFile = "Localization-xliff-import-file";

        [LocalizationImportMenu]
        public static void PopulateImportMenu(LocalizationTableCollection collection, DropdownMenu menu)
        {
            if (collection is StringTableCollection)
            {
                menu.AppendAction(L10n.Tr("XLIFF into collection"), _ => ImportIntoCollection(new MenuCommand(collection)));
                menu.AppendAction(L10n.Tr("XLIFF file"), _ => ImportXliffFile());
                menu.AppendAction(L10n.Tr("XLIFF directory"), _ => ImportXliffDirectory());
            }
        }

        [LocalizationExportMenu]
        public static void PopulateExportMenu(LocalizationTableCollection collection, DropdownMenu menu)
        {
            if (collection is StringTableCollection)
            {
                menu.AppendAction(L10n.Tr("XLIFF..."), _ => ExportCollection(new MenuCommand(collection)));
            }
        }

        [MenuItem("CONTEXT/StringTableCollection/Export/XLIFF...")]
        public static void ExportCollection(MenuCommand command)
        {
            var collection = command.context as StringTableCollection;
            Debug.Assert(collection != null, "Expected StringTableCollection");

            var wnd = ExporterWindow.ShowWindow();
            wnd.SelectedTables.SetSelection(collection);
        }

        [MenuItem("CONTEXT/StringTable/Export/XLIFF...")]
        static void ExportStringTable(MenuCommand command)
        {
            var table = command.context as StringTable;
            Debug.Assert(table != null, "Expected StringTable");

            var wnd = ExporterWindow.ShowWindow();
            wnd.SelectedTables.SetSelection(table);
        }

        [MenuItem("CONTEXT/StringTableCollection/Import/XLIFF...")]
        public static void ImportIntoCollection(MenuCommand command)
        {
            var collection = command.context as StringTableCollection;
            Debug.Assert(collection != null, "Expected StringTableCollection");

            var file = EditorUtility.OpenFilePanel("Import XLIFF", EditorPrefs.GetString(k_PrefXliffFile, ""), "xlf");
            if (string.IsNullOrEmpty(file))
                return;
            EditorPrefs.SetString(k_PrefXliffFile, file);

            Xliff.ImportFileIntoCollection(collection, file, null, TaskReporter.CreateDefaultReporter());
        }

        [MenuItem("CONTEXT/StringTable/Import/XLIFF...")]
        static void ImportIntoTable(MenuCommand command)
        {
            var table = command.context as StringTable;
            Debug.Assert(table != null, "Expected StringTable");

            var file = EditorUtility.OpenFilePanel("Import XLIFF", EditorPrefs.GetString(k_PrefXliffFile, ""), "xlf");
            if (string.IsNullOrEmpty(file))
                return;
            EditorPrefs.SetString(k_PrefXliffFile, file);

            Xliff.ImportFileIntoTable(file, table, Xliff.ImportNotesBehavior.Replace, TaskReporter.CreateDefaultReporter());
        }

        public static void ImportXliffDirectory()
        {
            var dir = EditorUtility.OpenFolderPanel("Import XLIFF from directory", EditorPrefs.GetString(k_PrefXliffDirectory, ""), "");
            if (string.IsNullOrEmpty(dir))
                return;
            EditorPrefs.SetString(k_PrefXliffDirectory, dir);

            Xliff.ImportDirectory(dir, null, TaskReporter.CreateDefaultReporter());
        }

        public static void ImportXliffFile()
        {
            var file = EditorUtility.OpenFilePanel("Import XLIFF", "", "xlf");
            if (string.IsNullOrEmpty(file))
                return;

            Xliff.ImportFile(file, null, TaskReporter.CreateDefaultReporter());
        }
    }
}
