using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Awaken.Utility.SerializableTypeReference;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace XNode {
    [Serializable]
    public class NodePort {
        public enum IO : byte {
            Input,
            Output
        }

        public int ConnectionCount {
            get { return connections.Count; }
        }

        /// <summary> Return the first non-null connection </summary>
        public NodePort Connection {
            get {
                for (int i = 0; i < connections.Count; i++) {
                    if (connections[i].node != null) {
                        return connections[i].Port;
                    }
                }

                return null;
            }
        }

        public IO direction {
            get { return _direction; }
            internal set { _direction = value; }
        }

        public Node.ConnectionType connectionType {
            get { return _connectionType; }
            internal set { _connectionType = value; }
        }

        public Node.TypeConstraint typeConstraint {
            get { return _typeConstraint; }
            internal set { _typeConstraint = value; }
        }

        /// <summary> Is this port connected to anytihng? </summary>
        public bool IsConnected {
            get { return connections.Count != 0; }
        }

        public bool IsInput {
            get { return direction == IO.Input; }
        }

        public bool IsOutput {
            get { return direction == IO.Output; }
        }

        public FieldNameCompressed fieldNameCompressed {
            get { return _fieldNameCompressed; }
        }
        
        public string fieldName {
            get { return _fieldNameCompressed; }
        }

        public Node node {
            get { return _node; }
        }

        public bool IsDynamic {
            get { return _dynamic; }
        }

        public bool IsStatic {
            get { return !_dynamic; }
        }

        public Type ValueType {
            get {
                if (valueType == null && _typeQualifiedNameCompressed.nameCode > 0) valueType = Type.GetType(_typeQualifiedNameCompressed, false);
                return valueType;
            }
            set {
                valueType = value;
                if (value != null) _typeQualifiedNameCompressed = (TypeQualifiedNameCompressed)value.AssemblyQualifiedName;
            }
        }

        private Type valueType;

        [SerializeField] private Node _node;
        [SerializeField] public List<PortConnection> connections = new List<PortConnection>();
        [SerializeField] private SerializableTypeReference[] _allowedTypes;
        [SerializeField] private IO _direction;
        [SerializeField] private Node.ConnectionType _connectionType;
        [SerializeField] private Node.TypeConstraint _typeConstraint;
        [SerializeField] public FieldNameCompressed _fieldNameCompressed;
        [SerializeField] public TypeQualifiedNameCompressed _typeQualifiedNameCompressed;
        [SerializeField] private bool _dynamic;

        /*public NodePort CreateCopyFromOld() {
            return new NodePort() {
                _fieldName = fieldName,
                _fieldNameCompressed = (FieldNameCompressed)_fieldName,
                _node = _node,
                _typeQualifiedName = _typeQualifiedName,
                _typeQualifiedNameCompressed = (TypeQualifiedNameCompressed)_typeQualifiedName,
                connections = CopyConnections(connections),
                _direction = _direction,
                _connectionType = _connectionType,
                _typeConstraint = _typeConstraint,
                _dynamic = _dynamic,
                _allowedTypes = Awaken.Utility.Collections.ArrayUtils.CreateCopy(_allowedTypes),
            };

            static List<PortConnection> CopyConnections(List<PortConnection> connections) {
                var result = new List<PortConnection>(connections.Count);
                foreach (var connection in connections) {
                    result.Add(new PortConnection() {
                        fieldName = connection.fieldName,
                        fieldNameCompressed = (NodePort.FieldNameCompressed)connection.fieldName,
                        node = connection.node,
                        reroutePoints = new List<Vector2>(connection.reroutePoints)
                    });
                }

                return result;
            }
        }
        public NodePort Recalculate() {
            _fieldNameCompressed = (FieldNameCompressed)_fieldName;
            _typeQualifiedNameCompressed = (TypeQualifiedNameCompressed)_typeQualifiedName;
            return this;
        }*/
        /// <summary> Construct a static targetless nodeport. Used as a template. </summary>
        public NodePort(FieldInfo fieldInfo) {
            _fieldNameCompressed = (FieldNameCompressed)fieldInfo.Name;
            ValueType = fieldInfo.FieldType;
            _dynamic = false;
            var attribs = fieldInfo.GetCustomAttributes(false);
            for (int i = 0; i < attribs.Length; i++) {
                if (attribs[i] is Node.InputAttribute inputAttribute) {
                    _direction = IO.Input;
                    _connectionType = inputAttribute.connectionType;
                    _typeConstraint = inputAttribute.typeConstraint;
                    _allowedTypes = new SerializableTypeReference[inputAttribute.allowedTypes.Length];
                    for (int j = 0; j < inputAttribute.allowedTypes.Length; j++) {
                        _allowedTypes[j] = inputAttribute.allowedTypes[j];
                    }
                } else if (attribs[i] is Node.OutputAttribute outputAttribute) {
                    _direction = IO.Output;
                    _connectionType = outputAttribute.connectionType;
                    _typeConstraint = outputAttribute.typeConstraint;
                    _allowedTypes = new SerializableTypeReference[outputAttribute.allowedTypes.Length];
                    for (int j = 0; j < outputAttribute.allowedTypes.Length; j++) {
                        _allowedTypes[j] = outputAttribute.allowedTypes[j];
                    }
                }
            }
        }

        /// <summary> Copy a nodePort but assign it to another node. </summary>
        public NodePort(NodePort nodePort, Node node) {
            _fieldNameCompressed = nodePort._fieldNameCompressed;
            //_fieldName = nodePort._fieldNameCompressed;
            ValueType = nodePort.valueType;
            _direction = nodePort.direction;
            _dynamic = nodePort._dynamic;
            _connectionType = nodePort._connectionType;
            _typeConstraint = nodePort._typeConstraint;
            _allowedTypes = nodePort._allowedTypes;
            _node = node;
        }

        /// <summary> Construct a dynamic port. Dynamic ports are not forgotten on reimport, and is ideal for runtime-created ports. </summary>
        public NodePort(string fieldName, Type type, IO direction, Node.ConnectionType connectionType, Node.TypeConstraint typeConstraint, Node node, SerializableTypeReference[] allowedTypes = null) {
            _fieldNameCompressed = (FieldNameCompressed)fieldName;
            //_fieldName = fieldName;
            this.ValueType = type;
            _direction = direction;
            _node = node;
            _dynamic = true;
            _connectionType = connectionType;
            _typeConstraint = typeConstraint;
            if (allowedTypes != null) {
                _allowedTypes = allowedTypes;
            }
        }
        
        public NodePort(){}

        /// <summary> Checks all connections for invalid references, and removes them. </summary>
        public void VerifyConnections() {
            for (int i = connections.Count - 1; i >= 0; i--) {
                if (connections[i].node != null &&
                    !string.IsNullOrEmpty(connections[i].fieldNameCompressed) &&
                    connections[i].node.GetPort(connections[i].fieldNameCompressed) != null)
                    continue;
                connections.RemoveAt(i);
            }
        }

        /// <summary> Return the output value of this node through its parent nodes GetValue override method. </summary>
        /// <returns> <see cref="Node.GetValue(NodePort)"/> </returns>
        public object GetOutputValue() {
            if (direction == IO.Input) return null;
            return node.GetValue(this);
        }

        /// <summary> Return the output value of the first connected port. Returns null if none found or invalid.</summary>
        /// <returns> <see cref="NodePort.GetOutputValue"/> </returns>
        public object GetInputValue() {
            NodePort connectedPort = Connection;
            if (connectedPort == null) return null;
            return connectedPort.GetOutputValue();
        }

        /// <summary> Return the output values of all connected ports. </summary>
        /// <returns> <see cref="NodePort.GetOutputValue"/> </returns>
        public object[] GetInputValues() {
            object[] objs = new object[ConnectionCount];
            for (int i = 0; i < ConnectionCount; i++) {
                NodePort connectedPort = connections[i].Port;
                if (connectedPort == null) {
                    // if we happen to find a null port, remove it and look again
                    connections.RemoveAt(i);
                    i--;
                    continue;
                }

                objs[i] = connectedPort.GetOutputValue();
            }

            return objs;
        }

        /// <summary> Return the output value of the first connected port. Returns null if none found or invalid. </summary>
        /// <returns> <see cref="NodePort.GetOutputValue"/> </returns>
        public T GetInputValue<T>() {
            object obj = GetInputValue();
            return obj is T ? (T)obj : default(T);
        }

        /// <summary> Return the output values of all connected ports. </summary>
        /// <returns> <see cref="NodePort.GetOutputValue"/> </returns>
        public T[] GetInputValues<T>() {
            object[] objs = GetInputValues();
            T[] ts = new T[objs.Length];
            for (int i = 0; i < objs.Length; i++) {
                if (objs[i] is T) ts[i] = (T)objs[i];
            }

            return ts;
        }

        /// <summary> Return true if port is connected and has a valid input. </summary>
        /// <returns> <see cref="NodePort.GetOutputValue"/> </returns>
        public bool TryGetInputValue<T>(out T value) {
            object obj = GetInputValue();
            if (obj is T) {
                value = (T)obj;
                return true;
            } else {
                value = default(T);
                return false;
            }
        }

        /// <summary> Return the sum of all inputs. </summary>
        /// <returns> <see cref="NodePort.GetOutputValue"/> </returns>
        public float GetInputSum(float fallback) {
            object[] objs = GetInputValues();
            if (objs.Length == 0) return fallback;
            float result = 0;
            for (int i = 0; i < objs.Length; i++) {
                if (objs[i] is float) result += (float)objs[i];
            }

            return result;
        }

        /// <summary> Return the sum of all inputs. </summary>
        /// <returns> <see cref="NodePort.GetOutputValue"/> </returns>
        public int GetInputSum(int fallback) {
            object[] objs = GetInputValues();
            if (objs.Length == 0) return fallback;
            int result = 0;
            for (int i = 0; i < objs.Length; i++) {
                if (objs[i] is int) result += (int)objs[i];
            }

            return result;
        }

        /// <summary> Connect this <see cref="NodePort"/> to another </summary>
        /// <param name="port">The <see cref="NodePort"/> to connect to</param>
        public void Connect(NodePort port) {
            if (connections == null) connections = new List<PortConnection>();
            if (port == null) {
                Debug.LogWarning("Cannot connect to null port");
                return;
            }

            if (port == this) {
                Debug.LogWarning("Cannot connect port to self.");
                return;
            }

            if (IsConnectedTo(port)) {
                Debug.LogWarning("Port already connected. ");
                return;
            }

            if (direction == port.direction) {
                Debug.LogWarning("Cannot connect two " + (direction == IO.Input ? "input" : "output") + " connections");
                return;
            }
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(node, "Connect Port");
            UnityEditor.Undo.RecordObject(port.node, "Connect Port");
#endif
            if (port.connectionType == Node.ConnectionType.Override && port.ConnectionCount != 0) {
                port.ClearConnections();
            }

            if (connectionType == Node.ConnectionType.Override && ConnectionCount != 0) {
                ClearConnections();
            }

            connections.Add(new PortConnection(port));
            if (port.connections == null) port.connections = new List<PortConnection>();
            if (!port.IsConnectedTo(this)) port.connections.Add(new PortConnection(this));
            node.OnCreateConnection(this, port);
            port.node.OnCreateConnection(this, port);
        }

        public List<NodePort> GetConnections() {
            List<NodePort> result = new List<NodePort>();
            for (int i = 0; i < connections.Count; i++) {
                NodePort port = GetConnection(i);
                if (port != null) result.Add(port);
            }

            return result;
        }

        public NodePort GetConnection(int i) {
            //If the connection is broken for some reason, remove it.
            if (connections[i].node == null || connections[i].fieldNameCompressed.Equals(FieldNameCompressed.Null)) {
                connections.RemoveAt(i);
                return null;
            }

            NodePort port = connections[i].node.GetPort(connections[i].fieldNameCompressed);
            if (port == null) {
                connections.RemoveAt(i);
                return null;
            }

            return port;
        }

        /// <summary> Get index of the connection connecting this and specified ports </summary>
        public int GetConnectionIndex(NodePort port) {
            for (int i = 0; i < ConnectionCount; i++) {
                if (connections[i].Port == port) return i;
            }

            return -1;
        }

        public bool IsConnectedTo(NodePort port) {
            for (int i = 0; i < connections.Count; i++) {
                if (connections[i].Port == port) return true;
            }

            return false;
        }

        /// <summary> Returns true if this port can connect to specified port </summary>
        public bool CanConnectTo(NodePort port) {
            // Figure out which is input and which is output
            NodePort input = null, output = null;
            if (IsInput) input = this;
            else output = this;
            if (port.IsInput) input = port;
            else output = port;
            // If there isn't one of each, they can't connect
            if (input == null || output == null) return false;
            // Check input type constraints
            if (input.typeConstraint == XNode.Node.TypeConstraint.Inherited) {
                if ((_allowedTypes?.Length ?? 0) > 0) {
                    if (!_allowedTypes.Any(t => ((Type)t).IsAssignableFrom(output.ValueType))) {
                        return false;
                    }
                } else {
                    if (!ValueType.IsAssignableFrom(output.ValueType)) {
                        return false;
                    }
                }
            }

            if (input.typeConstraint == XNode.Node.TypeConstraint.Strict) {
                if ((_allowedTypes?.Length ?? 0) > 0) {
                    if (_allowedTypes.All(t => t != output.ValueType)) {
                        return false;
                    }
                } else {
                    if (ValueType != output.ValueType) {
                        return false;
                    }
                }
            }

            if (input.typeConstraint == XNode.Node.TypeConstraint.InheritedInverse && !output.ValueType.IsAssignableFrom(input.ValueType)) return false;
            // Check output type constraints
            if (output.typeConstraint == XNode.Node.TypeConstraint.Inherited) {
                if ((_allowedTypes?.Length ?? 0) > 0) {
                    if (!_allowedTypes.Any(t => ((Type)t).IsAssignableFrom(input.ValueType))) {
                        return false;
                    }
                } else {
                    if (!ValueType.IsAssignableFrom(input.ValueType)) {
                        return false;
                    }
                }
            }

            if (output.typeConstraint == XNode.Node.TypeConstraint.Strict) {
                if ((_allowedTypes?.Length ?? 0) > 0) {
                    if (_allowedTypes.All(t => t != input.ValueType)) {
                        return false;
                    }
                } else {
                    if (ValueType != input.ValueType) {
                        return false;
                    }
                }
            }

            if (output.typeConstraint == XNode.Node.TypeConstraint.InheritedInverse && !output.ValueType.IsAssignableFrom(input.ValueType)) return false;
            // Success
            return true;
        }

        /// <summary> Disconnect this port from another port </summary>
        public void Disconnect(NodePort port) {
            // Remove this ports connection to the other
            for (int i = connections.Count - 1; i >= 0; i--) {
                if (connections[i].Port == port) {
                    connections.RemoveAt(i);
                }
            }

            if (port != null) {
                // Remove the other ports connection to this port
                for (int i = 0; i < port.connections.Count; i++) {
                    if (port.connections[i].Port == this) {
                        port.connections.RemoveAt(i);
                    }
                }
            }

            // Trigger OnRemoveConnection
            node.OnRemoveConnection(this);
            if (port != null) port.node.OnRemoveConnection(port);
        }

        /// <summary> Disconnect this port from another port </summary>
        public void Disconnect(int i) {
            // Remove the other ports connection to this port
            NodePort otherPort = connections[i].Port;
            if (otherPort != null) {
                for (int k = 0; k < otherPort.connections.Count; k++) {
                    if (otherPort.connections[k].Port == this) {
                        otherPort.connections.RemoveAt(i);
                    }
                }
            }

            // Remove this ports connection to the other
            connections.RemoveAt(i);

            // Trigger OnRemoveConnection
            node.OnRemoveConnection(this);
            if (otherPort != null) otherPort.node.OnRemoveConnection(otherPort);
        }

        public void ClearConnections() {
            while (connections.Count > 0) {
                Disconnect(connections[0].Port);
            }
        }

#if UNITY_EDITOR
        /// <summary> Get reroute points for a given connection. This is used for organization </summary>
        public List<Vector2> EDITOR_GetReroutePoints(int index) {
#if !ADDRESSABLES_BUILD
            return connections[index].EDITOR_GetReroutePoints();
#else
            return null;
#endif
        }
#endif

        /// <summary> Swap connections with another node </summary>
        public void SwapConnections(NodePort targetPort) {
            int aConnectionCount = connections.Count;
            int bConnectionCount = targetPort.connections.Count;

            List<NodePort> portConnections = new List<NodePort>();
            List<NodePort> targetPortConnections = new List<NodePort>();

            // Cache port connections
            for (int i = 0; i < aConnectionCount; i++)
                portConnections.Add(connections[i].Port);

            // Cache target port connections
            for (int i = 0; i < bConnectionCount; i++)
                targetPortConnections.Add(targetPort.connections[i].Port);

            ClearConnections();
            targetPort.ClearConnections();

            // Add port connections to targetPort
            for (int i = 0; i < portConnections.Count; i++)
                targetPort.Connect(portConnections[i]);

            // Add target port connections to this one
            for (int i = 0; i < targetPortConnections.Count; i++)
                Connect(targetPortConnections[i]);
        }

        /// <summary> Copy all connections pointing to a node and add them to this one </summary>
        public void AddConnections(NodePort targetPort) {
            int connectionCount = targetPort.ConnectionCount;
            for (int i = 0; i < connectionCount; i++) {
                NodePort otherPort = targetPort.connections[i].Port;
                Connect(otherPort);
            }
        }

        /// <summary> Move all connections pointing to this node, to another node </summary>
        public void MoveConnections(NodePort targetPort) {
            int connectionCount = connections.Count;

            // Add connections to target port
            for (int i = 0; i < connectionCount; i++) {
                NodePort otherPort = targetPort.connections[i].Port;
                Connect(otherPort);
            }

            ClearConnections();
        }

        /// <summary> Swap connected nodes from the old list with nodes from the new list </summary>
        public void Redirect(List<Node> oldNodes, List<Node> newNodes) {
            for (int i = 0; i < connections.Count; i++) {
                PortConnection connection = connections[i];
                int index = oldNodes.IndexOf(connection.node);
                if (index >= 0) {
                    connection.node = newNodes[index];
                    connections[i] = connection;
                }
            }
        }

        public void CopyPort(NodePort source) {
            if (source.IsOutput) {
                CopyOutputPort(source);
            } else {
                CopyInputPort(source);
            }
        }

        public void CopyOutputPort(NodePort source) {
            ValueType = source.valueType;
            _direction = source.direction;
            _dynamic = source._dynamic;
            _connectionType = source._connectionType;
            _typeConstraint = source._typeConstraint;
            _node = source._node;
            ClearConnections();
            foreach (var sourceConnection in source.connections) {
                Connect(sourceConnection.node.GetInputPort(sourceConnection.fieldNameCompressed));
            }
        }

        public void CopyInputPort(NodePort source) {
            ValueType = source.valueType;
            _direction = source.direction;
            _dynamic = source._dynamic;
            _connectionType = source._connectionType;
            _typeConstraint = source._typeConstraint;
            _node = source._node;
            ClearConnections();
            foreach (var sourceConnection in source.connections) {
                Connect(sourceConnection.node.GetOutputPort(sourceConnection.fieldNameCompressed));
            }
        }

        [Serializable]
        public struct PortConnection {
            [SerializeField] public FieldNameCompressed fieldNameCompressed;
            [SerializeField] public Node node;
            public NodePort Port => GetPort();

#if UNITY_EDITOR && !ADDRESSABLES_BUILD
            /// <summary> Extra connection path points for organization </summary>
            [SerializeField] List<Vector2> EDITOR_reroutePoints;

            public List<Vector2> EDITOR_GetReroutePoints() {
                if (EDITOR_reroutePoints == null) {
                    EDITOR_reroutePoints = new List<Vector2>();
                }
                return EDITOR_reroutePoints;
            }
#endif

            public PortConnection(NodePort port) {
                node = port.node;
                fieldNameCompressed = port.fieldNameCompressed;
#if UNITY_EDITOR && !ADDRESSABLES_BUILD
                EDITOR_reroutePoints = new List<Vector2>();
#endif
            }

            /// <summary> Returns the port that this <see cref="PortConnection"/> points to </summary>
            private NodePort GetPort() {
                if (node == null || fieldNameCompressed.Equals(FieldNameCompressed.Null)) return null;
                return node.GetPort(fieldNameCompressed);
            }
        }

        [Serializable]
        public struct TypeQualifiedNameCompressed {
            static readonly Dictionary<byte, string> NameCodeToNameMap = new() {
                { 0, $"{nameof(TypeQualifiedNameCompressed)}_Invalid_Type" },
                { 1, "Awaken.TG.Main.Stories.Core.ChapterEditorNode, TG.Main, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null" },
                { 2, "Awaken.TG.Main.Stories.Core.ChapterEditorNode[], TG.Main, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null" },
                { 3, "Awaken.TG.Main.Stories.Conditions.Core.ConditionsEditorNode[], TG.Main, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null" },
                { 4, "XNode.Node, XNode, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null" },
                { 5, "Awaken.TG.Main.Stories.Conditions.Core.ConditionsEditorNode, TG.Main, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null" },
                { 6, "Awaken.TG.Main.Stories.Core.FightNode, TG.Main, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null" },
                { byte.MaxValue, string.Empty },
            };

            static readonly Dictionary<string, byte> NameToNameCodeMap = NameCodeToNameMap.InvertDictionary();

            public byte nameCode;
            public string NamePreview => ToTypeName();
            public TypeQualifiedNameCompressed(string name) {
                if (string.IsNullOrEmpty(name)) {
                    this.nameCode = NameToNameCodeMap[string.Empty];
                    return;
                }
                if (NameToNameCodeMap.TryGetValue(name, out var nameCode) == false) {
                    nameCode = 0;
                    Log.Important?.Error($"No name code for type {name}");
                }

                this.nameCode = nameCode;
            }

            public string ToTypeName() {
                if (NameCodeToNameMap.TryGetValue(nameCode, out var name)) {
                    return name;
                }

                return $"{nameof(TypeQualifiedNameCompressed)}_Invalid_Type";
            }

            public static implicit operator string(TypeQualifiedNameCompressed compressed) => compressed.ToTypeName();
            public static explicit operator TypeQualifiedNameCompressed(string name) => new(name);
        }

        [Serializable]
        public struct FieldNameCompressed : IEquatable<FieldNameCompressed> {
            const byte DynamicInputPortNameCode = 244;

            static readonly StringBuilder NameStringBuilder = new StringBuilder();
            
            static readonly Dictionary<byte, string> NameCodeToNameMap = new() {
                { 0, $"{nameof(FieldNameCompressed)}_NoName" },
                { 1, "continuation" },
                { 2, "link" },
                { 3, "conditions" },
                { 4, "target" },
                { 5, "chapter" },
                { 6, "inputs" },
                { 7, "trueOutput" },
                { 8, "falseOutput" },
                { 9, "Success" },
                { 10, "failure" },
                { 11, "retreat" },
                { 12, "enableChoices" },
                { DynamicInputPortNameCode, Node.DynamicInputFieldNameBase }
            };

            static readonly Dictionary<string, byte> NameToNameCodeMap = NameCodeToNameMap.InvertDictionary();

            public static FieldNameCompressed Null => new() { nameCode = default, countValue = default };
            public static FieldNameCompressed Continuation = new("continuation");
            public static FieldNameCompressed Link = new("link");
            public static FieldNameCompressed Conditions = new("conditions");
            public static FieldNameCompressed Target = new("target");
            public static FieldNameCompressed Chapter = new("chapter");
            public static FieldNameCompressed Inputs = new("inputs");
            public static FieldNameCompressed TrueOutput = new("trueOutput");
            public static FieldNameCompressed FalseOutput = new("falseOutput");
            public static FieldNameCompressed Success = new("Success");
            public static FieldNameCompressed Failure = new("failure");
            public static FieldNameCompressed Retreat = new("retreat");

            public byte nameCode;
            public byte countValue;
            public string NamePreview => ToFieldNameString();
            
            public FieldNameCompressed(string fieldName) {
                if (fieldName.StartsWith(Node.DynamicInputFieldNameBase)) {
                    nameCode = DynamicInputPortNameCode;
                    int count = int.Parse(fieldName.Substring(Node.DynamicInputFieldNameBase.Length, fieldName.Length - Node.DynamicInputFieldNameBase.Length));
                    if (count > byte.MaxValue) {
                        Log.Important?.Error($"Count {count} in {fieldName} does not fit in byte.MaxValue: {byte.MaxValue}");
                        count = byte.MaxValue;
                    }

                    countValue = (byte)count;
                    return;
                }
                
                countValue = 0;
                var fieldNameParts = fieldName.Split(':');
                var name = fieldNameParts[0];
                if (NameToNameCodeMap.TryGetValue(name, out var matchingNameCode) == false) {
                    Log.Important?.Error($"There is no code for field name {name}. Please add name code in {nameof(FieldNameCompressed.NameCodeToNameMap)} dictionary");
                    nameCode = byte.MaxValue;
                    return;
                }

                nameCode = matchingNameCode;
                if (fieldNameParts.Length > 1) {
                    if (int.TryParse(fieldNameParts[1], out int count) == false) {
                        Log.Important?.Error($"Cannot parse {fieldNameParts[1]} in {fieldName} as integer");
                        return;
                    }

                    if (count > byte.MaxValue) {
                        Log.Important?.Error($"Count {count} in {fieldName} does not fit in byte.MaxValue: {byte.MaxValue}");
                        count = byte.MaxValue;
                    }

                    countValue = (byte)count;
                }
            }

            public string ToFieldNameString() {
                NameStringBuilder.Append(NameCodeToNameMap[nameCode]);
                string separator = nameCode == DynamicInputPortNameCode ? string.Empty : ":";
                if (countValue != 0) {
                    NameStringBuilder.Append(separator);
                    NameStringBuilder.Append(countValue);
                }

                var fieldName = NameStringBuilder.ToString();
                NameStringBuilder.Clear();
                return fieldName;
            }

            public static explicit operator FieldNameCompressed(string fieldNameString) => new(fieldNameString);
            public static implicit operator string(FieldNameCompressed fieldName) => fieldName.ToFieldNameString();

            public bool Equals(FieldNameCompressed other) {
                return nameCode == other.nameCode & countValue == other.countValue;
            }

            public override bool Equals(object obj) {
                return obj is FieldNameCompressed other && Equals(other);
            }

            public override int GetHashCode() {
                return HashCode.Combine(nameCode, countValue);
            }
        }
    }
}