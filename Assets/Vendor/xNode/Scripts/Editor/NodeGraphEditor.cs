using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Vendor.xNode.Scripts.Editor;
using Vendor.xNode.Scripts.Editor.GenericMenus;
using XNode;
using Object = UnityEngine.Object;

namespace XNodeEditor {
    /// <summary> Base class to derive custom Node Graph editors from. Use this to override how graphs are drawn in the editor. </summary>
    [CustomNodeGraphEditor(typeof(NodeGraph))]
    public class NodeGraphEditor : Internal.NodeEditorBase<NodeGraphEditor, NodeGraphEditor.CustomNodeGraphEditorAttribute, NodeGraph> {
        public virtual float BlackBoardWidth => 260;
        public readonly Color blackboardColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        public bool blackBoardFolded = true;
        public Vector2 blackboardScrollPosition;

        public virtual void OnGUI() { }
        
        public virtual void OnUpdate() { }
        
        // === Blackboard
        protected virtual void DrawBlackBoard() {
            // Make sure if IsHoveringInactiveAreaCustomFunc if always set
            window.IsHoveringInactiveAreaCustomFunc -= WindowOnIsHoveringInactiveAreaCustomFunc;
            window.IsHoveringInactiveAreaCustomFunc += WindowOnIsHoveringInactiveAreaCustomFunc;
            
            if (blackBoardFolded) {
                if (GUILayout.Button("Black board >", GUILayout.Width(120))) {
                    blackBoardFolded = false;
                }
            } else {
                // Draw black board
                var rect = EditorGUILayout.BeginVertical(GUILayout.Width(BlackBoardWidth), GUILayout.ExpandHeight(true));
                EditorGUI.DrawRect(rect, blackboardColor);

                if (GUILayout.Button("Black board <", GUILayout.Width(120))) {
                    blackBoardFolded = true;
                }

                blackboardScrollPosition = EditorGUILayout.BeginScrollView(blackboardScrollPosition);
                OnBlackBoardScrollable();
                EditorGUILayout.EndScrollView();
                OnBlackBoardBottom();
                EditorGUILayout.EndVertical();
            }
        }

        protected virtual void OnBlackBoardScrollable() { }
        protected virtual void OnBlackBoardBottom () { }

        /// <summary> Called when opened by NodeEditorWindow </summary>
        public virtual void OnOpen() { }

        /// <summary> Called when NodeEditorWindow gains focus </summary>
        public virtual void OnWindowFocus() { }

        /// <summary> Called when NodeEditorWindow loses focus </summary>
        public virtual void OnWindowFocusLost() { }

        public virtual Texture2D GetGridTexture() {
            return NodeEditorPreferences.GetSettings().gridTexture;
        }

        public virtual Texture2D GetSecondaryGridTexture() {
            return NodeEditorPreferences.GetSettings().crossTexture;
        }

        /// <summary> Return default settings for this graph type. This is the settings the user will load if no previous settings have been saved. </summary>
        public virtual NodeEditorPreferences.Settings GetDefaultPreferences() {
            return new NodeEditorPreferences.Settings();
        }

        /// <summary> Returns context node menu path. Null or empty strings for hidden nodes. </summary>
        public virtual string GetNodeMenuName(Type type) {
            //Check if type has the CreateNodeMenuAttribute
            Node.CreateNodeMenuAttribute attrib;
            if (NodeEditorUtilities.GetAttrib(type, out attrib)) // Return custom path
                return attrib.menuName;
            else // Return generated path
                return NodeEditorUtilities.NodeDefaultPath(type);
        }

        /// <summary> The order by which the menu items are displayed. </summary>
        public virtual int GetNodeMenuOrder(Type type) {
            //Check if type has the CreateNodeMenuAttribute
            Node.CreateNodeMenuAttribute attrib;
            if (NodeEditorUtilities.GetAttrib(type, out attrib)) // Return custom path
                return attrib.order;
            else
                return 0;
        }

