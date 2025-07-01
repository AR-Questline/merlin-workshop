using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Editor.Graphics {
    public class CompareHdrpAssets : OdinEditorWindow {
        [SerializeField] List<HDRenderPipelineAsset> assets = new();
        [SerializeField] List<DifferentFields> diffs = new();
        
        [MenuItem("TG/Assets/Compare Hdrp Assets")]
        static void OpenWindow() {
            var window = GetWindow<CompareHdrpAssets>();
            window.Show();
        }

        [Button]
        void LoadDiff() {
            var assetsJsons = assets.Select(asset => JsonUtility.ToJson(asset, false)).ToArray();
            diffs = FindDifferences(assetsJsons);
        }

        [Button, ShowIf("@diffs.Count > 0")]
        void SaveToCSV() {
            StringBuilder sb = new(diffs.Count * 16);
            for (int i = 0; i < diffs.Count; i++) {
                var diff = diffs[i];
                sb.Append(diff.fieldName).Append(',');
                foreach (var value in diff.values) {
                    sb.Append(value).Append(',');
                }
                sb.Length--;
                sb.Append('\n');
            }
            File.WriteAllText(Path.Combine(Application.persistentDataPath, "HdrpAssetsComparisonData.csv"), sb.ToString());
        }

        [Serializable]
        struct DifferentFields {
            [HideLabel] public string fieldName;

            [ListDrawerSettings(DefaultExpandedState = true)]
            public List<string> values;
        }

        static List<DifferentFields> FindDifferences(string[] jsonStrings) {
            var tokens = new List<JToken>(jsonStrings.Length);
            foreach (var json in jsonStrings)
                tokens.Add(JToken.Parse(json));

            var allPaths = new HashSet<string>();
            foreach (var t in tokens) {
                CollectPaths(t, "", allPaths);
            }
            
            var diffs = new List<DifferentFields>();
            foreach (var path in allPaths) {
                var values = new List<string>(tokens.Count);
                foreach (var t in tokens) {
                    var sel = t.SelectToken(path);
                    values.Add(sel != null ? sel.ToString() : "null");
                }

                // check if there's more than one distinct value
                bool allSame = true;
                for (int i = 1; i < values.Count; i++) {
                    if (values[i] != values[0]) {
                        allSame = false;
                        break;
                    }
                }

                if (!allSame) {
                    var diff = new DifferentFields();
                    diff.fieldName = path;
                    diff.values = values;
                    diffs.Add(diff);
                }
            }

            return diffs;
        }

        static void CollectPaths(JToken token, string currentPath, HashSet<string> paths) {
            switch (token.Type) {
                case JTokenType.Object:
                    foreach (var prop in ((JObject)token).Properties()) {
                        var next = string.IsNullOrEmpty(currentPath)
                            ? prop.Name
                            : $"{currentPath}.{prop.Name}";
                        CollectPaths(prop.Value, next, paths);
                    }
                    break;

                case JTokenType.Array:
                    var arr = (JArray)token;
                    for (int i = 0; i < arr.Count; i++) {
                        var next = $"{currentPath}[{i}]";
                        CollectPaths(arr[i], next, paths);
                    }
                    break;

                default:
                    paths.Add(currentPath);
                    break;
            }
        }
    }
}