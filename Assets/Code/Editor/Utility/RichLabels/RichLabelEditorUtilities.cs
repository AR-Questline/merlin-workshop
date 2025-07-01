using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.TG.Editor.Utility.RichLabels.Configs;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Utility.RichLabels;
using Awaken.TG.Main.Utility.RichLabels.SO;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.RichLabels {
    public static class RichLabelEditorUtilities {

        public static RichLabelConfig GetOrCreateRichLabelConfig(RichLabelConfigType configType) {
            string resourcePath = Path.Combine(RichLabelConfig.RichLabelResourcePath, configType.ToString());
            var config = Resources.Load<RichLabelConfig>(resourcePath);

            if (config == null) {
                config = ScriptableObject.CreateInstance<RichLabelConfig>();
                string assetPath = Path.Combine("Assets", "Resources", $"{resourcePath}.asset");
                AssetDatabase.CreateAsset(config, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return config;
        }

        public static Dictionary<string, List<RichLabelUsageEntry>> GetGuidsByCategories(IReadOnlyList<RichLabelUsageEntry> entries, RichLabelConfig richLabelConfig) {
            var result = new Dictionary<string, List<RichLabelUsageEntry>>();
            foreach (var category in richLabelConfig.GetPossibleCategories()) {
                result.Add(category.Guid, new List<RichLabelUsageEntry>());
            }

            foreach (var entry in entries) {
                if(richLabelConfig.TryGetCategory(entry.RichLabelGuid, out RichLabelCategory category)) {
                    result[category.Guid].Add(entry);
                }
            }

            return result;
        }
        
        public static Dictionary<string, List<string>> GetGuidsByCategories(IReadOnlyList<string> guids, RichLabelConfig richLabelConfig) {
            var result = new Dictionary<string, List<string>>();
            foreach (var category in richLabelConfig.GetPossibleCategories()) {
                result.Add(category.Guid, new List<string>());
            }

            foreach (var guid in guids) {
                if (richLabelConfig.TryGetCategory(guid, out RichLabelCategory category)) {
                    result[category.Guid].Add(guid);
                }
            }

            return result;
        }
        
        public static bool TryAddLabelOfGuid(RichLabelSet set, string guid, RichLabelCategory category) {
            if (set.richLabelGuids.Contains(guid)) {
                return false;
            }

            if (category.SingleChoice) {
                set.richLabelGuids.RemoveAll(g => category.Labels.Any(e => e.Guid == g));
                set.richLabelGuids.Add(guid);
                return true;
            }

            set.richLabelGuids.Add(guid);
            return true;
        }
        
        public static void FillSetCategoryWithLabelsFromTags(RichLabelSet set, RichLabelCategory category, LocationSpec locationSpec) {
            FillSetCategoryWithLabelsFromStrings(set, category, locationSpec.tags);
        }

        public static void FillSetCategoryWithLabelsFromStrings(RichLabelSet set, RichLabelCategory category, IEnumerable<string> strings) {
            foreach (var str in strings) {
                var richLabel = category.FindLabel(str);
                if (richLabel != null) {
                    set.richLabelGuids.Add(richLabel.Guid);
                    continue;
                }

                richLabel = new RichLabel(str);
                if (TryAddLabelOfGuid(set, richLabel.Guid, category)) {
                    category.Labels.Add(richLabel);
                }
            }
        }
        
        public static string RichLabelEntriesToString(RichLabelConfig richLabelConfig, IEnumerable<RichLabelUsageEntry> entries) {
            return string.Join(" | ", entries
                                   .OrderBy(e => richLabelConfig.GetCategoryIndexExpensive(e.RichLabelGuid).ToString(), StringComparer.OrdinalIgnoreCase)
                                   .Select(entry => richLabelConfig.GetLabelOrEmpty(entry.RichLabelGuid) + (entry.Include ? "" : " (not included)")));
        }
    }
}