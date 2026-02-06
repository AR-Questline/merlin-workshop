using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Utility.Skills;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Editor.Utility {
    public class ItemTemplateKeywordChecker {
        [MenuItem("TG/Design/Check Item Template Keywords")]
        public static void CheckItemTemplateKeywords() {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Data" });
            int problemCount = 0;

            foreach (string guid in prefabGuids) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                ItemTemplate[] items = prefab.GetComponentsInChildren<ItemTemplate>(true);
                if (items == null || items.Length == 0) continue;

                problemCount += CheckItemTemplates(prefab, items);
            }

            if (problemCount == 0) {
                Log.Debug?.Info("Check completed: All ItemTemplate descriptions contain valid keywords.");
            } else {
                Log.Debug?.Error($"Check completed: Found {problemCount} ItemTemplate(s) with keyword issues.");
            }
        }
    
        static int CheckItemTemplates(GameObject prefab, ItemTemplate[] items) {
            int problems = 0;
            
            foreach (var item in items) {
                string desc = item.IsMagic 
                    ? string.Join("\n", item.Description, item.LightCastInfo.MagicDescription, item.HeavyCastInfo.MagicDescription) 
                    : item.Description;
                
                bool valid = SkillsUtils.CheckKeywords(desc, out List<string> keywordsMarker);

                if (!valid) {
                    problems++;
                    Log.Debug?.Error($"Prefab '{prefab.name}' has an ItemTemplate with keyword marker but no valid keyword found.\nFound wrong keywords marker: {string.Join(", ", keywordsMarker)}", prefab);
                }
            }
         
            return problems;
        }
    }
}
