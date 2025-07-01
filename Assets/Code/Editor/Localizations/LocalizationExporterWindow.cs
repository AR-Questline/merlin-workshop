using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Awaken.TG.Editor.Debugging.GUIDSearching;
using Awaken.TG.Editor.SceneCaches.Items;
using Awaken.TG.Editor.SceneCaches.Quests;
using Awaken.TG.Editor.SimpleTools;
using Awaken.TG.Editor.Utility.Paths;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Templates;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Extensions;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using static Awaken.TG.Editor.Localizations.LocalizationTools;
using SelectionType = Awaken.TG.Editor.Localizations.StoryScriptExporterWindow.Settings.SelectionType;

namespace Awaken.TG.Editor.Localizations {
    public class LocalizationExporterWindow : OdinEditorWindow {
        // === Const's
        const string WorkModeGroup = "WorkMode";
        const string ExportTabGroup = WorkModeGroup + "/" + ExportTab + "/Inside Export Tab";
        const string ImportTab = "Import", ExportTab = "Export";
        const string StoryTab = "Story", OtherTab = "Other (Prefabs, Overrides, LocTerms, KeyBindings, Tags)";
        const string LocTermsTableName = "LocTerms";

        static StringTableCollection s_source;
        static int s_selectedSource;
        static int s_selectedWorkMode;

        static OnDemandCache<string, ExportRegionFilter> s_regionFilterCache = new(_ => ExportRegionFilter.Unknown);
        readonly string _applicationDataPathWithoutAssets = Application.dataPath.Substring(0, Application.dataPath.Length - 6);
        readonly Dictionary<Locale, List<DataToExport>> _dataToExport = new();

        string _lastSelectedPath;
        string _lastSavedSelectedPath;

        List<DataToExport> _currentData = new();

        public static StringTableCollection CurrentSource {
            get {
                if (s_source == null || s_source.name != Sources[s_selectedSource]) {
                    s_source = LocalizationEditorSettings.GetStringTableCollection(Sources[s_selectedSource]);
                }

                return s_source;
            }
        }
        
        static string[] Sources => LocalizationHelper.StringTables;

        // === Initialization
        [MenuItem("TG/Localization/Localization Exporter")]
        public static void ShowWindow() {
            ShowAndInit();
            return;

            static async void ShowAndInit() {
                LocalizationSettings.Instance.ResetState();
                await LocalizationSettings.InitializationOperation.Task;
                var window = GetWindow<LocalizationExporterWindow>();
                window.titleContent = new GUIContent("Localization Exporter");
                window._languagesToExport = LocalizationExporterUtilities.GetAllLanguages().Cast<string>().ToList();
            }
        }

        #region Export Tab View
        [ShowInInspector, TabGroup(WorkModeGroup, ExportTab), TabGroup(ExportTabGroup, StoryTab), TabGroup(ExportTabGroup, OtherTab, TabId = OtherTab),
         HideDuplicateReferenceBox]
        ExportTarget _exportTo = ExportTarget.GoogleSheets;
        
        [ShowInInspector, TabGroup(WorkModeGroup, ExportTab), TabGroup(ExportTabGroup, OtherTab), HideDuplicateReferenceBox]
        [Tooltip("If you want to export only specific regions, select them here.")]
        ExportRegionFilter _regionFilter = ExportRegionFilter.All;
        
        [ShowInInspector, TabGroup(WorkModeGroup, ExportTab), TabGroup(ExportTabGroup, OtherTab), HideDuplicateReferenceBox]
        [Tooltip("If you want to exclude some regions from export, select them here.")]
        ExportRegionFilter _excludeRegionFilter = ExportRegionFilter.None;
        
        [ShowInInspector, TabGroup(WorkModeGroup, ExportTab), TabGroup(ExportTabGroup, OtherTab), HideDuplicateReferenceBox]
        [Tooltip("Remove Debug - removes all Debug and ForRemoval templates" +
                 "\nRemoveUnusedDebug - removes templates above but only if they aren't used in game currently.")]
        TemplateTypesExportFilter _templateTypesFilter = TemplateTypesExportFilter.RemoveUnusedDebug;
        
