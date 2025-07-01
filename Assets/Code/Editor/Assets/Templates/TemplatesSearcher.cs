using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Templates;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;

namespace Awaken.TG.Editor.Assets.Templates {
    public static class TemplatesSearcher {
        public static readonly AddressableAssetGroup[] Groups = TemplatesToAddressablesMapping.ValidAddressableGroups.Select(AddressableHelper.FindGroup).ToArray();
        public static AddressableAssetGroup GroupFor(object obj) {
            var groupName = TemplatesToAddressablesMapping.AddressableGroup(obj);
            return Groups.FirstOrDefault(g => g.Name == groupName);
        }

        static TemplatesCacheProxy s_templatesCache;

        public static void Init() {
            s_templatesCache = new();
            TemplatesProvider.AssignEditorDelegate(FindAllOfType);
        }

        public static void EnsureInit() {
            if (s_templatesCache == null) {
                Init();
            }
        }

        public static void EDITOR_RuntimeReset() {
            s_templatesCache?.Instance.Refresh();
        }
        
        public static List<T> FindAllOfType<T>(List<T> results = null, TemplateTypeFlag templateType = TemplateTypeFlag.All) {
            List<ITemplate> allTemplates = s_templatesCache.Instance.Templates;
            results ??= new();
            foreach (ITemplate template in allTemplates) {
                if (IsCorrectTemplateType(template, templateType) && template is T t) {
                    results.Add(t);
                }
            }

            return results;
        }
        
        public static List<ITemplate> FindAllOfType(Type type, List<ITemplate> results = null, bool exactType = false, TemplateTypeFlag templateType = TemplateTypeFlag.All) {
            results ??= new();
            List<ITemplate> allTemplates = s_templatesCache.Instance.Templates;

            if (exactType) {
                foreach (ITemplate template in allTemplates) {
                    if (IsCorrectTemplateType(template, templateType) && type == template.GetType()) {
                        results.Add(template);
                    }
                }
            } else {
                foreach (ITemplate template in allTemplates) {
                    if (IsCorrectTemplateType(template, templateType) && type.IsInstanceOfType(template)) {
                        results.Add(template);
                    }
                }
            }
            return results;
        }
        
        static bool IsCorrectTemplateType(ITemplate template, TemplateTypeFlag templateType) {
            return templateType == TemplateTypeFlag.All || templateType.Contains(template.TemplateType);
        }

        public static List<string> GetPathsForType(Type type) {
            List<ITemplate> allTemplates = s_templatesCache.Instance.Templates;
            List<AddressableAssetEntry> allEntries = s_templatesCache.Instance.Entries;

            var result = new List<string>(200);
            for (int i = 0; i < allTemplates.Count; i++) {
                if (type.IsInstanceOfType(allTemplates[i])) {
                    result.Add(allEntries[i].AssetPath);
                }
            }

            return result;
        }

        public static bool IsTemplateAddressable(ITemplate template, Object obj) {
            List<ITemplate> allTemplates = s_templatesCache.Instance.Templates;
            foreach (ITemplate addressable in allTemplates) {
                if (ReferenceEquals(template, addressable)) {
                    return true;
                }
            }

            var groupName = TemplatesToAddressablesMapping.AddressableGroup(template);
            if (groupName == null) {
                return false;
            } else {
                var group = AddressableHelper.FindGroup(groupName);
                return group.entries.Any(e => ReferenceEquals(e.TargetAsset, obj));
            }
        }
        
        [MenuItem("TG/Assets/Templates/Refresh Group Split")]
        static void RefreshTemplateGroupsSplit(){
            foreach (var entry in s_templatesCache.Instance.Entries) {
                AddressableHelper.MoveEntry(entry, GroupFor(entry.MainAsset));
            }
        }
        
        [MenuItem("TG/Assets/Templates/SetDirty on all Items")]
        static void SetDirtyOnItems(){
            AssetDatabase.StartAssetEditing();
            foreach (var template in s_templatesCache.Instance.Templates) {
                if (template is ItemTemplate itemTemplate) {
                    EditorUtility.SetDirty(itemTemplate);
                }
            }
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    class TemplatesCacheProxy {
        readonly TemplatesCache _cache = new(TemplatesSearcher.Groups);

        public TemplatesCache Instance {
            get {
                if (_cache.ShouldBeRefreshed()) {
                    _cache.Refresh();
                }
                return _cache;
            }
        }
    }
}