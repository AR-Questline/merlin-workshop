using System;
using System.Collections.Generic;
using Awaken.Utility.Debugging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.Utility.Editor.Scenes {
    public abstract class SceneProcessor {
        
        static readonly Dictionary<Scene, List<Type>> SceneToExecutedProcessorsMap = new();
        public abstract int callbackOrder { get; }
        public abstract bool canProcessSceneInIsolation { get; }

        // Used in ApplicationScene with reflection
        public static void ResetAllScenesProcessedStatus() {
            SceneToExecutedProcessorsMap.Clear();
        }
        
        // Invoked from SceneService with reflection
        public static void ResetSceneProcessedStatus(Scene scene) {
            SceneToExecutedProcessorsMap.Remove(scene);
        }
        
        public void OnProcessScene(Scene scene) {
#if SCENES_PROCESSED
            return;
#endif
            if (SceneToExecutedProcessorsMap.TryGetValue(scene, out var executedProcessors) == false) {
                executedProcessors = new List<Type>(20);
                SceneToExecutedProcessorsMap.Add(scene, executedProcessors);
            }
            var thisProcessorType = this.GetType();
            if (executedProcessors.Contains(thisProcessorType)) {
                Log.Important?.Error($"Trying to use same processor twice {thisProcessorType.Name} on scene {scene.path}");
                return;
            }
            executedProcessors.Add(thisProcessorType);
            OnProcessScene(scene, Application.isPlaying);
        }
        
        protected abstract void OnProcessScene(Scene scene, bool processingInPlaymode);
        
        public class Comparer : IComparer<SceneProcessor> {
            public int Compare(SceneProcessor x, SceneProcessor y) {
                return x.callbackOrder.CompareTo(y.callbackOrder);
            }
        }
    }
}