        [ShowInInspector, TabGroup(WorkModeGroup, ExportTab), TabGroup(ExportTabGroup, StoryTab), HideDuplicateReferenceBox]
        [LabelText("ADVANCED - Export Only Ungrouped Entries")]
        bool _exportOnlyUngroupedEntries;

        [ShowInInspector, TabGroup(WorkModeGroup, ExportTab), TabGroup(ExportTabGroup, StoryTab), HideDuplicateReferenceBox]
        SelectionType _selectBy = SelectionType.Reference;

        [ShowInInspector, TabGroup(WorkModeGroup, ExportTab), TabGroup(ExportTabGroup, StoryTab), HideDuplicateReferenceBox]
        bool _includeDebugGraphs = true;

        [ShowIf(nameof(_selectBy), SelectionType.Directory), TabGroup(ExportTabGroup, StoryTab), SerializeField, LabelText("Get localizations from story graphs in: "),
         FolderPath(AbsolutePath = false)]
        string[] directoryPaths = {
            "Assets/Data/Templates/Stories"
        };

        [ShowIf(nameof(_selectBy), SelectionType.Reference), TabGroup(ExportTabGroup, StoryTab), ShowInInspector, LabelText("Get localizations from story graphs: ")]
        public List<StoryGraph> storyGraphs;
        
        [TabGroup(WorkModeGroup, ExportTab), TabGroup(ExportTabGroup, StoryTab), TabGroup(ExportTabGroup, OtherTab), HideDuplicateReferenceBox]
        [ShowInInspector, ValueDropdown(nameof(AllLanguages), IsUniqueList = true, DropdownTitle = "Select Languages to export")]
        List<string> _languagesToExport = new();

        [TabGroup(WorkModeGroup, ExportTab), TabGroup(ExportTabGroup, StoryTab), TabGroup(ExportTabGroup, OtherTab), HideDuplicateReferenceBox]
        [ShowInInspector, Tooltip("$GetExportOptionsTooltip")]
        ExportOptions _exportFilters = ExportOptions.Missing | ExportOptions.RedactedInEnglish;
        
        IEnumerable AllLanguages => LocalizationExporterUtilities.GetAllLanguages();

        // === Exporting Data
        [TabGroup(ExportTabGroup, StoryTab), Button("Export"), Tooltip("Exports from story graphs found under paths defined in Directory Paths")]
        void ExportStory() {
            if (_exportTo == ExportTarget.CSV) {
                ExportStoryToCSV();
            } else {
                ExportToGoogleSheet(GetSpreadsheetDataForStory(), true).Forget();
            }
        }

        [TabGroup(ExportTabGroup, OtherTab), Button("Export"), Tooltip("Exports everything except story")]
        void ExportOther() {
            s_regionFilterCache.Clear();
            LootCache.Get.GenerateRegionFilters(s_regionFilterCache);
            QuestCacheBaker.GenerateRegionFilters(s_regionFilterCache);
            
            foreach (var key in s_regionFilterCache.Keys.ToList()) {
                if (s_regionFilterCache[key] != ExportRegionFilter.Unknown) {
                    s_regionFilterCache[key] &= ~ExportRegionFilter.Unknown;
                }
            }
            
            if (_exportTo == ExportTarget.CSV) {
                ExportOtherToCSV();
            } else {
                ExportToGoogleSheet(GetSpreadsheetDataForOther(), false).Forget();
            }

            s_regionFilterCache.Clear();
        }

        #endregion

        #region Import Tab View
        
        [ShowInInspector, TabGroup(WorkModeGroup, ImportTab), LabelText("Import as Redacted")]
        [Tooltip("If true, translations will be marked as redacted, this means they were translated, and no longer be exported as missing, however, they are not proofread. " +
                 "Use it for the 'First Import' of translations.")]
        bool _importTextAsRedacted = true;
        
