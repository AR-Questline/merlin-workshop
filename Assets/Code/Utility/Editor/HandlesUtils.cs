using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Awaken.Utility.Editor {
    public static class HandlesUtils {
        static readonly GUIContent GUIContent = new();
        static GUIStyle s_labelStyle;

        public static Rect LabelRect(Vector3 position, string text, GUIStyle labelStyle) {
            var textContent = GUIContent;
            textContent.text = text;
            return HandleUtility.WorldPointToSizedRect(position, textContent, labelStyle);
        }

        public static bool Label(Vector3 position, string text, Color textColor, GUIStyle labelStyle, out Rect rect, in float3x2? cameraData = default) {
            rect = LabelRect(position, text, labelStyle);

            if (cameraData.HasValue) {
                var camera = cameraData.Value;
                var cameraPos = camera.c0;
                var cameraForward = camera.c1;
                var toCamera = cameraPos - (float3)position;
                var dot = math.dot(toCamera, cameraForward);
                if (dot < 0.1f) {
                    return false;
                }
            }

            Handles.BeginGUI();
            EditorGUI.DrawRect(rect, Color.black);
            Handles.EndGUI();
            labelStyle.normal.textColor = textColor;
            Handles.Label(position, GUIContent, labelStyle);
            return true;
        }

        public static bool Label(Rect rect, string text, Color textColor, GUIStyle labelStyle) {
            Handles.BeginGUI();
            EditorGUI.DrawRect(rect, Color.black);
            labelStyle.normal.textColor = textColor;
            EditorGUI.LabelField(rect, text, labelStyle);
            Handles.EndGUI();
            return true;
        }

        public static bool Label(Vector3 position, string text) {
            return Label(position, text, Color.white);
        }

        public static bool Label(Vector3 position, string text, Color color) {
            return Label(position, text, color, s_labelStyle ??= new GUIStyle(EditorStyles.label), out _);
        }

        public static void DrawSphere(Vector3 position, float radius) {
            Handles.DrawWireDisc(position, Vector3.up, radius);
            Handles.DrawWireDisc(position, Vector3.right, radius);
            Handles.DrawWireDisc(position, Vector3.forward, radius);
        }

        public static void DrawSphere(Vector3 position, float radius, Color color) {
            var colorCache = Handles.color;
            Handles.color = color;
            DrawSphere(position, radius);
            Handles.color = colorCache;
        }
        
        public static void DrawFrustum(Vector3 center, Quaternion rotation, float distance, float fov, float aspect) {
            var forward = rotation * Vector3.forward;
            var right = rotation * Vector3.right;
            var up = rotation * Vector3.up;
            
            var halfWidth = Mathf.Tan(fov * Mathf.Deg2Rad * 0.5f) * distance;
            var halfHeight = halfWidth / aspect;
            
            var topLeft = center + forward * distance + up * halfHeight - right * halfWidth;
            var topRight = center + forward * distance + up * halfHeight + right * halfWidth;
            var bottomLeft = center + forward * distance - up * halfHeight - right * halfWidth;
            var bottomRight = center + forward * distance - up * halfHeight + right * halfWidth;
            
            Handles.DrawLine(center, topLeft);
            Handles.DrawLine(center, topRight);
            Handles.DrawLine(center, bottomLeft);
            Handles.DrawLine(center, bottomRight);
            Handles.DrawLine(topLeft, topRight);
            Handles.DrawLine(topRight, bottomRight);
            Handles.DrawLine(bottomRight, bottomLeft);
            Handles.DrawLine(bottomLeft, topLeft);
        }

        static Vector3[] s_triangleVertices = new Vector3[3];
        public static void DrawTriangle(Vector3 a, Vector3 b, Vector3 c) {
            s_triangleVertices[0] = a;
            s_triangleVertices[1] = b;
            s_triangleVertices[2] = c;
            Handles.DrawAAConvexPolygon(s_triangleVertices);
        }

        public static void DrawTriangle(HandlesTriangle triangle) {
            s_triangleVertices[0] = triangle.v0;
            s_triangleVertices[1] = triangle.v1;
            s_triangleVertices[2] = triangle.v2;
            Handles.DrawAAConvexPolygon(s_triangleVertices);
        }

        public struct HandlesTriangle {
            public Vector3 v0;
            public Vector3 v1;
            public Vector3 v2;

            public HandlesTriangle(Vector3 v0, Vector3 v1, Vector3 v2) {
                this.v0 = v0;
                this.v1 = v1;
                this.v2 = v2;
            }
        }
    }
}
