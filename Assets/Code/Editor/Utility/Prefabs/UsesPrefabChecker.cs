using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.Prefabs {
    /// <summary>
    /// Utility class to check and fix missing prefab assets using the UsesPrefab attribute.
    /// Useful if you want to rename or move a prefab asset and update all references to it. <para/>
    /// Assumption: <br/>
    /// - All prefabs are located in Assets/Resources/Prefabs/MapViews folder. <br/>
    /// - All prefabs are named as class names (for auto-fixing). <br/>
    /// - Use the UsesPrefab attribute value of the last element in the path to search for a prefab. <br/>
    /// </summary>
    public static class UsesPrefabChecker {
        static readonly Type TargetAttribute = typeof(UsesPrefab);
        static readonly string[] AllowedAssembly = { "TG.Main" };

        static readonly StringBuilder LogBuilder = new();
        static readonly StringBuilder SummaryBuilder = new();

        const string PrefabExtension = ".prefab";
        const string SearchPrefabType = "t:prefab";
        const string SearchScriptType = "t:script";

        static readonly List<string> NonFixedIssues = new();
        static readonly List<string> FixedIssues = new();

        [MenuItem("TG/UI/Find and log missing prefab assets using UsesPrefab attribute")]
        static void LogMissingPrefab() {
            FixMissingPrefab(true);
        }

        [MenuItem("TG/UI/Find and try fix missing prefab assets using UsesPrefab attribute")]
        static void FixMissingPrefab() {
            FixMissingPrefab(false);
        }
        
        static void FixMissingPrefab(bool onlyLog) {
            NonFixedIssues.Clear();
            FixedIssues.Clear();
            PrepareLogBuilder();

            foreach (var item in GetMissingPrefabsData()) {
                if (onlyLog) break;
                
                if(TrySearchForNecessaryAssets(item.targetClass, item.attributePrefabName, out var assetsResult)) {
                    string attributeDirectory = Path.GetDirectoryName(assetsResult.prefabPath)?.Split("MapViews\\").Skip(1).FirstOrDefault();
                    string attributePath = Path.Combine(attributeDirectory ?? string.Empty, Path.GetFileNameWithoutExtension(assetsResult.prefabPath));
                    
                    string prefabAttribute = Path.GetFileNameWithoutExtension(assetsResult.prefabPath) == item.targetClass.Name ? 
                        $"[UsesPrefab(\"{attributeDirectory?.Replace("\\", "/")}/\" + nameof({item.targetClass.Name}))]" : 
                        $"[UsesPrefab(\"{attributePath.Replace("\\", "/")}\")]";

                    RewriteAttributeLine(assetsResult.scriptPath, prefabAttribute);
                    FixedIssues.Add(item.targetClass.Name);
                } else {
                    NonFixedIssues.Add(item.targetClass.Name);
                }
            }
            
            PrepareSummary();
        }

        static void PrepareLogBuilder() {
            LogBuilder.Clear();
            SummaryBuilder.Clear();
            
            SummaryBuilder.AppendLine($"Summary of Uses Prefab Checker".ToUpper().Bold());
            LogBuilder.AppendLine($"List of missing prefab assets in class with {TargetAttribute.Name.Italic()} attribute".ToUpper().Bold());
            LogBuilder.AppendLine("----------------------------------------");
        }
        
        static void PrepareSummary() {
            if (FixedIssues.Count > 0) {
                SummaryBuilder.AppendLine($"Total auto fixed issues: {FixedIssues.Count.ToString().Bold()}");
            }
            
            if (NonFixedIssues.Count > 0) {
                SummaryBuilder.AppendLine("----------------------------------------");
                SummaryBuilder.AppendLine("List of unfixed issue class name - requires manual action".Bold());
                
                foreach(string item in NonFixedIssues) {
                    SummaryBuilder.AppendLine(item);
                }
            }
            
            Log.Important?.Info(LogBuilder.ToString());
            Log.Important?.Info(SummaryBuilder.ToString());
        }

        static IEnumerable<(Type targetClass, string attributePrefabName, string attributePrefabPath)> GetMissingPrefabsData() {
            List<(Type, string, string)> missingPrefabs = new();
            
            foreach (Type type in TypeCache.GetTypesWithAttribute<UsesPrefab>()) {
                if (Attribute.GetCustomAttribute(type, TargetAttribute) is UsesPrefab usesPrefab) {
                    string prefabPath = Path.Combine(Application.dataPath, "Resources", World.PrefabPath, $"{usesPrefab.prefabName}{PrefabExtension}");
                    if (!File.Exists(prefabPath)) {
                        missingPrefabs.Add((type, usesPrefab.prefabName, prefabPath));
                        LogBuilder.AppendLine($"Target Class: {type.Name.Bold()}");
                        LogBuilder.AppendLine($"Searched prefab name: {usesPrefab.prefabName.Bold()}");
                        LogBuilder.AppendLine($"Searched prefab path: {prefabPath.Italic()}");
                        LogBuilder.AppendLine("--------------------");
                    }
                }
            }
            
            SummaryBuilder.AppendLine($"Total missing prefabs: {missingPrefabs.Count.ToString().Bold()}");
            return missingPrefabs;
        }
        
        static bool TrySearchForNecessaryAssets(Type type, string prefabName, out (string scriptPath, string prefabPath) result) {
            result = (string.Empty, string.Empty);
            
            string scriptGUID = AssetDatabase.FindAssets($"{SearchScriptType} {type.Name}").FirstOrDefault();
            string scriptPath = AssetDatabase.GUIDToAssetPath(scriptGUID);
            string prefabGUID = AssetDatabase.FindAssets($"{SearchPrefabType} {Path.GetFileName(prefabName)}").FirstOrDefault();
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGUID);
            
            if (File.Exists(scriptPath) && File.Exists(prefabPath)) {
                result = (scriptPath, prefabPath);
                return true;
            } 
            
            return false;
        }

        static void RewriteAttributeLine(string scriptPath, string prefabAttribute) {
            string[] lines = File.ReadAllLines(scriptPath);

            for (int i = 0; i < lines.Length; i++) {
                if(lines[i].Contains("UsesPrefab")) {
                    lines[i] = $"{lines[i].Split("[UsesPrefab").FirstOrDefault()}{prefabAttribute}";
                    break;
                }
            }
                
            File.WriteAllLines(scriptPath, lines);
        }
    }
}