        /// <summary>
        /// Add items for the context menu when right-clicking this node.
        /// Override to add custom menu items.
        /// </summary>
        /// <param name="menu"></param>
        /// <param name="compatibleType">Use it to filter only nodes with ports value type, compatible with this type</param>
        /// <param name="direction">Direction of the compatiblity</param>
        public virtual void AddContextMenuItems(INodeGenericMenu menu, Type compatibleType = null, NodePort.IO direction = NodePort.IO.Input) {
            Vector2 pos = NodeEditorWindow.current.WindowToGridPosition(Event.current.mousePosition);

            IEnumerable<Type> nodeTypes = NodeEditorReflection.nodeTypes.OrderBy(GetNodeMenuOrder);

            if (compatibleType != null && NodeEditorPreferences.GetSettings().createFilter) {
                nodeTypes = NodeEditorUtilities.GetCompatibleNodesTypes(NodeEditorReflection.nodeTypes, compatibleType, direction);
            }

            nodeTypes = nodeTypes.Where(n => {
                var attribute = n.GetCustomAttribute<Node.CreateNodeMenuAttribute>();
                return attribute != null && attribute.graphType == target.GetType();
            });

            foreach (Type type in nodeTypes) {
                //Get node context menu path
                string path = GetNodeMenuName(type);
                if (string.IsNullOrEmpty(path)) continue;

                // Check if user is allowed to add more of given node type
                Node.DisallowMultipleNodesAttribute disallowAttrib;
                bool disallowed = false;
                if (NodeEditorUtilities.GetAttrib(type, out disallowAttrib)) {
                    int typeCount = target.nodes.Count(x => x.GetType() == type);
                    if (typeCount >= disallowAttrib.max) disallowed = true;
                }

                // Add node entry to context menu
                if (disallowed) {
                    menu.AddItem(new GUIContent(path), false, null);
                } else {
                    Type type1 = type;
                    menu.AddItem(new GUIContent(path), false, () => {
                        Node node = CreateNode(type1, pos);
                        NodeEditorWindow.current.AutoConnect(node);
                    });
                }
            }
            menu.AddSeparator("9997/");
            if (NodeEditorWindow.copyBuffer != null && NodeEditorWindow.copyBuffer.Length > 0) {
                menu.AddItem(new GUIContent("9998/Paste"), false, () => NodeEditorWindow.current.PasteNodes(pos));
            }
            else menu.AddDisabledItem(new GUIContent("9998/Paste"));
            menu.AddItem(new GUIContent("9999/Preferences"), false, NodeEditorReflection.OpenPreferences);
            menu.AddCustomContextMenuItems(target);
        }

