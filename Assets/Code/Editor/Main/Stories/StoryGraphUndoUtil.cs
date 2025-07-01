using System.Collections.Generic;
using Awaken.TG.Main.Stories.Core;
using UnityEditor;
using XNode;

namespace Awaken.TG.Editor.Main.Stories {
    public static class StoryGraphUndoUtil {
        public static void Record(Node node) {
            HashSet<Node> connectedNodes = new HashSet<Node>();
            foreach (var port in node.Ports) {
                foreach (var nodePort in port.GetConnections()) {
                    if (nodePort != null) {
                        connectedNodes.Add(nodePort.node);
                    }
                }
            }

            foreach (var n in connectedNodes) {
                Undo.RegisterCompleteObjectUndo(n, "Changing node");
            }

            if (node is StoryNode s) {
                Undo.RegisterCompleteObjectUndo(s.Graph.StringTable, "Changing node");
            }

            Undo.RegisterCompleteObjectUndo(node, "Changing node");
        }

        public static void Record(NodeElement element) {
            if (element.genericParent is StoryNode s) {
                Undo.RegisterCompleteObjectUndo(s.Graph.StringTable, "Changing element");
            }
            
            Undo.RegisterCompleteObjectUndo(element, "Changing element");
            if (element.HasTargetPort()) {
                Undo.RegisterCompleteObjectUndo(element.TargetPort().node, "Changing element");
            }

            foreach (var conditionNode in element.ConditionNodes()) {
                Undo.RegisterCompleteObjectUndo(conditionNode, "Changing element");
            }
        }
    }
}