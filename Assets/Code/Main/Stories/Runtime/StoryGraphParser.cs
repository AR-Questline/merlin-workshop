using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Stories.Conditions;
using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Conditions.GameModes;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps;
using Awaken.Utility.Collections;
using Awaken.Utility.Times;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Runtime {
    public class StoryGraphParser {
        readonly Dictionary<ChapterEditorNode, StoryChapter> _chapterMap = new();
        readonly Dictionary<ConditionsEditorNode, StoryConditions> _conditionMap = new();
        
        StructList<StoryChapter> _chapters = new(0);
        StructList<StoryConditions> _conditions = new(0);

        public static StoryGraphRuntime Parse(StoryGraph graph) {
            StartNode startNode = null;
            
            var parser = new StoryGraphParser();
            foreach (var node in graph.nodes) {
                if (node is StoryStartEditorNode editorStartNode) {
                    startNode = new StartNode {
                        involveAI = editorStartNode.involveAI,
                        involveHero = editorStartNode.involveHero,
                        enableChoices = editorStartNode.enableChoices,
                    };
                    var steps = new StructList<SStoryStartChoice>(editorStartNode.elements.Count + 1);
                    foreach (var element in editorStartNode.elements) {
                        if (element is SEditorStoryStartChoice startChoice) {
                            var step = startChoice.CreateRuntimeStep(parser);
                            step.parentChapter = startNode;
                            steps.Add(step);
                        }
                    }
                    startNode.continuation = parser.GetChapter(editorStartNode.ContinuationChapter as ChapterEditorNode);
                    startNode.choices = steps.ToArray();
                    startNode.steps = Array.Empty<StoryStep>();
                }
                if (node is ChapterEditorNode editorChapter) {
                    if (editorChapter.Steps.Any(step => step is SEditorBookmark)) {
                        parser.GetChapter(editorChapter);
                    }
                }
            }
            
            return new StoryGraphRuntime {
                guid = graph.GUID,
                usedSoundBanksNames = graph.usedSoundBanksNames.Contains(graph.name) ? graph.usedSoundBanksNames : CreateCopyWithGraphNameAdded(graph.usedSoundBanksNames, graph.name),
                tags = graph.tags,
                variables = graph.variables.ToArray(),
                variableReferences = graph.variableReferences.ToArray(),
                sharedBetweenMultipleNPCs = graph.sharedBetweenMultipleNPCs,
                
                startNode = startNode,
                chapters = parser._chapters.ToArray(),
                conditions = parser._conditions.ToArray(),
            };
        }

        public static void Serialize(StoryGraph graph) {
            var runtimeGraph = Parse(graph);
            var path = Path.Combine(StoryGraphRuntime.BakingDirectoryPath, $"{graph.GUID}.story");
            StoryWriter.Write(runtimeGraph, path);
            runtimeGraph.Dispose();
        }
        
        public StoryChapter GetChapter(ChapterEditorNode editorChapter) {
            if (editorChapter == null) {
                return null;
            }
            if (_chapterMap.TryGetValue(editorChapter, out var chapter)) {
                return chapter;
            }

            chapter = new StoryChapter();
            _chapterMap.Add(editorChapter, chapter);
            _chapters.Add(chapter);

            var steps = new StructList<StoryStep>(editorChapter.elements.Count + 1);
            bool endNeeded = editorChapter.ContinuationChapter == null || editorChapter.ContinuationChapter.IsEmptyAndHasNoContinuation;
            foreach (var element in editorChapter.elements) {
                if (element is EditorStep editorStep) {
                    endNeeded = endNeeded && !editorStep.MayHaveContinuation;
                    var step = editorStep.CreateRuntimeStep(this);
                    if (step != null) {
                        step.parentChapter = chapter;
                        steps.Add(step);
                    }
                }
            }
            if (endNeeded) {
                steps.Add(new SLeave {
                    conditions = Array.Empty<StoryConditionInput>(),
                    parentChapter = chapter,
                });
            }

            chapter.steps = steps.ToArray();
            chapter.continuation = GetChapter(editorChapter.ContinuationChapter as ChapterEditorNode);
#if UNITY_EDITOR
            chapter.EditorNode = editorChapter;
#endif            
            return chapter;
        }
        
        public StoryConditions GetConditions(ConditionsEditorNode editorConditions) {
            if (editorConditions == null) {
                return null;
            }
            if (_conditionMap.TryGetValue(editorConditions, out var conditions)) {
                return conditions;
            }
            conditions = editorConditions.CreateRuntimeConditions(this);
            _conditionMap.Add(editorConditions, conditions);
            _conditions.Add(conditions);

            StructList<StoryConditionInput> inputs = new(0);
            foreach (var input in editorConditions.InputNodes) {
                var inputConditions = GetConditions(input);
                if (inputConditions != null) {
                    inputs.Add(new StoryConditionInput {
                        conditions = inputConditions,
                        negate = input.IsConnectionNegated(editorConditions),
                    });
                }
            }
            
            StructList<StoryCondition> steps = new(0);
            foreach (var element in editorConditions.Elements) {
                var condition = element.CreateRuntimeCondition(this);
                if (condition != null) {
                    steps.Add(condition);
                }
            }
            
            conditions.inputs = inputs.ToArray();
            conditions.conditions = steps.ToArray();
            
            return conditions;
        }
        
        static string[] CreateCopyWithGraphNameAdded(string[] banksNames, string graphName) {
            var usedSoundBanksNamesCopy = new string[banksNames.Length + 1];
            for (int i = 0; i < banksNames.Length; i++) {
                usedSoundBanksNamesCopy[i] = banksNames[i];
            }
            usedSoundBanksNamesCopy[banksNames.Length] = graphName;
            return usedSoundBanksNamesCopy;
        }
        
#if UNITY_EDITOR
        [UnityEditor.MenuItem("TG/Debug/Story/Serialization Test")]
        static void TestSerialization() {
            const string guid = "guid";
            var graph = new StoryGraphRuntime();
            if (true) { // build graph
                graph.guid = guid;
                graph.usedSoundBanksNames = new[] { "sound1", "sound2" };
                graph.tags = new[] { "tag1", "tag2" };
                graph.variables = Array.Empty<VariableDefine>();
                graph.variableReferences = Array.Empty<VariableReferenceDefine>();
                graph.sharedBetweenMultipleNPCs = false;

                graph.startNode = new StartNode() {
                    enableChoices = true,
                    involveHero = false,
                    involveAI = true,
                    choices = Array.Empty<SStoryStartChoice>(),
                    steps = Array.Empty<StoryStep>(),
                };

                var firstCondition = new StoryAndConditions {
                    inputs = Array.Empty<StoryConditionInput>(),
                    conditions = new StoryCondition[] {
                        new CIsDemo(),
                    }
                };
                var secondCondition = new StoryOrConditions {
                    inputs = new StoryConditionInput[] {
                        new() {
                            conditions = firstCondition,
                            negate = true,
                        },
                    },
                    conditions = new StoryCondition[] {
                        new CIsDemo(),
                        new CGender() {
                            gender = Gender.Male
                        }
                    }
                };

                var firstChapter = new StoryChapter {
                    steps = new StoryStep[] {
                        new SBookmark {
                            flag = "flag",
                            storySettings = true,
                            involveHero = true,
                            involveAI = false,
                            conditions = new StoryConditionInput[] {
                                new() {
                                    conditions = firstCondition,
                                    negate = true,
                                },
                            },
                        },
                        new SLeave {
                            conditions = new StoryConditionInput[] {
                                new() {
                                    conditions = secondCondition,
                                    negate = false,
                                },
                            },
                        },
                    }
                };
                var secondChapter = new StoryChapter {
                    steps = new StoryStep[] {
                        new SNodeJump {
                            spanFlag = "span.flag",
                            span = TimeSpans.Ever,
                            targetChapter = firstChapter,
                            conditions = Array.Empty<StoryConditionInput>(),
                        }
                    }
                };

                graph.startNode.continuation = firstChapter;
                firstChapter.continuation = secondChapter;

                graph.chapters = new[] {
                    firstChapter,
                    secondChapter
                };
                graph.conditions = new StoryConditions[] {
                    firstCondition,
                    secondCondition
                };
            }
            
            var path = Path.Combine(Application.temporaryCachePath, "story.story");
            
            StoryWriter.Write(graph, path);
            graph.Dispose();
            
            var readGraph = StoryReader.Read(guid, path);
            readGraph.Dispose();
        }
#endif
    }
}