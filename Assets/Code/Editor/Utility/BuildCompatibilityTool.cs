using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility {
    public static class BuildCompatibilityTool {
        static string[] s_overridenArguments;

        [MenuItem("TG/Build/Compatibility mode/Run build compatibility", false, 4001)]
        static void RunBuildCompatibilityForAllScenes() {
            if (Application.isBatchMode) {
                RunBuildCompatibility(BuildTools.GetAllScenes());
            } else {
                OverridesWizard.ShowForOverrides(static (overrides, extraDefines) => {
                    s_overridenArguments = overrides;
                    RunBuildCompatibility(BuildTools.GetAllScenes());
                });
            }
        }

        static void RunBuildCompatibility(string[] scenesToProcessPaths) {
            BuildTools.ProcessScenes(scenesToProcessPaths);
            if (BuildTools.HasArgument("build_addressables", s_overridenArguments)) {
                BuildTools.PrepareAndBuildAddressables();
            }

            SimulateBuildDefines.AddSimulateBuildDefine();
        }
    }
}