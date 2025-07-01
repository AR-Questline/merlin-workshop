using UnityEditor;

namespace Awaken.TG.Editor.Utility {
    public static class SimulateBuildDefines {
        [MenuItem("TG/Build/Simulate Build defines/Add Simulate Build define", false, 4003)]
        public static void AddSimulateBuildDefine() {
            ScriptingDefinesUtils.AddScriptingDefine("SIMULATE_BUILD");
        }
        
        [MenuItem("TG/Build/Simulate Build defines/Add Scenes processed define", false, 4003)]
        public static void AddScenesProcessedDefine() {
            ScriptingDefinesUtils.AddScriptingDefine("SCENES_PROCESSED");
        }

        [MenuItem("TG/Build/Simulate Build defines/Add Addressables build define", false, 4003)]
        public static void AddAddressablesBuildDefine() {
            ScriptingDefinesUtils.AddScriptingDefine("ADDRESSABLES_BUILD");
        }

        [MenuItem("TG/Build/Simulate Build defines/Add archives produced define", false, 4003)]
        public static void AddArchivesProducedDefine() {
            ScriptingDefinesUtils.AddScriptingDefine("ARCHIVES_PRODUCED");
        }
    }
}