using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Debugging;
using Awaken.TG.Main.Stories.Extensions;
using UnityEngine;
using Vendor.xNode.Scripts.Attributes;
using XNode;
using Node = XNode.Node;

namespace Awaken.TG.Main.Stories.Core {
    public class NodeElement : ScriptableObject {
        
        // === References

        [HideInInspector] public StoryNode genericParent;
        [SerializeField, HideInStoryGraph] List<PortAssignment> portAssignments = new();

        public DebugInfo DebugInfo { get; } = new();
        public bool JustPasted { get; set; }
        
        public void ResetCache() {
            DebugInfo?.Clear();
        }

        public void CopyPortAssignments() {
            portAssignments = portAssignments.Select(a => new PortAssignment {localTag = a.localTag, portName = a.portName}).ToList();
        }

        // === Ports

        /// <summary>
        /// Whether this step already has a private port with the given tag.
        /// </summary>
        protected bool HasPort(string tag) {
            return portAssignments.Any(a => a.localTag == tag);
        }

        /// <summary>
        /// Returns the node connected via a tagged port.
        /// </summary>
        protected Node ConnectedNode(string portTag) {
            return PrivatePort(tag: portTag, direction: NodePort.IO.Output).ConnectedNode();
        }

        string NextFreePortID(string prefix) {
            for (int i = 1;; i++) {
                string portName = $"{prefix}:{i}";
                if (genericParent.DynamicPorts.All(p => p.fieldNameCompressed != portName)) return portName;
            }
        }

        /// <summary>
        /// Returns a private port for use by this step, creating it first if necessary.
        /// Multiple ports can be created - in this case, they will be discriminated by tags,
        /// which must be unique within a single step.
        /// </summary>
        protected NodePort PrivatePort(string tag, NodePort.IO direction, Type type = null) {
            PortAssignment assignment = portAssignments
                .FirstOrDefault(a => a.localTag == tag);
            if (assignment == null) {
                string portName = NextFreePortID(prefix: tag);
                if (direction == NodePort.IO.Output) {
                    if (type == null) type = typeof(Node);
                    genericParent.AddDynamicOutput(
                        type: type,
                        connectionType: Node.ConnectionType.Override,
                        typeConstraint: Node.TypeConstraint.None,
                        fieldName: portName);
                } else if (direction == NodePort.IO.Input) {
                    if (type == null) type = typeof(Node[]);
                    genericParent.AddDynamicInput(
                        type: type,
                        connectionType: Node.ConnectionType.Multiple,
                        typeConstraint: Node.TypeConstraint.None,
                        fieldName: portName);
                }

                assignment = new PortAssignment {localTag = tag, portName = portName};
                portAssignments.Add(item: assignment);
            }

            return genericParent.GetPort(fieldName: (NodePort.FieldNameCompressed)assignment.portName);
        }

        public void RemovePrivatePort(string portTag) {
            PortAssignment assignment = portAssignments
                .FirstOrDefault(a => a.localTag == portTag);
            if (assignment != null) {
                portAssignments.Remove(item: assignment);
                genericParent.RemoveDynamicPort(fieldName: (NodePort.FieldNameCompressed)assignment.portName);
            }
        }

        public bool HasTargetPort() {
            return HasPort("target");
        }

        // convenience method for steps that have one port pointing to a story node to jump to
        // (which is most of them)
        public NodePort TargetPort() {
            return PrivatePort("target", direction: NodePort.IO.Output);
        }

        public Node TargetNode() {
            return ConnectedNode("target");
        }

        // convenience method for conditions
        public bool HasConditionPort() {
            return HasPort("conditions");
        }

        public void RemoveConditionPort() {
            RemovePrivatePort("conditions");
        }

        public NodePort ConditionPort() {
            return PrivatePort("conditions", direction: NodePort.IO.Input, type: typeof(ConditionsEditorNode[]));
        }

        public IEnumerable<ConditionsEditorNode> ConditionNodes() {
            return ConditionPort()?.GetConnections()
                .Select(c => c.node)
                .OfType<ConditionsEditorNode>()
                ?? Enumerable.Empty<ConditionsEditorNode>();
        }

        // === Dynamic ports

        [Serializable]
        class PortAssignment {
            public string localTag;
            public string portName;
        }

        // === Callbacks

        public virtual void OnAdded(Node _) { }

        public virtual void OnRemoved(Node _) {
            foreach (PortAssignment a in portAssignments) {
                NodePort port = genericParent.GetPort(fieldName: (NodePort.FieldNameCompressed)a.portName);
                genericParent.RemoveDynamicPort(port: port);
            }
        }

#if UNITY_EDITOR
        public delegate void OnNodeDestroyed(NodeElement element);
        public static event OnNodeDestroyed OnNodeDestroyedEvent = delegate { };
        
        void OnDestroy() {
            OnNodeDestroyedEvent?.Invoke(this);
        }
#endif
    }

    public class NodeElement<T> : NodeElement where T : class {
        public T Parent => genericParent as T;
    }
}