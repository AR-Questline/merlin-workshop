using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Utility;
using Awaken.TG.Editor.Utility.StoryGraphs;
using Awaken.TG.Main.Stories.Core;
using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace Awaken.TG.Editor.Main.Stories {
    [CustomNodeEditor(typeof(CommentNode))]
    public class CommentNodeEditor : NodeEditor {
        CommentNode Target => (CommentNode) target;
        static readonly List<string> AdditionalExcludes = new List<string> {
            nameof(CommentNode.width), nameof(CommentNode.height), nameof(CommentNode.tint), nameof(CommentNode.nodeType)
        };
        
        bool _isDragging;
        
        protected override void PostNameHeaderGUI() {
            if (Target.tint == default) {
                Target.tint = new(0.1f, 0.1f, 0.1f, 0.3f);
            }
            Target.tint = EditorGUILayout.ColorField(GUIContent.none, Target.tint, false, true, false, GUILayout.Width(25));
        }

        public override float PostNameHeaderWidth => 50;
        
        public override void OnBodyGUI() {
            serializedObject.Update();
            DrawAll();
            serializedObject.ApplyModifiedProperties();
        }

        void DrawAll() {
            Event e = Event.current;
            if (e.type == EventType.MouseDrag) {
                if (_isDragging) {
                    Target.width = Mathf.Max(200, (int) e.mousePosition.x + 16);
                    Target.height = Mathf.Max(54, (int) e.mousePosition.y - 34);
                    NodeEditorWindow.current.Repaint();
                }
            } else if (e.type == EventType.MouseDown) {
                // Ignore everything except left clicks
                if (e.button != 0) return;
                if (NodeEditorWindow.current.nodeSizes.TryGetValue(target, out var size)) {
                    // Mouse position checking is in node local space
                    Rect lowerRight = new Rect(size.x - 34, size.y - 34, 30, 30);
                    if (lowerRight.Contains(e.mousePosition)) _isDragging = true;
                }
            } else if (e.type == EventType.MouseUp) {
                _isDragging = false;
                // Select nodes inside the group
                if (Selection.Contains(target)) {
                    var selection = Selection.objects.ToHashSet();
                    // Select Nodes
                    selection.UnionWith(Target.ContainedNodes());
                    Selection.objects = selection.ToArray();
                }
            } else if (e.type == EventType.Repaint) {
                // Move to bottom
                if (target.graph.nodes.IndexOf(item: target) != 0) {
                    target.graph.nodes.Remove(item: target);
                    target.graph.nodes.Insert(0, item: target);
                }

                // Add scale cursors
                if (NodeEditorWindow.current.nodeSizes.TryGetValue(target, out var size)) {
                    Rect lowerRight = new Rect(target.position, new Vector2(30, 30));
                    lowerRight.y += size.y - 34;
                    lowerRight.x += size.x - 34;
                    lowerRight = NodeEditorWindow.current.GridToWindowRect(lowerRight);
                    NodeEditorWindow.current.onLateGUI += () => AddMouseRect(lowerRight);
                }
            }
            
            EditorGUIUtility.labelWidth = 84;

            List<string> excludes = new List<string> {"m_Script", "graph", "position", "ports", "parent" };
            excludes.AddRange(AdditionalExcludes);

            // draw node content
            NodeGUIUtil.DrawNodePropertiesExcept(serializedObject, Target, excludes);
            
            EditorGUILayout.Space(Target.height);
        }
        
        public override int GetWidth() {
            return Target.width;
        }

        public override Color GetTint() {
            return Target.tint;
        }
        
        static void AddMouseRect(Rect rect) {
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeUpLeft);
        }
    }
}