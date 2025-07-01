using System;
using System.IO;
using Awaken.Utility.Debugging;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Editor.VisualScripting.Parsing.Scripts {
    public static class UnitMaker {

        static readonly string PathToTemplate = @"Assets/Code/Editor/VisualScripting/Parsing/Scripts/UnitTemplate.txt";
        public static readonly string PathToCodeDirectory = @"Assets/Code";
        public static readonly string[] DefaultUsings = {"Unity.VisualScripting", "UnityEngine", "System"};

        public static void MakeUnit(ScriptGraphAsset asset, bool parseRecursively = false, int depth = 100, bool forceOverride = false) {
            if (depth == 0) return;

            if (!forceOverride && UnitExist(asset)) {
                if (!EditorUtility.DisplayDialog($"Parsing {asset}", $"Unit for asset {asset} already exists.\nDo you want to override?", "Yes", "No")) {
                    return;
                }
            }
            
            GraphInput input = null;
            GraphOutput output = null;
            
            foreach (var unit in asset.graph.units) {
                if (unit is GraphInput i) {
                    if (input != null) throw new Exception($"There are more than one input units in graph {asset}");
                    input = i;
                }
                if (unit is GraphOutput o) {
                    if (output != null) throw new Exception($"There are more than one output units in graph {asset}");
                    output = o;
                }
                if (parseRecursively && unit is SubgraphUnit subgraph) {
                    MakeUnit(subgraph.nest.macro, true, depth - 1);
                }
            }

            if (input == null && output == null) return;
            
            if (input == null) throw new Exception($"There is no input unit in graph {asset}");
            if (output == null) throw new Exception($"There is no output unit in graph {asset}");
            
            Log.Important?.Info($"Parsing graph {asset}");
            
            (string name, string space) = UnitNameAndSpace(asset);
            var script = new UnitScript(PathToTemplate);
            script.SetName(name, space);
            script.SetInvoke(input, output);
            script.Create();
        }

        public static (string, string) UnitNameAndSpace(ScriptGraphAsset asset) {
            var path = AssetDatabase.GetAssetPath(asset);
            int index = path.IndexOf("VisualScripts");
            var name = Path.GetFileName(path);
            var space = "Awaken.TG.VisualScripts.Units.Generated." + path[(index+14)..^(1+name.Length)];
            space = space.Replace("\\", ".").Replace("/", ".").Replace(" ", "_");
            return (name[..^6], space);
        }

        public static bool UnitExist(ScriptGraphAsset asset) {
            (string name, string space) = UnitNameAndSpace(asset);
            
            var path = string.Join("/", UnitMaker.PathToCodeDirectory, space[10..]).Replace(".", "/");
            if (!Directory.Exists(path)) {
                return false;
            }

            path += $"/{name}.cs";
            if (!File.Exists(path)) {
                return false;
            }

            return true;
        }
    }
}