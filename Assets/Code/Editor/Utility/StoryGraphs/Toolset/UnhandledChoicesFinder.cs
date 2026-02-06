using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.Templates;
using UnityEditor;
using XNode;
using static Awaken.TG.Editor.Utility.StoryGraphs.Converter.GraphConverterUtils;

namespace Awaken.TG.Editor.Utility.StoryGraphs.Toolset {
    public class UnhandledChoicesFinder : StoryGraphUtilityTool<SearchResult<DefaultResultEntry>, DefaultResultEntry> {
        
        protected override bool Validate() => true;
        
        protected override void ExecuteTool() {
            var allChoices = AllElements<StoryNode, SEditorChoice>();

            foreach (var entry in allChoices) {
                var graph = entry.graph;
                var node = entry.node;
                var sChoice = entry.element;
                var text = sChoice.Text.Translate();
                
                var graphPath = AssetDatabase.GetAssetPath(graph);
                if (IsDebugOrForRemoval(graph, graphPath)) {
                    continue;
                }
                
                if (text is "(Leave)" or "(Leave.)") {
                    continue;
                }
                
                if (sChoice.TargetNode() != null) {
                    continue;
                }
                
                bool hasOnlyOneChoiceElement = node.elements.Count == 1;
                bool hasContinuationNode = node.GetPort(NodePort.FieldNameCompressed.Continuation).IsConnected;
                if (hasOnlyOneChoiceElement && hasContinuationNode) {
                    continue;
                }
                    
                ResultController.Feed(new DefaultResultEntry(graph, node, text));
            }
        }
        
        bool IsDebugOrForRemoval(NodeGraph graph, string graphPath) {
            var graphTemplate = graph as ITemplate;
            bool isDebugPath = graphPath.Contains("Obsolete") || graphPath.Contains("Debug");
            bool isDebugTemplate = graphTemplate is { TemplateType: TemplateType.Debug or TemplateType.ForRemoval };
            return isDebugPath || isDebugTemplate;
        }
    }
}