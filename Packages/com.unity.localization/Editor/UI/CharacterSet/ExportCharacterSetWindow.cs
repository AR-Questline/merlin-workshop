using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Localization.Tables;
using UnityEngine.UIElements;

namespace UnityEditor.Localization.UI
{
    class ExportCharacterSetWindow : EditorWindow
    {
        public ProjectCollectionsTableSelector SelectedTables { get; private set; }

        public static ExportCharacterSetWindow ShowWindow() => GetWindow<ExportCharacterSetWindow>("Export Character Set", true);

        [LocalizationExportMenu]
        static void PopulateExportMenu(LocalizationTableCollection collection, DropdownMenu menu)
        {
            if (collection is StringTableCollection)
            {
                menu.AppendAction(L10n.Tr("Character Set..."), _ => ShowWindow().SelectedTables.SetSelection(collection));
            }
        }

        void OnEnable()
        {
            var template = Resources.GetTemplate(nameof(ExportCharacterSetWindow));
            rootVisualElement.Add(template);

            var exportButton = rootVisualElement.Q<Button>("export-button");
            exportButton.clicked += Export;

            SelectedTables = rootVisualElement.Q<ProjectCollectionsTableSelector>();
        }

        void Export()
        {
            bool anyTablesSelected = SelectedTables.SelectedTableIndexes.Any(k => k.Value.Count > 0);
            if (!anyTablesSelected)
            {
                Debug.LogError("No tables selected for export. Please select at least 1 table to export distinct characters from.");
                return;
            }

            var path = EditorUtility.SaveFilePanel("Export Character Set", "Assets/", "Character Set", "txt");
            if (string.IsNullOrEmpty(path))
                return;

            var collectionsWithSelectedIndexes = SelectedTables.SelectedTableIndexes;

            // We combine the distinct characters for every collection and then run a final Distinct.
            IEnumerable<char> e = "";
            foreach (var kvp in collectionsWithSelectedIndexes)
            {
                if (kvp.Value.Count == 0)
                    continue;

                var stringTableCollection = kvp.Key as StringTableCollection;
                foreach (var idx in kvp.Value)
                {
                    var table = stringTableCollection.Tables[idx].asset as StringTable;
                    e = e.Concat(table.CollectLiteralCharacters());
                }
            }

            var distinctCharacters = string.Concat(e.Distinct().OrderBy(c => c));
            File.WriteAllText(path, distinctCharacters, new UTF8Encoding(false));
            AssetDatabase.Refresh();

            EditorUtility.RevealInFinder(path);
        }
    }
}
