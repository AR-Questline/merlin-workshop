using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.Utility.Editor.Scenes {
    public static class ScenesProcessing {
        static SceneProcessor[] s_instancesSorted;
        static SceneProcessor[] InstancesSorted => s_instancesSorted ??= GetSceneProcessorsSorted();

        // Invoked from SceneService with reflection
        public static void ProcessScene(Scene scene, bool useContextIndependentProcessors) {
            foreach (var instance in InstancesSorted) {
                try {
                    if (instance.canProcessSceneInIsolation == useContextIndependentProcessors) {
                        instance.OnProcessScene(scene);
                    }
                } catch (Exception e) {
                    Debug.LogException(e);
                }
            }
        }

        static SceneProcessor[] GetSceneProcessorsSorted() {
            var instancesArr = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => !type.IsInterface && !type.IsAbstract && typeof(SceneProcessor).IsAssignableFrom(type))
                .Select(type => Activator.CreateInstance(type) as SceneProcessor).ToArray();
            Array.Sort(instancesArr, new SceneProcessor.Comparer());
            return instancesArr;
        }
    }
}