        /// <summary> Returned gradient is used to color noodles </summary>
        /// <param name="output"> The output this noodle comes from. Never null. </param>
        /// <param name="input"> The output this noodle comes from. Can be null if we are dragging the noodle. </param>
        public virtual Gradient GetNoodleGradient(NodePort output, NodePort input) {
            Gradient grad = new Gradient();

            // If dragging the noodle, draw solid, slightly transparent
            if (input == null) {
                Color a = GetTypeColor(output.ValueType);
                grad.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(a, 0f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(0.6f, 0f) }
                );
            }
            // If normal, draw gradient fading from one input color to the other
            else {
                Color a = GetTypeColor(output.ValueType);
                Color b = GetTypeColor(input.ValueType);
                // If any port is hovered, tint white
                if (window.hoveredPort == output || window.hoveredPort == input) {
                    a = Color.Lerp(a, Color.white, 0.8f);
                    b = Color.Lerp(b, Color.white, 0.8f);
                }
                grad.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
                );
            }
            return grad;
        }

        /// <summary> Returned float is used for noodle thickness </summary>
        /// <param name="output"> The output this noodle comes from. Never null. </param>
        /// <param name="input"> The output this noodle comes from. Can be null if we are dragging the noodle. </param>
        public virtual float GetNoodleThickness(NodePort output, NodePort input) {
            return NodeEditorPreferences.GetSettings().noodleThickness;
        }

        public virtual NoodlePath GetNoodlePath(NodePort output, NodePort input) {
            return NodeEditorPreferences.GetSettings().noodlePath;
        }

        public virtual NoodleStroke GetNoodleStroke(NodePort output, NodePort input) {
            return NodeEditorPreferences.GetSettings().noodleStroke;
        }

        /// <summary> Returned color is used to color ports </summary>
        public virtual Color GetPortColor(NodePort port) {
            return GetTypeColor(port.ValueType);
        }

        /// <summary>
        /// The returned Style is used to configure the paddings and icon texture of the ports.
        /// Use these properties to customize your port style.
        ///
        /// The properties used is:
        /// <see cref="GUIStyle.padding"/>[Left and Right], <see cref="GUIStyle.normal"/> [Background] = border texture,
        /// and <seealso cref="GUIStyle.active"/> [Background] = dot texture;
        /// </summary>
        /// <param name="port">the owner of the style</param>
        /// <returns></returns>
        public virtual GUIStyle GetPortStyle(NodePort port) {
            if (port.direction == NodePort.IO.Input)
                return NodeEditorResources.styles.inputPort;

            return NodeEditorResources.styles.outputPort;
        }

        /// <summary> The returned color is used to color the background of the door.
        /// Usually used for outer edge effect </summary>
        public virtual Color GetPortBackgroundColor(NodePort port) {
            return Color.gray;
        }

        /// <summary> Returns generated color for a type. This color is editable in preferences </summary>
        public virtual Color GetTypeColor(Type type) {
            return NodeEditorPreferences.GetTypeColor(type);
        }

        /// <summary> Override to display custom tooltips </summary>
        public virtual string GetPortTooltip(NodePort port) {
            Type portType = port.ValueType;
            string tooltip = "";
            tooltip = portType.PrettyName();
            if (port.IsOutput) {
                object obj = port.node.GetValue(port);
                tooltip += " = " + (obj != null ? obj.ToString() : "null");
            }
            return tooltip;
        }

        /// <summary> Deal with objects dropped into the graph through DragAndDrop </summary>
        public virtual void OnDropObjects(UnityEngine.Object[] objects) {
            if (GetType() != typeof(NodeGraphEditor)) Debug.Log("No OnDropObjects override defined for " + GetType());
        }

        /// <summary> Create a node and save it in the graph asset </summary>
        public virtual Node CreateNode(Type type, Vector2 position) {
            return CreateNode(type, position, null);
        }
        
        public virtual Node CreateNode(Type type, Vector2 position, Action<Node> config) {
            Node node = CreateNode(type, target, position);
            if (NodeEditorPreferences.GetSettings().autoSave) AssetDatabase.SaveAssets();
            NodeEditorWindow.RepaintAll();
            config?.Invoke(node);
            return node;
        }
        
        public static Node CreateNode(Type type, NodeGraph graph) => CreateNode(type, graph, Vector2.zero);
        
        public static Node CreateNode(Type type, NodeGraph graph, Vector2 position) {
            Node.graphHotfix = graph;
            Node node = ScriptableObject.CreateInstance(type) as Node;
            Undo.RegisterCreatedObjectUndo(node, "Created node");
            Undo.RegisterCompleteObjectUndo(graph, "Created node");
            Undo.RegisterCompleteObjectUndo(node, "Created node");
            graph.AddNode(node);
            node.position = position;
            if (node.name == null || node.name.Trim() == "") node.name = NodeEditorUtilities.NodeDefaultName(type);
            if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(graph))) AssetDatabase.AddObjectToAsset(node, graph);
            return node;
        }

        /// <summary> Creates a copy of the original node in the graph </summary>
        public virtual Node CopyNode(Node original) {
            Undo.RecordObject(target, "Duplicate Node");
            Node node = target.CopyNode(original);
            Undo.RegisterCreatedObjectUndo(node, "Duplicate Node");
            node.name = original.name;
            AssetDatabase.AddObjectToAsset(node, target);
            if (NodeEditorPreferences.GetSettings().autoSave) AssetDatabase.SaveAssets();
            return node;
        }

        /// <summary> Return false for nodes that can't be removed </summary>
        public virtual bool CanRemove(Node node) {
            // Check graph attributes to see if this node is required
            Type graphType = target.GetType();
            NodeGraph.RequireNodeAttribute[] attribs = Array.ConvertAll(
                graphType.GetCustomAttributes(typeof(NodeGraph.RequireNodeAttribute), true), x => x as NodeGraph.RequireNodeAttribute);
            if (attribs.Any(x => x.Requires(node.GetType()))) {
                if (target.nodes.Count(x => x.GetType() == node.GetType()) <= 1) {
                    return false;
                }
            }
            return true;
        }

        /// <summary> Safely remove a node and all its connections. </summary>
        public virtual void RemoveNode(Node node) {
            if (!CanRemove(node)) return;

            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(node, out var guid, out long fileId);
            // Remove the node
            Undo.RecordObject(node, "Delete Node");
            Undo.RecordObject(target, "Delete Node");
            foreach (var port in node.Ports) {
                foreach (var conn in port.GetConnections().Where(n => n != null).Distinct()) {
                    Undo.RecordObject(conn.node, "Delete Node");
                }
            }
            target.RemoveNode(node);
            target.removedNodes.Add(fileId);
            Undo.DestroyObjectImmediate(node);
            if (NodeEditorPreferences.GetSettings().autoSave) AssetDatabase.SaveAssets();
        }
        
        // Helpers
        bool WindowOnIsHoveringInactiveAreaCustomFunc() {
            if (blackBoardFolded) {
                return false;
            }
            Vector2 mousePos = Event.current.mousePosition;
            Rect blackBoardRect = new Rect(0, 0, BlackBoardWidth, window.position.height);
            return blackBoardRect.Contains(mousePos);
        }

        [AttributeUsage(AttributeTargets.Class)]
        public class CustomNodeGraphEditorAttribute : Attribute, INodeEditorAttrib {
            Type inspectedType;
            public string editorPrefsKey;
            /// <summary> Tells a NodeGraphEditor which Graph type it is an editor for </summary>
            /// <param name="inspectedType">Type that this editor can edit</param>
            /// <param name="editorPrefsKey">Define unique key for unique layout settings instance</param>
            public CustomNodeGraphEditorAttribute(Type inspectedType, string editorPrefsKey = "xNode.Settings") {
                this.inspectedType = inspectedType;
                this.editorPrefsKey = editorPrefsKey;
            }

            public Type GetInspectedType() {
                return inspectedType;
            }
        }
    }
}