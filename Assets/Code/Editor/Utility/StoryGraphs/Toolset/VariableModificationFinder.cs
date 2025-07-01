using System;
using System.Linq;
using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Interfaces;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.Templates;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Extensions;
using Awaken.Utility.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using XNode;
using static Awaken.TG.Editor.Utility.StoryGraphs.Converter.GraphConverterUtils;

namespace Awaken.TG.Editor.Utility.StoryGraphs.Toolset {
    [Serializable]
    public class VariableModificationFinder : StoryGraphUtilityTool<SearchResult<VariableModificationResultEntry>, VariableModificationResultEntry> {
        [BoxGroup(InputSectionName, centerLabel: true), PropertyOrder(InputSectionOrder)]
        [SerializeField, Getter]
        string searchedVariable;
        
        protected override bool Validate() => !string.IsNullOrEmpty(searchedVariable);

        protected override void ExecuteTool() {
            ResultController.SetCurrentlySearched(searchedVariable);

            var allQuestModification = AllElementsWithInterface<StoryNode, IStoryVariableRef>()
                .Where(
                    trio => {
                        IStoryVariableRef sVariableReference = (IStoryVariableRef)trio.element;
                        bool anyMathingVariable = false;
                        foreach (Variable variable in sVariableReference.variables) {
                            if (variable.name.Contains(searchedVariable)) {
                                anyMathingVariable = true;
                                break;
                            }
                        }
                        return !trio.node.Graph.hiddenInToolWindows.HasFlagFast(EditorFinderType.QuestModification)
                               && anyMathingVariable;
                    })
                .Select(
                    trio => {
                        IStoryVariableRef sVariableReference = (IStoryVariableRef)trio.element;
                        return (trio.graph, trio.node, trio.element.GetType().Name, sVariableReference.extraInfo,
                            relatedVariables: string.Join("      ", sVariableReference.variables.Select(v =>
                                $"[{v.name.ColoredText(ARColor.EditorBlue)}] {v.type}" 
                                // in a condition, type other than const defines value source
                                // in a variable reference the value is always used as an operand
                                + (trio.element is not EditorCondition  || v.type == VariableType.Const ? $":{v.value}" : ""))),
                            contexts: string.Join(" | ", sVariableReference.optionalContext.Select(c => c.type.ToStringFast())));
                    });
            
            foreach (var valueTuple in allQuestModification.OrderBy(tuple => tuple.graph.name)) {
                ResultController.Feed(new VariableModificationResultEntry(valueTuple.graph, valueTuple.node, valueTuple.Name, valueTuple.relatedVariables, valueTuple.extraInfo, valueTuple.contexts));
            } 
        }
    }
    
    [Serializable]
    public class VariableModificationResultEntry : DefaultResultEntry {
        [SerializeField, DisplayAsString, TableColumnWidth(80)] string nodeType;
        [SerializeField, DisplayAsString(EnableRichText = true), TableColumnWidth(330)] string relatedVariables;
        [SerializeField, DisplayAsString] string extraInfo;
        [SerializeField, DisplayAsString] string contexts;

        public VariableModificationResultEntry(NodeGraph graph, StoryNode node, string stepName, string relatedVariables, string extraInfo, string contexts, string notes = "") : base(graph, node, notes){
            nodeType = stepName;
            this.relatedVariables = relatedVariables;
            this.extraInfo = extraInfo;
            this.contexts = contexts;
        }
    }
}
