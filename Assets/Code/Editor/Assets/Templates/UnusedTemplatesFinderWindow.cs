using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Awaken.TG.Editor.Utility.Assets;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Assets.Templates {
    public class UnusedTemplatesFinderWindow : OdinEditorWindow {
        [InfoBox("1. FIRST! Click 'Scan All Templates' to analyze usage\n" +
                 "2. Filter by template type (C# class), TemplateType (Regular/System/Debug/ForRemoval), or Usage Type (Scene/Prefab/Story Graph/etc.)\n" +
                 "3. Use 'Scene Filter' to find templates used ONLY in selected scenes\n" +
                 "4. Select templates and delete them if unnecessary")]
        [PropertyOrder(int.MinValue), Button(ButtonSizes.Large), GUIColor(0.3f, 0.8f, 1f)]
        public void ScanAllTemplates() {
            Scan();
        }

        [TitleGroup("Scanned Templates")]
        [HorizontalGroup("Scanned Templates/Input", Gap = 20, Width = 0.6f)]
        [FoldoutGroup("Scanned Templates/Input/Filters")]
        [ShowInInspector, ReadOnly, GUIColor(0.8f, 0.8f, 1f)]
        int _filteredCount;

        [FoldoutGroup("Scanned Templates/Input/Filters", Expanded = true), PropertySpace]
        [EnumToggleButtons]
        [OnValueChanged(nameof(ApplyFilters))]
        public UsageFilterMode usageFilter = UsageFilterMode.UnusedOnly;
        [FoldoutGroup("Scanned Templates/Input/Filters")]
        [EnumToggleButtons, LabelText("Template Type")]
        [OnValueChanged(nameof(ApplyFilters))]
        public TemplateTypeFlag templateTypeFilterMode = TemplateTypeFlag.All;
        [FoldoutGroup("Scanned Templates/Input/Filters")]
        [EnumToggleButtons, LabelText("Usage Type"), DisableIf(nameof(usageFilter), UsageFilterMode.UnusedOnly)]
        [OnValueChanged(nameof(ApplyFilters))]
        public UsageTypeFilter usageTypeFilter = UsageTypeFilter.All;

        [FoldoutGroup("Scanned Templates/Input/Filters")]
        [ListDrawerSettings(DraggableItems = true, ShowPaging = false, ShowFoldout = true), PropertySpace]
        // [ValueDropdown(nameof(GetTemplateTypes))]
        [OnValueChanged(nameof(ApplyFilters))]
        [InlineButton(nameof(ClearTemplateFilter), SdfIconType.ArrowCounterclockwise, "Clear Filter")]
        public List<Type> templateTypeFilter = new();

        [FoldoutGroup("Scanned Templates/Input/Filters")]
        [ListDrawerSettings(DraggableItems = true, ShowPaging = false, ShowFoldout = true)]
        [LabelText("Scene Filter (templates used ONLY in these scenes)")]
        [OnValueChanged(nameof(ApplyFilters))]
        [InlineButton(nameof(ClearSceneFilter), SdfIconType.ArrowCounterclockwise, "Clear Filter")]
        public List<SceneAsset> sceneFilter = new();

        [FoldoutGroup("Scanned Templates/Input/Summary")]
        [ShowInInspector, ReadOnly, LabelText("Templates Scanned")]
        int _totalScanned;
        [FoldoutGroup("Scanned Templates/Input/Summary")]
        [ShowInInspector, ReadOnly, LabelText("Unused Templates"), GUIColor(0.8f, 0.8f, 1f)]
        int _totalUnused;

        [FoldoutGroup("Scanned Templates/Input/Summary"), PropertySpace]
        [ShowInInspector, ReadOnly, TableList(AlwaysExpanded = true, HideToolbar = true)]
        List<TypeSummary> _typeSummary = new();

        [TitleGroup("Results")]
        [HorizontalGroup("Results/Output", Gap = 20, Width = 0.75f)]
        [ShowInInspector, TableList(IsReadOnly = true, ShowPaging = true, NumberOfItemsPerPage = 10, AlwaysExpanded = true)]
        public List<TemplateResultEntry> filteredResults = new();

        readonly List<TemplateResultEntry> _allResults = new();
        static readonly Dictionary<string, string> AssetTypeCache = new();

        [MenuItem("TG/Design/Unused Templates Finder")]
        public static void ShowWindow() {
            var window = GetWindow<UnusedTemplatesFinderWindow>();
            window.titleContent = new GUIContent("Unused Templates Finder");
            window.minSize = new Vector2(1000, 600);
            window.Show();
        }

        [FoldoutGroup("Results/Output/Bulk Actions", Expanded = true)]
        [HorizontalGroup("Results/Output/Bulk Actions/Select")]
        [Button("Select All", Icon = SdfIconType.CheckSquareFill)]
        void SelectAllFiltered() {
            SetSelection(true);
        }

        [FoldoutGroup("Results/Output/Bulk Actions")]
        [HorizontalGroup("Results/Output/Bulk Actions/Select")]
        [Button("Deselect All", Icon = SdfIconType.XSquareFill)]
        void DeselectAllFiltered() {
            SetSelection(false);
        }

        void SetSelection(bool select) {
            if (filteredResults.Count == 0) {
                return;
            }

            for (int i = 0; i < filteredResults.Count; i++) {
                var entry = filteredResults[i];
                if (entry.template != null) {
                    entry.isSelected = select;
                }
            }
        }

        [FoldoutGroup("Results/Output/Bulk Actions")]
        [Button("Export Filtered List to CSV", Icon = SdfIconType.ListColumnsReverse)]
        void ExportToCSV() {
            var path = EditorUtility.SaveFilePanel("Export Template List", "Assets", "UnusedTemplates", "csv");
            if (string.IsNullOrEmpty(path)) {
                return;
            }

            var csv = new StringBuilder();
            csv.AppendLine("Template Name,Type,Template Type,GUID,Usage Count,Asset Path,Used In Scenes,Used In Prefabs,Used In Story Graphs,Used In Loot Tables,Used In Other");

            foreach (var entry in filteredResults) {
                csv.AppendLine(entry.ToCsvLine());
            }

            File.WriteAllText(path, csv.ToString());
            Log.Important?.Info($"Exported {filteredResults.Count} templates to {path}");
            EditorUtility.RevealInFinder(path);
        }

        [FoldoutGroup("Results/Output/Bulk Actions")]
        [Button("Delete Selected Templates", Icon = SdfIconType.TrashFill), GUIColor(1f, 0.5f, 0.5f)]
        void DeleteSelectedTemplates() {
            var selected = filteredResults.Where(r => r.isSelected && r.template != null).ToList();
            if (selected.Count == 0) {
                EditorUtility.DisplayDialog("No Selection", "Please check the boxes next to templates you want to delete.", "OK");
                return;
            }

            var message = $"Are you sure you want to delete {selected.Count} templates?\n\nThis action cannot be undone!\n\nTemplates to delete:\n";
            message += string.Join("\n", selected.Take(10).Select(s => s.template.name));
            if (selected.Count > 10) {
                message += $"\n... and {selected.Count - 10} more";
            }

            if (EditorUtility.DisplayDialog("Confirm Delete", message, "Delete", "Cancel")) {
                int deletedCount = 0;
                foreach (var entry in selected) {
                    var assetPath = AssetDatabase.GetAssetPath(entry.template);
                    if (!string.IsNullOrEmpty(assetPath) && AssetDatabase.DeleteAsset(assetPath)) {
                        deletedCount++;
                    }
                }
                Log.Important?.Info($"Deleted {deletedCount} templates");
                AssetDatabase.Refresh();
                Scan();
            }
        }

        void Scan() {
            try {
                TemplatesSearcher.EnsureInit();

                _allResults.Clear();
                filteredResults.Clear();
                AssetTypeCache.Clear();

                EditorUtility.DisplayProgressBar("Scanning Templates", "Loading dependency cache...", 0.3f);
                var dependencies = AssetDependencyAnalyzer.AnalyzeDependenciesFromCache();

                EditorUtility.DisplayProgressBar("Scanning Templates", "Processing templates...", 0.7f);

                var allTemplates = TemplatesProvider.EditorGetAllOfType<ITemplate>();
                _totalScanned = 0;

                foreach (var template in allTemplates) {
                    _totalScanned++;

                    var dependents = dependencies.GetDependents(template.GUID);
                    var templatePath = GetTemplatePath(template);

                    var entry = new TemplateResultEntry {
                        guid = template.GUID,
                        template = template as Template,
                        templateName = template.DebugName,
                        templateTypeName = template.GetType().Name,
                        templateType = template.GetType(),
                        templateTypeEnum = template.TemplateType,
                        templateAssetPath = templatePath,
                        usedInScenes = FilterAssetsByExtension(dependents, ".unity"),
                        usedInPrefabs = FilterAssetsByExtension(dependents, ".prefab"),
                        usedInStoryGraphs = FilterAssetsByType(dependents, nameof(StoryGraph)),
                        usedInLootTables = FilterAssetsByType(dependents, nameof(LootTableAsset)),
                        usedInOther = FilterOtherAssets(dependents)
                    };

                    entry.allUsageCount = entry.usedInScenes.Count + entry.usedInPrefabs.Count +
                                         entry.usedInStoryGraphs.Count + entry.usedInLootTables.Count + entry.usedInOther.Count;
                    entry.OnDeleted += OnTemplateDeleted;
                    _allResults.Add(entry);
                }

                EditorUtility.ClearProgressBar();
                CalculateSummary();
                ApplyFilters();
            } catch (Exception e) {
                EditorUtility.ClearProgressBar();
                Log.Critical?.Error($"Failed to scan templates: {e.Message}");
            }
        }

        void OnTemplateDeleted(TemplateResultEntry entry) {
            entry.OnDeleted -= OnTemplateDeleted;
            ApplyFilters();
        }

        void CalculateSummary() {
            _totalUnused = 0;
            var typeGroups = new Dictionary<Type, int>();

            foreach (var result in _allResults) {
                if (result.allUsageCount == 0) {
                    _totalUnused++;
                    var type = result.templateType ?? typeof(object);
                    typeGroups[type] = typeGroups.GetValueOrDefault(type, 0) + 1;
                }
            }

            _typeSummary.Clear();
            foreach (var group in typeGroups.OrderByDescending(g => g.Value).ThenBy(g => g.Key?.Name ?? "Unknown")) {
                _typeSummary.Add(new TypeSummary {
                    typeName = group.Key?.Name ?? "Unknown",
                    unusedCount = group.Value
                });
            }
        }

        void ApplyFilters() {
            SetSelection(false);
            filteredResults.Clear();

            var results = _allResults.AsEnumerable();

            results = usageFilter switch {
                UsageFilterMode.UnusedOnly => results.Where(r => r.allUsageCount == 0),
                UsageFilterMode.UsedOnly => results.Where(r => r.allUsageCount > 0),
                _ => results
            };

            if (templateTypeFilter is { Count: > 0 }) {
                results = results.Where(r => templateTypeFilter.Any(t => t != null && t.IsAssignableFrom(r.templateType)));
            }

            if (templateTypeFilterMode != TemplateTypeFlag.All) {
                results = results.Where(r => templateTypeFilterMode.Contains(r.templateTypeEnum));
            }

            if (usageTypeFilter != UsageTypeFilter.All) {
                results = results.Where(r => {
                    var hasScene = (usageTypeFilter & UsageTypeFilter.Scene) != 0 && r.usedInScenes.Count > 0;
                    var hasPrefab = (usageTypeFilter & UsageTypeFilter.Prefab) != 0 && r.usedInPrefabs.Count > 0;
                    var hasStory = (usageTypeFilter & UsageTypeFilter.StoryGraph) != 0 && r.usedInStoryGraphs.Count > 0;
                    var hasLoot = (usageTypeFilter & UsageTypeFilter.LootTable) != 0 && r.usedInLootTables.Count > 0;
                    var hasOther = (usageTypeFilter & UsageTypeFilter.Other) != 0 && r.usedInOther.Count > 0;
                    return hasScene || hasPrefab || hasStory || hasLoot || hasOther;
                });
            }

            if (sceneFilter is { Count: > 0 }) {
                var sceneNames = sceneFilter.Where(s => s != null).Select(s => s.name).ToHashSet();
                results = results.Where(r => {
                    if (!r.UsedOnlyInScenes) {
                        return false;
                    }
                    return r.usedInScenes.All(s => sceneNames.Contains(Path.GetFileNameWithoutExtension(s.path)));
                });
            }

            filteredResults = results.ToList();
            filteredResults.Sort((a, b) => {
                var usageComp = a.allUsageCount.CompareTo(b.allUsageCount);
                if (usageComp != 0) {
                    return usageComp;
                }

                var typeComp = string.Compare(a.templateType?.Name ?? "Unknown", b.templateType?.Name ?? "Unknown", StringComparison.Ordinal);
                if (typeComp != 0) {
                    return typeComp;
                }

                var nameA = a.template?.name ?? a.templateAssetPath;
                var nameB = b.template?.name ?? b.templateAssetPath;
                return string.Compare(nameA, nameB, StringComparison.Ordinal);
            });

            _filteredCount = filteredResults.Count;
        }

        static string GetTemplatePath(ITemplate template) {
            return template switch {
                Component comp => AssetDatabase.GetAssetPath(comp.gameObject),
                ScriptableObject so => AssetDatabase.GetAssetPath(so),
                _ => AssetDatabase.GUIDToAssetPath(template.GUID)
            };
        }

        static string GetAssetTypeName(string assetPath) {
            if (AssetTypeCache.TryGetValue(assetPath, out var cachedType)) {
                return cachedType;
            }

            var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            var typeName = asset?.GetType().Name ?? "Unknown";
            AssetTypeCache[assetPath] = typeName;
            return typeName;
        }

        static List<UsedAssetRef> FilterAssetsByExtension(IEnumerable<string> paths, string extension) {
            return paths
                .Where(p => p.EndsWith(extension))
                .Distinct()
                .OrderBy(p => p)
                .Select(p => new UsedAssetRef(p))
                .ToList();
        }

        static List<UsedAssetRef> FilterAssetsByType(IEnumerable<string> paths, string typeName) {
            return paths
                .Where(p => !p.EndsWith(".unity") && !p.EndsWith(".prefab") && GetAssetTypeName(p) == typeName)
                .Distinct()
                .OrderBy(p => p)
                .Select(p => new UsedAssetRef(p))
                .ToList();
        }

        static List<UsedAssetRef> FilterOtherAssets(IEnumerable<string> paths) {
            return paths
                .Where(p => {
                    if (p.EndsWith(".unity") || p.EndsWith(".prefab")) {
                        return false;
                    }
                    var typeName = GetAssetTypeName(p);
                    return typeName != nameof(StoryGraph) && typeName != nameof(LootTableAsset);
                })
                .Distinct()
                .OrderBy(p => p)
                .Select(p => new UsedAssetRef(p))
                .ToList();
        }

        // IEnumerable<ValueDropdownItem<Type>> GetTemplateTypes() {
        //     yield return new ValueDropdownItem<Type>("All", null);
        //     var types = typeof(ITemplate).Assembly.GetTypes()
        //         .Where(t => typeof(ITemplate).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
        //         .OrderBy(t => t.Name);
        //     foreach (var type in types) {
        //         yield return new ValueDropdownItem<Type>(type.Name, type);
        //     }
        // }

        void ClearSceneFilter() {
            sceneFilter.Clear();
            ApplyFilters();
        }

        void ClearTemplateFilter() {
            templateTypeFilter.Clear();
            ApplyFilters();
        }

        public enum UsageFilterMode {
            All,
            UnusedOnly,
            UsedOnly
        }

        [Flags]
        public enum UsageTypeFilter {
            Scene = 1 << 0,
            Prefab = 1 << 1,
            StoryGraph = 1 << 2,
            LootTable = 1 << 3,
            Other = 1 << 4,
            All = Scene | Prefab | StoryGraph | LootTable | Other
        }

        [Serializable]
        public class TypeSummary {
            [ShowInInspector, ReadOnly, TableColumnWidth(200)]
            public string typeName;
            [ShowInInspector, ReadOnly, TableColumnWidth(40)]
            public int unusedCount;
        }
    }
}