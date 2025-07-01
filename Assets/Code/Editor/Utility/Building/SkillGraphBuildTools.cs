using System;
using System.IO;
using Awaken.TG.Editor.Assets.Templates;
using Awaken.TG.EditorOnly;
using Awaken.TG.Main.Skills;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel;
using Unity.VisualScripting;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Utility.Building {
    public static class SkillGraphBuildTools {
        [MenuItem("TG/Build/Baking/Bake SkillGraphs", false, -3000)]
        public static void PrepareForBuild() {
            TemplatesSearcher.EnsureInit();
            AssetDatabase.StartAssetEditing();

            if (Directory.Exists(StreamedSkillGraphs.BakingDirectoryPath)) {
                Directory.Delete(StreamedSkillGraphs.BakingDirectoryPath, true);
            }
            Directory.CreateDirectory(StreamedSkillGraphs.BakingDirectoryPath);

            try {
                foreach (var skillGraph in TemplatesSearcher.FindAllOfType<SkillGraph>()) {
                    var asset = skillGraph.EditorAsset;
                    if (asset == null) {
                        skillGraph.EditorSerializableGuid = SerializableGuid.Empty;
                        EditorUtility.SetDirty(skillGraph);
                        Log.Critical?.Error($"SkillGraph with no VisualGraph{skillGraph}.", skillGraph);
                        continue;
                    }
                    if (EnsureScript(asset) == false) {
                        skillGraph.EditorSerializableGuid = SerializableGuid.Empty;
                        EditorUtility.SetDirty(skillGraph);
                        Log.Critical?.Error($"Failed to serialize SkillGraph {skillGraph}. See error above", skillGraph);
                    }
                }
            } finally {
                AssetDatabase.StopAssetEditing();
            }
        }

        static unsafe bool EnsureScript(ScriptGraphAsset graph) {
            graph.Serialize(out var json, out var dependencies);
            var guids = new Guid[dependencies.Length];
            for (int i = 0; i < dependencies.Length; i++) {
                Object dependency = dependencies[i];
                if (dependency is ScriptGraphAsset dependencyGraph) {
                    guids[i] = AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(dependencyGraph)).ToSystemGuid();
                    if (EnsureScript(dependencyGraph) == false) {
                        return false;
                    }
                } else {
                    Log.Important?.Error($"Cannot Serialize UnityObject {dependency}. It must by assigned through SkillVariables.", dependency);
                    return false;
                }
            }
            var guid = AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(graph)).ToSystemGuid();
            var bakingPath = Path.Combine(StreamedSkillGraphs.BakingDirectoryPath, $"{guid:N}.skill");
            var writer = new FileWriter(bakingPath, FileMode.CreateNew);
            
            writer.Write(guids.Length);
            foreach (var dependencyGuid in guids) {
                writer.Write(dependencyGuid);
            }
            fixed (char* ptr = json) {
                writer.Write(ptr, json.Length);
            }
            writer.Dispose();
            
            return true;
        }
    }
}