        [ShowInInspector, TabGroup(WorkModeGroup, ImportTab), LabelText("Import as Proofread")]
        [Tooltip("If true, translations will be imported as proof read, which means they are ready for production, and were checked for correctness, controversies, special language rules, etc. " +
                 "Use it for the 'Second Import' of translation.")]
        bool _importAsProofread;
        
        // === Importing Data 
        [TabGroup(WorkModeGroup, ImportTab), Button]
        void ImportTranslationsFromDirectory() {
            string directory = EditorUtility.OpenFolderPanel("Select Directory To Import From", "", "");
            SearchOption directorySearchOption = SearchOption.TopDirectoryOnly;
            PopupWindow.DisplayDialog("Search in subfolders",
                "Do you want to import CSVs only from this folder or do you want to include all folders under it?", "Only This Folder", "Include Subfolders",
                () => { directorySearchOption = SearchOption.AllDirectories; });

            var files = Directory.GetFiles(directory, "*", directorySearchOption);
            int count = files.Length;
            int processed = 0;
            EditorUtility.DisplayProgressBar("Importing Translations From Files", $"Files Processed: 0/{count}", 0);
            AssetDatabase.StartAssetEditing();
            foreach (var pathToFile in files) {
                LocalizationExporterUtilities.ImportCSV(_importTextAsRedacted, _importAsProofread, pathToFile);
                processed++;
                EditorUtility.DisplayProgressBar("Importing Translations From Files", $"Files Processed: {processed}/{count}", processed / (float)count);
            }

            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }

        [TabGroup(WorkModeGroup, ImportTab), Button]
        void ImportTranslationsFromFile() {
            LocalizationExporterUtilities.ImportCSV(_importTextAsRedacted, _importAsProofread, EditorUtility.OpenFilePanel("Select File To Import", "", "csv"));
        }

        #endregion

        static bool HasAnyTranslation(StringTableEntry t, bool allowProjectLocale = true) {
            if (string.IsNullOrWhiteSpace(t.LocalizedValue) || string.Equals(t.LocalizedValue, t.Key)) {
                return false;
            }

            IEnumerable<Locale> locales = allowProjectLocale ? Locales : Locales.Where(l => l != LocalizationSettings.ProjectLocale);
            foreach (var locale in locales) {
                StringTable stringTable = (StringTable)CurrentSource.GetTable(locale.Identifier);
                if (t.Key != null && !string.IsNullOrWhiteSpace(stringTable.GetEntry(t.Key)?.LocalizedValue)) {
                    return true;
                }
            }

            return false;
        }

        bool ShouldBeExported(StringTableEntry t) {
            if (t.Key.Contains("Debug") || t.SharedEntry.Metadata.GetMetadata<DebugMarkerMeta>() != null) {
                return false;
            }

            string guid = LocalizationCleanupTools.ExtractGuid(t);
            if (guid == null) {
                GUIDCache.Load();
                var usages = GUIDCache.Instance.GetIdOverrideUsages(t.Key).ToList();
                if (usages.Count == 1) {
                    guid = AssetDatabase.AssetPathToGUID(usages[0]);
                }
            }

            if (guid != null) {
                var flag = s_regionFilterCache[guid];

                Template template = TemplatesUtil.Load<Template>(guid);
                bool templateIsDebugOrForRemoval = template?.TemplateType is TemplateType.Debug or TemplateType.ForRemoval;
                if (templateIsDebugOrForRemoval && _templateTypesFilter == TemplateTypesExportFilter.RemoveDebug) {
                    // With this filter we remove all Debug and ForRemoval templates
                    return false;
                }

                bool shouldBeExcluded = _excludeRegionFilter.HasCommonBitsFast(flag);
                if (shouldBeExcluded) {
                    return false;
                }

                bool regionFilterOnlyKnown = (_regionFilter & ~ExportRegionFilter.Unknown).HasCommonBitsFast(flag);
                if (regionFilterOnlyKnown) {
                    // This asset is for sure placed in regions we want
                    return true;
                }

                bool checkRegionFilter = _regionFilter.HasCommonBitsFast(flag);
                if (!checkRegionFilter) {
                    // This asset for sure (except for holes in caches) doesn't belong to regions we want
                    return false;
                }

                // There are only Unknown assets when we get here
                return _templateTypesFilter == TemplateTypesExportFilter.ExportAll || !templateIsDebugOrForRemoval;
            } else {
                // ID doesn't contain guid, so we can't determine its region location
                return _regionFilter.HasFlagFast(ExportRegionFilter.Unknown);
            }
        }

