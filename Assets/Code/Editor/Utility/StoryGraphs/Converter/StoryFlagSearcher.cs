using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Stories.Conditions;
using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Steps;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using XNode;
using XNodeEditor;
using static Awaken.TG.Editor.Utility.StoryGraphs.Converter.GraphConverterUtils;

namespace Awaken.TG.Editor.Utility.StoryGraphs.Converter {
    public class StoryFlagSearcher : OdinEditorWindow {
        const string SearchGroup = "Search";
        
        [SerializeField] string flag = "";
        [SerializeField] List<Result> exactResults = new();
        [SerializeField] List<Result> partialResults = new();
        
        [Button, HorizontalGroup(SearchGroup)]
        void Search() {
            exactResults.Clear();
            partialResults.Clear();
            foreach (var (result, f) in AllNodesWithFlags()) {
                if (f == flag) {
                    exactResults.Add(result);
                } else if (f.Contains(flag)) {
                    partialResults.Add(result);
                }
            }
        }

        [MenuItem("TG/Assets/Story/Find Flag")]
        static void Open() {
            GetWindow<StoryFlagSearcher>().Show();
        }

        static IEnumerable<(Result result, string flag)> AllNodesWithFlags() {
            return FromSteps<SEditorFlagChange>(step => step.flag)
                .Concat(FromConditions<CEditorFlag>(condition => condition.flag));
        }

        static IEnumerable<(Result result, string flag)> FromSteps<TStep>(Func<TStep, string> getFlag) where TStep : EditorStep {
            foreach (var trio in AllElements<ChapterEditorNode, TStep>()) {
                yield return (new Result(trio.graph, trio.node), getFlag(trio.element));
            }
        }
        
        static IEnumerable<(Result result, string flag)> FromConditions<TCondition>(Func<TCondition, string> getFlag) where TCondition : EditorCondition {
            foreach (var trio in AllElements<ConditionsEditorNode, TCondition>()) {
                yield return (new Result(trio.graph, trio.node), getFlag(trio.element));
            }
        }

        [Serializable]
        struct Result {
            [HorizontalGroup, HideLabel] public NodeGraph graph;
            [HorizontalGroup, HideLabel] public StoryNode node;

            public Result(NodeGraph graph, StoryNode node) {
                this.graph = graph;
                this.node = node;
            }

            [HorizontalGroup, Button]
            void Ping() {
                NodeEditorWindow.Open(graph).CenterOnNode(node);
            }
        }
    }
}