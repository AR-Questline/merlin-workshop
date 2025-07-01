using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Steps;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using XNode;

namespace Awaken.TG.Editor.Utility.StoryGraphs {
    public class FindStepsUsageWindow : OdinEditorWindow {
        [ShowInInspector] Type stepType;
        [ShowInInspector] StoryGraph[] storyGraphs;

        [Button]
        void Find() {
            if (stepType == null) {
                return;
            }
            if (!typeof(EditorStep).IsAssignableFrom(stepType)) {
                return;
            }
            IEnumerable<NodeGraph> graphs = (IEnumerable<NodeGraph>)typeof(Converter.GraphConverterUtils)
                .GetMethod("AllStoriesWithElement",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                .MakeGenericMethod(stepType)
                .Invoke(null, null);
            storyGraphs = graphs.OfType<StoryGraph>().ToArray();
        }

        [MenuItem("TG/Assets/Find story step usage")]
        static void ShowWindow() {
            var window = GetWindow<FindStepsUsageWindow>();
            window.titleContent = new GUIContent("Find step usage");
            window.Show();
        }
    }
}
