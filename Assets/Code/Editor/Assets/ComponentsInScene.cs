using System;
using System.Collections.Generic;
using Awaken.Utility.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Editor.Assets {
    public class ComponentsInScene {
        [MenuItem("TG/Assets/List Components In Scene", false, priority: 9_000)]
        public static void ListComponentsInScene() {
            var counter = new Dictionary<Type, int>();
            for (int i = 0; i < SceneManager.sceneCount; i++) {
                var scene = SceneManager.GetSceneAt(i);
                var roots = scene.GetRootGameObjects();

                foreach (var root in roots) {
                    var components = root.GetComponentsInChildren<Component>(true);
                    foreach (var component in components) {
                        if (component == null) {
                            continue;
                        }
                        if (component.hideFlags.HasCommonBitsFast(HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor)) {
                            continue;
                        }
                        if (component.gameObject.hideFlags.HasCommonBitsFast(HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor)) {
                            continue;
                        }

                        var type = component.GetType();
                        if (!counter.TryAdd(type, 1)) {
                            counter[type]++;
                        }
                    }
                }
            }

            // sort (type, count) pairs so most used first
            var sorted = new List<(Type type, int count)>(counter.Count);
            foreach (var kvp in counter) {
                sorted.Add((kvp.Key, kvp.Value));
            }
            sorted.Sort((a, b) => b.count.CompareTo(a.count));

            // print to console
            foreach (var (type, count) in sorted) {
                Debug.Log($"{type.Name}: {count}");
            }
        }
    }
}