        static bool IsTranslationMissing(DataToExport d) {
            return !d.IsTranslated || string.IsNullOrWhiteSpace(d.translation) || d.translationHash != d.Source.Trim().GetHashCode();
        }
        
        void ExportStoryToCSV() {
            s_selectedSource = 0;
            string mainPath = EditorUtility.SaveFolderPanel("Choose save file location", _lastSavedSelectedPath, "");
            ClearCurrentData();
            PopulateStoryData();
            ExportCSV(true, mainPath);
        }
        
        async UniTaskVoid ExportToGoogleSheet(Dictionary<Locale, Dictionary<string, List<DataToExport>>> getSpreadsheetData, bool isStory) {
            var progressBar = ProgressBar.Create("Google sheets export", null, true);
            string completeMessage = "Export to Google Sheets completed";

            // if (!ARGoogleSheets.Initialized) {
            //     progressBar.Display(0, "Authorizing...");
            //     ARGoogleSheets.Initialize();
            // }

            progressBar.Display(0, "Fetching data...");
            await UniTask.DelayFrame(1);

            var spreadSheetData = getSpreadsheetData.ToArray();

            string[] spreadSheetsUrls = new string[spreadSheetData.Length];
            for (int i = 0; i < spreadSheetData.Length; i++) {
                var localeData = spreadSheetData[i];
                // var spreadsheet = ARGoogleSheets.CreateSpreadsheetForLocalization(localeData.Key.LocaleName, localeData.Value, isStory);
                // if (progressBar.DisplayCancellable((i + 1) / (float)spreadSheetData.Length, $"Exporting: {localeData.Key.LocaleName} ")) {
                //     completeMessage = "Export to Google Sheets canceled, exported data may be incomplete.";
                //     break;
                // }

                // while (spreadsheet.IsCompleted == false) {
                //     await UniTask.DelayFrame(1);
                // }

                // spreadSheetsUrls[i] = spreadsheet.Result.SpreadsheetUrl;
            }

            EditorUtility.ClearProgressBar();

            if (EditorUtility.DisplayDialog("Export to Google Sheets", completeMessage, "Open Google Sheets", "Close")) {
                foreach (var url in spreadSheetsUrls) {
                    Application.OpenURL(url);
                }
            }
        }

        // === Populating Data Operations
        void PopulateStoryData() {
            if (_selectBy == SelectionType.Directory) {
                foreach (var path in directoryPaths) {
                    string[] guids = AssetDatabase.FindAssets(null, new[] { PathUtils.FilesystemToAssetPath($"{_applicationDataPathWithoutAssets}{path}") });
                    PopulateStoryData(guids, false);
                }
            } else {
                string[] guids = storyGraphs.Select(graph => {
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(graph, out string guid, out long _);
                    return guid;
                }).ToArray();
                PopulateStoryData(guids, false);
            }
        }

