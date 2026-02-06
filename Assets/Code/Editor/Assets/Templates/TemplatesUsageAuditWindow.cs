using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Debugging.GUIDSearching;
using Awaken.TG.Main.Templates;
using Awaken.TG.Assets;
using Awaken.TG.Main.General.Caches;
using Awaken.TG.Main.Heroes.Items;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Awaken.TG.Editor.Assets.Templates {
    public class TemplatesUsageAuditWindow : OdinEditorWindow {
        [PropertyOrder(-10)]
        [ShowInInspector, ReadOnly]
        int _foundCount;

        [FormerlySerializedAs("Results")]
        [ShowInInspector]
        [PropertyOrder(-9)]
        [InfoBox("Click 'Scan' to search for templates marked as Debug/ForRemoval that are still used in project.")]
        [ListDrawerSettings(DraggableItems = false, IsReadOnly = true, HideAddButton = true, HideRemoveButton = true, ShowPaging = true, NumberOfItemsPerPage = 20)]
        public List<ResultEntry> results = new();

        [PropertyOrder(-11)]
        [ShowInInspector, EnumToggleButtons]
        [OnValueChanged(nameof(OnFilterModeChanged))]
        public FilterMode filterMode = FilterMode.ShowAll;

        public enum FilterMode {
            ShowAll,
            OnlyIncludedInBuild,
            OnlyUsedInGame
        }

        [MenuItem("TG/Design/Used Templates Marked Wrong (Window)")]
        public static void ShowWindow() {
            var window = GetWindow<TemplatesUsageAuditWindow>();
            window.titleContent = new GUIContent("Templates Usage Audit");
            window.minSize = new Vector2(600, 300);
            window.Show();
        }

        [Button(ButtonSizes.Large), PropertyOrder(-20)]
        public void Scan() {
            try {
                TemplatesSearcher.EnsureInit();
                GUIDCache.Load();
                ARAssetReference.EditorAssignUnusedGuids(GUIDCache.Instance.UnusedCache);

                results.Clear();

                foreach (var template in TemplatesProvider.EditorGetAllOfType<Template>()) {
                    if (template.TemplateType is TemplateType.Debug or TemplateType.ForRemoval) {
                        // Consider only templates actually used
                        if (template.templateType == TemplateType.ForRemoval || ARAssetReference.EditorIsUsed(template.GUID)) {
                            var dependent = GUIDCache.Instance.GetDependent(template, true);
                            var path = AssetDatabase.GetAssetPath(template);

                            results.Add(new ResultEntry {
                                guid = template.GUID,
                                template = template,
                                templateType = template.TemplateType,
                                templateAssetPath = string.IsNullOrEmpty(path) ? AssetDatabase.GUIDToAssetPath(template.GUID) : path,
                                dependentAssets = dependent.Select(p => p.Replace('\\', '/')).Distinct().OrderBy(p => p)
                                    .Select(p => new DependentEntry { path = p }).ToList(),
                                isInGameCache = template is ItemTemplate itemTemplate ? ItemsInGameCache.Get.Editor_HasAnyOccurrencesOf(itemTemplate) : null,
                            });
                        }
                    }
                }

                var filteredResults = results;
                switch (filterMode) {
                    case FilterMode.OnlyIncludedInBuild:
                        filteredResults = results.Where(r => ARAssetReference.EditorIsUsed(r.guid)).ToList();
                        break;
                    case FilterMode.OnlyUsedInGame:
                        filteredResults = results.Where(r => ARAssetReference.EditorIsUsed(r.guid) && (r.isInGameCache == null || r.isInGameCache == true)).ToList();
                        break;
                    case FilterMode.ShowAll:
                    default:
                        break;
                }
                results = filteredResults
                    .OrderBy(r => r.UsageCount)
                    .ThenBy(r => r.template != null ? r.template.name : r.templateAssetPath)
                    .ToList();

                _foundCount = results.Count;
            }
            catch (Exception e) {
                Debug.LogException(e);
            }
        }

        void OnFilterModeChanged() {
            Scan();
        }

        [Serializable]
        public class DependentEntry {
            [FormerlySerializedAs("Path")] [ShowInInspector, ReadOnly]
            public string path;

            [HorizontalGroup("DependentActions"), Button, GUIColor(0.7f, 1f, 0.7f)]
            public void Ping() {
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (obj) {
                    EditorGUIUtility.PingObject(obj);
                }
            }

            [HorizontalGroup("DependentActions"), Button]
            public void Select() {
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (obj) {
                    Selection.activeObject = obj;
                }
            }

            [HorizontalGroup("DependentActions"), Button]
            public void CopyPath() {
                if (!string.IsNullOrEmpty(path)) {
                    EditorGUIUtility.systemCopyBuffer = path.Replace('\\', '/');
                }
            }
        }

        [Serializable]
        public class ResultEntry {
            [HideInInspector]
            public string guid;
            [HideInInspector]
            public string templateAssetPath;

            [ShowInInspector, ReadOnly]
            public Template template;

            [ShowInInspector, ReadOnly]
            public TemplateType templateType;

            [ShowInInspector, ReadOnly]
            public int UsageCount => dependentAssets?.Count ?? 0;

            [ReadOnly]
            public bool? isInGameCache;

            public bool ShowInGameCacheWarning => isInGameCache.HasValue && !isInGameCache.Value;

            [ShowInInspector, ShowIf(nameof(ShowInGameCacheWarning)), GUIColor(0.87f, 0.5f, 0.06f)]
            public string InGameCacheWarning => "Item is not in ItemsInGameCache, which gives 98% that it's actually not used in the game" +
                                                " (and can be deleted, together with dependent assets)."; 

            [FormerlySerializedAs("DependentAssets")]
            [ShowInInspector]
            [ListDrawerSettings(DraggableItems = false, ShowPaging = false)]
            public List<DependentEntry> dependentAssets = new();

            [HorizontalGroup("RowActions"), Button(ButtonSizes.Medium), GUIColor(0.7f, 0.9f, 1f)]
            public void PingTemplate() {
                if (template != null) {
                    EditorGUIUtility.PingObject(template);
                } else if (!string.IsNullOrEmpty(templateAssetPath)) {
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(templateAssetPath);
                    if (obj) {
                        EditorGUIUtility.PingObject(obj);
                    }
                }
            }

            [HorizontalGroup("RowActions"), Button(ButtonSizes.Medium)]
            public void SelectTemplate() {
                if (template != null) {
                    Selection.activeObject = template;
                } else if (!string.IsNullOrEmpty(templateAssetPath)) {
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(templateAssetPath);
                    if (obj) {
                        Selection.activeObject = obj;
                    }
                }
            }

            [HorizontalGroup("RowActions"), Button(ButtonSizes.Medium)]
            public void CopyTemplatePath() {
                if (!string.IsNullOrEmpty(templateAssetPath)) {
                    EditorGUIUtility.systemCopyBuffer = templateAssetPath.Replace('\\', '/');
                }
            }
        }
    }
}
