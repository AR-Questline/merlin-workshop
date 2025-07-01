using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Editor.Debugging {
    public static class SavingStats {
        [MenuItem("TG/Debug/Print saving models info")]
        public static void SaveInfo() {
            Dictionary<Type, int> count = new Dictionary<Type, int>();
        
            var allRoots = World.AllInOrder().Where(m => !(m is Element) && CanBeSaved(m));
            Queue<Model> models = new Queue<Model>(allRoots);
            while (models.Count > 0) {
                Model model = models.Dequeue();
                Type modelType = model.GetType();
                if (count.TryGetValue(modelType, out int c)) {
                    count[modelType] = ++c;
                } else {
                    count.Add(modelType, 1);
                }

                foreach (var element in model.AllElements().Where(CanBeSaved)) {
                    models.Enqueue(element);
                }
            }
            
            Log.Important?.Info($"<color=blue>Whole</color> => <color=yellow>{count.Aggregate(0, (prev, pair) => prev + pair.Value)}</color>");

            var orderedCount = count
                .Select(kv => (kv.Key, kv.Value))
                .OrderByDescending(kv => kv.Item2);
            
            foreach (var pair in orderedCount) {
                Log.Important?.Info($"<color=blue>{pair.Item1}</color> => <color=yellow>{pair.Item2}</color>");
            }
        }

        static bool CanBeSaved(Model model) {
            return model.IsNotSaved == false;
        }
        
        [MenuItem("TG/Debug/Print models count info")]
        public static void ModelsCountInfo() {
            Dictionary<Type, int> count = new Dictionary<Type, int>();
        
            var models = World.AllInOrder();
            foreach (var model in models) {
                Type modelType = model.GetType();
                if (count.TryGetValue(modelType, out int c)) {
                    count[modelType] = ++c;
                } else {
                    count.Add(modelType, 1);
                }
            }

            Log.Important?.Info($"<color=blue>Whole</color> => <color=yellow>{count.Aggregate(0, (prev, pair) => prev + pair.Value)}</color>");

            var orderedCount = count
                .Select(kv => (kv.Key, kv.Value))
                .OrderByDescending(kv => kv.Item2);
            
            foreach (var pair in orderedCount) {
                Log.Important?.Info($"<color=blue>{pair.Item1}</color> => <color=yellow>{pair.Item2}</color>");
            }
        }
    }
}