        void PopulateStoryData(string[] guids, bool replaceCurrentData = true) {
            if (replaceCurrentData) {
                ClearCurrentData();
            } else {
                guids = guids.Where(g => !_currentData.Any(d => d.ID.Contains(g))).ToArray();
                if (!guids.Any()) {
                    return;
                }
            }

            StringTable stringTable = (StringTable)CurrentSource.GetTable(LocalizationSettings.ProjectLocale.Identifier);
            List<ActorTermMap> termsWithActors = CollectActorsForTerms(guids, (StringTable)CurrentSource.GetTable(LocalizationSettings.ProjectLocale.Identifier));

            HashSet<string> globalValidKeys = new(100);
            HashSet<string> globalAddedKeys = new(100);
            List<PopulateGraphCache> graphCache = new(200);

            foreach (string guid in guids) {
                var storyGraph = AssetDatabase.LoadAssetAtPath<StoryGraph>(AssetDatabase.GUIDToAssetPath(guid));
                if (storyGraph == null) {
                    continue;
                }
                
                var templateType = ((ITemplate)storyGraph).TemplateType;
                if (!_includeDebugGraphs && templateType is TemplateType.Debug or TemplateType.ForRemoval) {
                    continue;
                }
                
                var scriptTexts = StoryScriptExporter.EnumerateOnlyOneStoryGraph(storyGraph, ScriptType.Texts);
                var validKeysList = stringTable.Values
                    .Where(t => t.Key != null && t.Key.Contains(guid) && !string.IsNullOrWhiteSpace(t.LocalizedValue) && !t.LocalizedValue.Contains(t.Key))
                    .Select(v => v.Key).ToList();
                globalValidKeys.AddRange(validKeysList);

                graphCache.Add(new PopulateGraphCache {
                    guid = guid,
                    texts = scriptTexts.ToList(),
                    validKeys = validKeysList,
                });
            }

            foreach (var data in graphCache.OrderByDescending(d => d.texts.Count)) {
                foreach (var text in data.texts) {
                    if (globalValidKeys.Contains(text.id) && globalAddedKeys.Add(text.id)) {
                        var actor = termsWithActors.FirstOrDefault(a => a.term == text.id).actor;
                        _currentData.Add(new DataToExport(new(text.id), actor, text.previousLine));
                    }
                }

                foreach (var key in data.validKeys) {
                    if (globalAddedKeys.Add(key)) {
                        var actor = termsWithActors.FirstOrDefault(a => a.term == key).actor;
                        _currentData.Add(new DataToExport(new(key), actor));
                    }
                }
            }

            PopulateAllLanguages();
        }

        void ClearCurrentData() {
            _dataToExport.Clear();
            _currentData.Clear();
        }

        void PopulateAllLanguages() {
            foreach (Locale locale in Locales) {
                if (locale == LocalizationSettings.ProjectLocale) {
                    _dataToExport[locale] = _currentData;
                } else {
                    _dataToExport[locale] = _currentData.ConvertAll(d => new DataToExport(d)).ToList();
                }

                foreach (var d in _dataToExport[locale]) {
                    d.translation = ((StringTable)CurrentSource.GetTable(locale.Identifier))
                        .GetEntry(d.ID)?
                        .LocalizedValue ?? string.Empty;
                    d.SetLanguage(locale);
                    d.SetGestureAndCategory();
                }
            }
        }

        void UpdateDataForSelectedSource(int selectedSourceIndex) {
            s_selectedSource = selectedSourceIndex;
            ClearCurrentData();
            
            _currentData.AddRange(
                ((StringTable)CurrentSource.GetTable(LocalizationSettings.ProjectLocale.Identifier)).Values
                .Where(t => HasAnyTranslation(t) && ShouldBeExported(t))
                .Select(entry => new DataToExport(new(entry.Key), string.Empty)));
            PopulateAllLanguages();
        }

        void ExportCSV(bool isStory, string pathToSave = null) {
            if (string.IsNullOrEmpty(pathToSave)) {
                pathToSave = EditorUtility.SaveFolderPanel("Choose save file location", _lastSavedSelectedPath, "");
            }

            _lastSavedSelectedPath = pathToSave;

            foreach (var data in _dataToExport) {
                if (!_languagesToExport.Contains(data.Key.LocaleName)) {
                    continue;
                }

                string pathForLocale = $"{pathToSave}/{data.Key.LocaleName}";
                Directory.CreateDirectory(pathForLocale);
                ExportLanguageCSV(data, pathForLocale, isStory);
            }

            UpdateAllSources();
        }

