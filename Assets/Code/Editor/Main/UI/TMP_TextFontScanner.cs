using UnityEngine;
using UnityEditor;
using TMPro;
using System.Collections.Generic;
using System.Text;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Editor.Main.UI {
    public class TMP_TextFontScanner : EditorWindow {
        const string NoneLabel = "None";
        
        [MenuItem("TG/UI/Scan TMP Fonts and Styles in Prefabs")]
        public static void ScanTMPFonts() {
            Dictionary<string, List<(string objPath, string fontName, FontStyles fontStyle, string textStyleName)>>
                results = new();

            // Accumulators for occurrences
            Dictionary<string, int> fontCounts = new();
            Dictionary<FontStyles, int> fontStyleCounts = new();
            Dictionary<string, int> textStyleCounts = new();

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });

            foreach (string guid in guids) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                var tmpComponents = prefab.GetComponentsInChildren<TMP_Text>(true);
                if (tmpComponents.Length == 0) continue;

                var entries = new List<(string, string, FontStyles, string)>();

                foreach (var tmp in tmpComponents) {
                    string objPath = tmp.transform.GetHierarchyPath();
                    string fontName = tmp.font != null ? tmp.font.name : NoneLabel;
                    FontStyles fontStyle = tmp.fontStyle;
                    string textStyleName = tmp.textStyle != null ? tmp.textStyle.name : NoneLabel;

                    entries.Add((objPath, fontName, fontStyle, textStyleName));

                    // Count occurrences
                    if (!fontCounts.TryAdd(fontName, 1)) {
                        fontCounts[fontName]++;
                    }

                    if (!fontStyleCounts.TryAdd(fontStyle, 1)) {
                        fontStyleCounts[fontStyle]++;
                    }

                    if (!textStyleCounts.TryAdd(textStyleName, 1)) {
                        textStyleCounts[textStyleName]++;
                    }
                }

                results[path] = entries;
            }

            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine("========= TMP_Text components found in prefabs =========");
            foreach (var kv in results) {
                foreach (var item in kv.Value) {
                    stringBuilder.AppendLine(
                        $"Prefab: {kv.Key} | Object: {item.objPath} | Font: {item.fontName} | FontStyle: {item.fontStyle} | TextStyle: {item.textStyleName}");
                }
            }

            Log.Debug?.Info(stringBuilder.ToString());
            stringBuilder.Clear();

            stringBuilder.AppendLine("========= Summary of TMP_Text usages (occurrences) =========");
            stringBuilder.AppendLine("Fonts:");
            foreach (var pair in fontCounts) {
                stringBuilder.AppendLine($"  {pair.Key}: {pair.Value}");
            }

            stringBuilder.AppendLine("\nFontStyles:");
            foreach (var pair in fontStyleCounts) {
                stringBuilder.AppendLine($"  {pair.Key}: {pair.Value}");
            }

            stringBuilder.AppendLine("\nTextStyle names (TMP_Style):");
            foreach (var pair in textStyleCounts) {
                stringBuilder.AppendLine($"  {pair.Key}: {pair.Value}");
            }

            Log.Debug?.Info(stringBuilder.ToString());
        }
    }

    /// <summary>
    /// Full hierarchy path for the object
    /// </summary>
    internal static class TransformHierarchyExtensions {
        internal static string GetHierarchyPath(this Transform transform) {
            string path = transform.name;
            while (transform.parent != null) {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }
    }
}