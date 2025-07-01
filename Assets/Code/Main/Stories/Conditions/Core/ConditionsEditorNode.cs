using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Extensions;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps;
using XNode;

namespace Awaken.TG.Main.Stories.Conditions.Core {
    /// <summary>
    /// Represents conditions attached to a step. If the conditions aren't fulfilled,
    /// the step is not executed.
    /// </summary>
    public abstract class ConditionsEditorNode : StoryNode<EditorCondition> {
        [Input] public ConditionsEditorNode[] inputs = Array.Empty<ConditionsEditorNode>();
        [Output] public ConditionsEditorNode trueOutput;
        [Output] public ConditionsEditorNode falseOutput;

        public Node TrueTargetNode => GetOutputPort((NodePort.FieldNameCompressed)nameof(trueOutput)).ConnectedNode();
        public Node FalseTargetNode => GetOutputPort((NodePort.FieldNameCompressed)nameof(falseOutput)).ConnectedNode();
        public IEnumerable<ConditionsEditorNode> InputNodes => GetInputPort((NodePort.FieldNameCompressed)nameof(inputs)).GetInputValues<ConditionsEditorNode>();

        public bool IsConnectionNegated(ConditionsEditorNode target) {
            var conditionPort = target.GetInputPort((NodePort.FieldNameCompressed)nameof(inputs)); 
            var matchingConnection = conditionPort.GetConnections().FirstOrDefault(c => c.node == this);
            if (matchingConnection == null) {
                return false;
            }
            return matchingConnection.fieldNameCompressed.Equals(NodePort.FieldNameCompressed.FalseOutput);
        }

        public bool IsConnectionNegated(EditorStep target) {
            var conditionPort = target.ConditionPort();
            var matchingConnection = conditionPort.GetConnections().FirstOrDefault(c => c.node == this);
            if (matchingConnection == null) {
                return false;
            }
            return matchingConnection.fieldNameCompressed.Equals(NodePort.FieldNameCompressed.FalseOutput);
        }

        public bool IsConnectionNegated(SEditorStoryStartChoice target) {
            var conditionPort = target.ConditionPort();
            var matchingConnection = conditionPort.GetConnections().FirstOrDefault(c => c.node == this);
            if (matchingConnection == null) {
                return false;
            }
            return matchingConnection.fieldNameCompressed.Equals(NodePort.FieldNameCompressed.FalseOutput);
        }
        
        public abstract StoryConditions CreateRuntimeConditions(StoryGraphParser parser);
    }
}
