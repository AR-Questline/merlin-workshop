using System.Linq;
using UnityEditor.Localization.Reporting;
using UnityEditor.Localization.UI;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UIElements;
using Resources = UnityEditor.Localization.UI.Resources;

namespace UnityEditor.Localization.Plugins.XLIFF
{
    class ExporterWindow : EditorWindow
    {
        const string k_XliffSourceLanguage = "Localization-XLIFF-SourceLanguage";

        ObjectField m_SourceLocale;

        public ProjectCollectionsTableSelector SelectedTables { get; private set; }

        Locale SourceLangauge
        {
            get => LocalizationEditorSettings.GetLocale(EditorPrefs.GetString(k_XliffSourceLanguage, "en"));
            set => EditorPrefs.SetString(k_XliffSourceLanguage, value?.Identifier.Code);
        }

        string ExportName
        {
            get
            {
                var nameField = rootVisualElement.Q<TextField>("name-text-field");
                return string.IsNullOrEmpty(nameField.value) ? Application.productName : nameField.value;
            }
        }

        public static ExporterWindow ShowWindow() => GetWindow<ExporterWindow>("Export XLIFF", true);

        void OnEnable()
        {
            var template = Resources.GetTemplate("XliffExporterWindow");
            rootVisualElement.Add(template);

            m_SourceLocale = rootVisualElement.Q<ObjectField>();
            m_SourceLocale.objectType = typeof(Locale);
            m_SourceLocale.value = SourceLangauge;
            m_SourceLocale.RegisterValueChangedCallback(evt => SourceLangauge = evt.newValue as Locale);

            var exportButton = rootVisualElement.Q<Button>("export-button");
            exportButton.clicked += Export;

            SelectedTables = rootVisualElement.Q<ProjectCollectionsTableSelector>();
        }

        void Export()
        {
            var sourceLocaleField = rootVisualElement.Q<ObjectField>();
            var sourceLocale = sourceLocaleField.value as Locale;
            if (sourceLocale == null)
            {
                UnityEngine.Debug.LogError("A Source Language must be assigned");
                return;
            }

            bool anyTablesSelected = SelectedTables.SelectedTableIndexes.Any(k => k.Value.Count > 0);
            if (!anyTablesSelected)
            {
                UnityEngine.Debug.LogError("No tables selected for export. Please select at least 1 table to export to XLIFF.");
                return;
            }

            var dir = EditorUtility.SaveFolderPanel("Export XLIFF to directory", "", "");
            if (string.IsNullOrEmpty(dir))
                return;

            var version = rootVisualElement.Q<XliffVersionPopup>();

            var selectedTables = SelectedTables.SelectedTableIndexes.ToDictionary(k => k.Key as StringTableCollection, v => v.Value);
            Xliff.ExportSelected(sourceLocale.Identifier, dir, ExportName, version.value, selectedTables, TaskReporter.CreateDefaultReporter());
            EditorUtility.OpenWithDefaultApp(dir);
        }
    }
}
