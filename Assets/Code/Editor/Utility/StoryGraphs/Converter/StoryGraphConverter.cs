using Awaken.TG.Main.Stories.Conditions;
using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Utility.Reflections;
using Awaken.Utility.Times;
using static Awaken.TG.Editor.Utility.StoryGraphs.Converter.GraphConverterUtils;

namespace Awaken.TG.Editor.Utility.StoryGraphs.Converter {
    public static class StoryGraphConverter {
        // === Examples

        /// <summary>
        /// Example of usage
        /// Change all nodes of like AndNode with CVariable to OrNode with COncePer + COncePer (one OrNode with two COncePer elements)
        /// </summary>
        //[MenuItem("TG/StoryGraphsConverter1")]
        static void Convert1() {
            AllNodesWithElement<AndEditorNode, CEditorVariable
                >() // Obtain all nodes of type AndNode which contains element CVariable as GraphNodePair<AndNode>
                .SwapNodes( // Swap nodes (include create new node, register in graph and remove old node)
                    StartNodeSwap() // Start defining swap actions 
                        .CopyPorts() // Copy ports
                        .CopyPosition() // Copy position
                        .FromTo<AndEditorNode, OrEditorNode>() //Define source node type and target node type
                )
                .ExtractElements<OrEditorNode, CEditorVariable
                >() // From swapped nodes obtain all elements of type CVariable as GraphNodeElementTrio<OrNode, CVariable>
                .SwapElements( // Swap elements (include create new element, register in node and remove odl element)
                    StartElementSwap() // Start defining swap actions (note this is difference from StartNodeSwap)
                        .CopyField("timeSpan", "span",
                            ReflectionExtension
                                .Enum2EnumByInt<TimeSpans
                                >()) // Copy field 'timeSpan' from CVariable to field 'span' in COncePer
                        // with Enum2EnumByInt converter, in real CVariable do not contains timeSpan field but this is only example
                        .FromTo<CEditorVariable, CEditorOncePer>() //Define source element type and target element type
                )
                .ExtractNodes() // From GraphNodeElementTrio<OrNode, CVariable> extract nodes as GraphNodePair<OrNode>
                .AddElement( // Add new element to nodes (include create new element and add this element to node)
                    // (note: change GraphNodePair<OrNode> to GraphNodeElementTrio<OrNode, COncePer> but only with new elements)
                    StartElementAdd() // Start defining add action
                        .ElementOfType<CEditorOncePer>() // Define new element type
                )
                .Save(); // Save changes
        }
    }
}
