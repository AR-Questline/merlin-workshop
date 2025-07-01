using System;
using System.Diagnostics;
using Awaken.TG.Editor.Assets;
using Awaken.TG.Graphics.Scene;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Awaken.TG.Editor.Utility {
    public class BuildSceneBaking : IDisposable {
        public static bool isBakingScenes;
        static int s_semaphore;

        static Stopwatch s_watch = new();

        public BuildSceneBaking() {
            s_semaphore++;

            if (s_semaphore == 1) {
                ProjectValidator.SceneValidator.UnregisterCallbacks();
                Set(true);
                s_watch.Start();
            }
        }
        
        public void Dispose() {
            s_semaphore--;
            
            if (s_semaphore == 0) {
                ProjectValidator.SceneValidator.RegisterCallbacks();
                Set(false);
                s_watch.Stop();
                Debug.LogError($"Finished baking in {s_watch.Elapsed.Hours:00}:{s_watch.Elapsed.Minutes:00}:{s_watch.Elapsed.Seconds:00}");
            }
        }

        void Set(bool active) {
            isBakingScenes = active;
            SceneConfigs.DisableSceneRefresh = active;
            EditorPrefs.SetBool("AR_BakingScenes", active);
        }
    }
}