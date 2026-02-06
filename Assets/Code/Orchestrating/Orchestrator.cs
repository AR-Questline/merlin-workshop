using Animancer;
using Awaken.Babel;
using Awaken.Kandra;
using Awaken.PackageUtilities.Collections;
using Awaken.PackageUtilities.CommonInterfaces;
using Awaken.TG.Debugging;
using Awaken.TG.Debugging.Logging;
using Awaken.TG.MVC;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.Graphics.Mipmaps;
using Awaken.Utility.LowLevel;
#if !UNITY_EDITOR
using UnityEngine;
#endif

namespace Awaken.Orchestrating {
    public static class Orchestrator {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        static void Initialize() {
            AllocationsTracker.Init();
            
            LogsCollector.Init();
            
            Configuration.InitializeData();
            MipmapsStreamingMasterTextures.Init();
            KandraRendererManager.Init();
            PlayerLoopBasedLifetime.Init();
            //HLODManager.Init();
            AnimancerDisposeTracker.Init();
            UnityUpdateProvider.GetOrCreate();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged += targetState => {
                if (targetState == UnityEditor.PlayModeStateChange.EnteredEditMode) {
                }
                if (targetState == UnityEditor.PlayModeStateChange.ExitingEditMode) {
                    Configuration.InitializeData();
                }
                if (targetState == UnityEditor.PlayModeStateChange.EnteredPlayMode) {
                }
                if (targetState == UnityEditor.PlayModeStateChange.ExitingPlayMode) {
                    Configuration.InitializeData();
                }
            };
#endif
        }
    }
}