        void ExportLanguageCSV(KeyValuePair<Locale, List<DataToExport>> data, string pathToSave, bool isStory) {
            var dataToExport = _exportFilters.HasFlagFast(ExportOptions.Missing) ? data.Value.Where(IsTranslationMissing).ToList() : data.Value;
            dataToExport = CurrentSource.name == LocTermsTableName
                ? LocalizationExporterUtilities.GetDataForExportUngrouped(dataToExport)
                : LocalizationExporterUtilities.GetDataForExportGrouped(dataToExport);

            if (_exportOnlyUngroupedEntries) {
                List<DataToExport> ungroupedData = new();
                foreach (var d in dataToExport) {
                    if (d.groupedTermsData.Count > 1) {
                        foreach (var gd in d.groupedTermsData) {
                            var originalData = data.Value.First(dte => dte.ID == gd.id);
                            var newD = new DataToExport(originalData);
                            newD.groupedTermsData = new List<LocTermData> { gd };
                            ungroupedData.Add(newD);
                        }
                    }
                }

                dataToExport = ungroupedData;
            }

            dataToExport = _exportFilters.HasFlagFast(ExportOptions.RedactedInEnglish) ? dataToExport.Where(d=>d.IsSourceRedacted()).ToList() : dataToExport;
            dataToExport = _exportFilters.HasFlagFast(ExportOptions.Proofread) ? dataToExport.Where(d=>d.IsProofRead(data.Key)).ToList() : dataToExport;
            dataToExport = _exportFilters.HasFlagFast(ExportOptions.WithoutProofread) ? dataToExport.Where(d=>!d.IsProofRead(data.Key)).ToList() : dataToExport;
            
            string suffix = _exportFilters != ExportOptions.None ? _exportFilters.ToString().Replace(", ","_") : string.Empty;
            LocalizationExporterUtilities.ExportCsv(pathToSave, $"{s_source.name}_{data.Key.LocaleName}{suffix}", dataToExport, isStory);
        }

        void ExportOtherToCSV() {
            string mainPath = EditorUtility.SaveFolderPanel("Choose save file location", _lastSavedSelectedPath, "");
            // Start with 1 to skip story (we never want to export all story graphs)
            for (int i = 1; i < Sources.Length; i++) {
                UpdateDataForSelectedSource(i);
                Directory.CreateDirectory(mainPath);
                ExportCSV(false, mainPath);
            }
        }
        
        // --- Helper methods
        Dictionary<Locale, Dictionary<string, List<DataToExport>>> GetSpreadsheetDataForStory() {
            ClearCurrentData();
            PopulateStoryData();
            
            var spreadSheetData = new Dictionary<Locale, Dictionary<string, List<DataToExport>>>();
            foreach (var data in _dataToExport) {
                if (!_languagesToExport.Contains(data.Key.LocaleName)) {
                    continue;
                }

                var dataToGroup = _exportFilters.HasFlagFast(ExportOptions.Missing) ? data.Value.Where(IsTranslationMissing).ToList() : data.Value;
                List<DataToExport> groupedData = LocalizationExporterUtilities.GetDataForExportGrouped(dataToGroup);
                
                groupedData = _exportFilters.HasFlagFast(ExportOptions.RedactedInEnglish) ? groupedData.Where(d=>d.IsSourceRedacted()).ToList() : groupedData;
                groupedData = _exportFilters.HasFlagFast(ExportOptions.Proofread) ? groupedData.Where(d=>d.IsProofRead(data.Key)).ToList() : groupedData;
                groupedData = _exportFilters.HasFlagFast(ExportOptions.WithoutProofread) ? groupedData.Where(d=>!d.IsProofRead(data.Key)).ToList() : groupedData;
                
                if (!groupedData.Any()) {
                    continue;
                }

                spreadSheetData[data.Key] = new Dictionary<string, List<DataToExport>> { { data.Key.LocaleName, groupedData } };
            }

            return spreadSheetData;
        }

        Dictionary<Locale, Dictionary<string, List<DataToExport>>> GetSpreadsheetDataForOther() {
            Dictionary<Locale, Dictionary<string, List<DataToExport>>> allData = new();
            for (int i = 1; i < Sources.Length; i++) {
                UpdateDataForSelectedSource(i);

                foreach (var pair in _dataToExport) {
                    var localeName = pair.Key.LocaleName;
                    
                    if (!_languagesToExport.Contains(localeName)) {
                        continue;
                    }

                    var dataToExport = _exportFilters.HasFlagFast(ExportOptions.Missing) ? pair.Value.Where(IsTranslationMissing).ToList() : pair.Value;
                    dataToExport = CurrentSource.name == LocTermsTableName
                        ? LocalizationExporterUtilities.GetDataForExportUngrouped(dataToExport)
                        : LocalizationExporterUtilities.GetDataForExportGrouped(dataToExport);
                    dataToExport = _exportFilters.HasFlagFast(ExportOptions.RedactedInEnglish) ? dataToExport.Where(d=>d.IsSourceRedacted()).ToList() : dataToExport;
                    dataToExport = _exportFilters.HasFlagFast(ExportOptions.Proofread) ? dataToExport.Where(d=>d.IsProofRead(pair.Key)).ToList() : dataToExport;
                    dataToExport = _exportFilters.HasFlagFast(ExportOptions.WithoutProofread) ? dataToExport.Where(d=>!d.IsProofRead(pair.Key)).ToList() : dataToExport;
                    
                    if (!dataToExport.Any()) continue;

                    if (!allData.ContainsKey(pair.Key)) {
                        allData.Add(pair.Key, new Dictionary<string, List<DataToExport>>());
                    }

                    allData[pair.Key][CurrentSource.name] = dataToExport;
                }
            }

            return allData;
        }

        // === Odin
        [UsedImplicitly]
        string GetExportOptionsTooltip() {
            var tooltip = new StringBuilder("Currently selected:\n");

            // Highlight selected options first
            if (_exportFilters == ExportOptions.None) {
                tooltip.Append(" - None (No filtering applied)\n");
            } else {
                if (_exportFilters.HasFlagFast(ExportOptions.Missing))
                    tooltip.Append(" - ✅ Missing: Only exports missing translations.\n");

                if (_exportFilters.HasFlagFast(ExportOptions.RedactedInEnglish))
                    tooltip.Append(" - ✅ RedactedInEnglish: Exports translations with a final/redacted English source.\n");

                if (_exportFilters.HasFlagFast(ExportOptions.Proofread))
                    tooltip.Append(" - ✅ Proofread: Exports only proofread translations.\n");

                if (_exportFilters.HasFlagFast(ExportOptions.WithoutProofread))
                    tooltip.Append(" - ✅ WithoutProofread: Exports only translations that are not proofread.\n");
            }

            tooltip.Append("\nAvailable options:\n");
            tooltip.Append(" - Missing: Only exports missing translations.\n");
            tooltip.Append(" - RedactedInEnglish: Exports translations with a final/redacted English source.\n");
            tooltip.Append(" - Proofread: Exports only proofread translations.\n");
            tooltip.Append(" - WithoutProofread: Exports translations that are not proofread.\n");

            return tooltip.ToString().TrimEnd();
        }
    }
    
    class PopulateGraphCache {
        public string guid;
        public List<ScriptEntry> texts = new(200);
        public List<string> validKeys = new(200);
    }
    
    [Flags]
    enum ExportOptions : byte {    
        None = 0,
        Missing = 1 << 0,             // 0001
        RedactedInEnglish = 1 << 1,   // 0010
        Proofread = 1 << 2,           // 0100
        WithoutProofread = 1 << 3     // 1000
    }
}