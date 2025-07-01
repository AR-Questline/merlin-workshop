using Awaken.TG.Utility;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths;
using Awaken.Utility.Maths.Data;
using Sirenix.OdinInspector.Editor;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Awaken.Utility.Editor.Maths {
    [CustomEditor(typeof(Polygon2DAuthoring))]
    public class Polygon2DAuthoringEditor : OdinEditor {
        const float HandleSizeMultiplier = 0.35f;

        bool _editing;

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            _editing = EditorGUILayout.Toggle("Editing", _editing);
        }

        public void OnSceneGUI(float bottom, float top, bool? editingOverride = null, Color? colorOverride = null) {
            OnSceneGUI(bottom, editingOverride, colorOverride);
            OnSceneGUI(top, false, colorOverride);
            DrawVolumeLines(bottom, top, colorOverride);
        }

        public void OnSceneGUI() {
            OnSceneGUI(0);
        }

        public void OnSceneGUI(float y, bool? editingOverride = null, Color? colorOverride = null) {
            var localToWorld = ((Polygon2DAuthoring)target).transform.localToWorldMatrix;

            if (editingOverride ?? _editing) {
                EditingControl(y, localToWorld);
            }

            Handles.color = colorOverride ?? serializedObject.FindProperty("_gizmosColor").colorValue;
            DrawLines(y, localToWorld);

            DrawBounds(y, localToWorld);
        }

        void EditingControl(float y, Matrix4x4 localToWorld) {
            var polygonLocalPoints = serializedObject.FindProperty("_polygonLocalPoints");
            var currentEvent = Event.current;

            if (currentEvent.shift || currentEvent.control) {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            }

            EditorGUI.BeginChangeCheck();

            if (currentEvent.control && currentEvent.shift) {
                var toRemove = -1;
                Handles.color = Color.red;
                for (int i = 0; i < polygonLocalPoints.arraySize; i++) {
                    var item = polygonLocalPoints.GetArrayElementAtIndex(i);
                    var position = item.vector2Value;
                    var position3D = localToWorld.MultiplyPoint3x4(position.X0Y());
                    position3D.y = y;

                    var size = HandleUtility.GetHandleSize(position3D) * HandleSizeMultiplier;
                    if (Handles.Button(position3D, Quaternion.identity, size, size, Handles.SphereHandleCap)) {
                        toRemove = i;
                    }
                }
                if (toRemove != -1) {
                    polygonLocalPoints.DeleteArrayElementAtIndex(toRemove);
                    GUI.changed = true;
                }
            } else if (currentEvent.control) {
                Handles.color = Color.gray;
                var closestPoint = 0;
                var closestDistance = float.MaxValue;

                var eventRay = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
                var worldClickPoint = eventRay.GetPoint(100);
                if (Physics.Raycast(eventRay, out var hit)) {
                    worldClickPoint = hit.point;
                }
                var localClickPoint = localToWorld.inverse.MultiplyPoint3x4(worldClickPoint).XZ();

                for (int i = 0; i < polygonLocalPoints.arraySize; i++) {
                    var item = polygonLocalPoints.GetArrayElementAtIndex(i);
                    var position = item.vector2Value;
                    var position3D = localToWorld.MultiplyPoint3x4(position.X0Y());
                    position3D.y = y;

                    Handles.SphereHandleCap(0,
                        position3D,
                        Quaternion.identity,
                        HandleUtility.GetHandleSize(position3D) * HandleSizeMultiplier,
                        EventType.Repaint);

                    var nextIndex = (i + 1) % polygonLocalPoints.arraySize;
                    var nextLocalPoint = polygonLocalPoints.GetArrayElementAtIndex(nextIndex).vector2Value;

                    Algorithms2D.DistanceToLineSegmentSq(localClickPoint,
                        new LineSegment2D(position, nextLocalPoint), out var distance);
                    if (distance < closestDistance) {
                        closestDistance = distance;
                        closestPoint = nextIndex;
                    }
                }

                if (currentEvent.type == EventType.MouseDown) {
                    polygonLocalPoints.InsertArrayElementAtIndex(closestPoint);
                    polygonLocalPoints.GetArrayElementAtIndex(closestPoint).vector2Value = localClickPoint;
                    currentEvent.Use();
                    GUI.changed = true;
                }
            } else {
                for (int i = 0; i < polygonLocalPoints.arraySize; i++) {
                    var item = polygonLocalPoints.GetArrayElementAtIndex(i);
                    var position = item.vector2Value;
                    var position3D = localToWorld.MultiplyPoint3x4(position.X0Y());
                    position3D.y = y;

                    EditorGUI.BeginChangeCheck();
                    var newPosition = Handles.PositionHandle(position3D, Quaternion.identity);
                    if (EditorGUI.EndChangeCheck()) {
                        item.vector2Value = localToWorld.inverse.MultiplyPoint3x4(newPosition).XZ();
                        GUI.changed = true;
                    }
                    Handles.Label(position3D, i.ToString());
                }
            }

            if (EditorGUI.EndChangeCheck()) {
                serializedObject.ApplyModifiedProperties();
            }
        }

        void DrawLines(float y, Matrix4x4 localToWorld) {
            var polygonLocalPoints = serializedObject.FindProperty("_polygonLocalPoints");
            if (polygonLocalPoints.arraySize < 2) {
                return;
            }
            var prevLoc = polygonLocalPoints.GetArrayElementAtIndex(polygonLocalPoints.arraySize - 1).vector2Value;
            var previousPoint = localToWorld.MultiplyPoint3x4(prevLoc.X0Y());
            previousPoint.y = y;

            for (int i = 0; i < polygonLocalPoints.arraySize; i++) {
                var nextLoc = polygonLocalPoints.GetArrayElementAtIndex(i).vector2Value;
                var nextPoint = localToWorld.MultiplyPoint3x4(nextLoc.X0Y());
                nextPoint.y = y;
                Handles.DrawLine(previousPoint, nextPoint);

                var vectorAlongLine = nextLoc - prevLoc;
                var normal = math.normalize(new float2(vectorAlongLine.y, -vectorAlongLine.x));
                var worldNormal = localToWorld.MultiplyVector(normal.x0y());
                var normalPosition = (previousPoint + nextPoint) / 2;
                Handles.DrawLine(normalPosition, normalPosition + worldNormal*25);

                prevLoc = nextLoc;
                previousPoint = nextPoint;
            }
        }

        void DrawBounds(float y, Matrix4x4 localToWorld) {
            var polygonLocalPoints = serializedObject.FindProperty("_polygonLocalPoints");
            var result = new UnsafeArray<float2>((uint)polygonLocalPoints.arraySize, Allocator.Temp);
            for (var i = 0u; i < result.Length; i++) {
                var localPoint = polygonLocalPoints.GetArrayElementAtIndex((int)i).vector2Value;
                result[i] = localToWorld.MultiplyPoint3x4(localPoint.X0Y()).XZ();
            }

            Polygon2DUtils.Bounds(result, out var bounds);
            var min = bounds.min;
            var max = bounds.max;

            Handles.DrawDottedLine(new Vector3(min.x, y, min.y), new Vector3(max.x, y, min.y), 5);
            Handles.DrawDottedLine(new Vector3(max.x, y, min.y), new Vector3(max.x, y, max.y), 5);
            Handles.DrawDottedLine(new Vector3(max.x, y, max.y), new Vector3(min.x, y, max.y), 5);
            Handles.DrawDottedLine(new Vector3(min.x, y, max.y), new Vector3(min.x, y, min.y), 5);
        }

        void DrawVolumeLines(float bottom, float top, Color? colorOverride = null) {
            var polygonLocalPoints = serializedObject.FindProperty("_polygonLocalPoints");
            var localToWorld = ((Polygon2DAuthoring)target).transform.localToWorldMatrix;

            Handles.color = colorOverride ?? serializedObject.FindProperty("_gizmosColor").colorValue;
            for (int i = 0; i < polygonLocalPoints.arraySize; i++) {
                var point = polygonLocalPoints.GetArrayElementAtIndex(i).vector2Value;
                var bottomPoint = localToWorld.MultiplyPoint3x4(point.X0Y());
                bottomPoint.y = bottom;
                var topPoint = bottomPoint;
                topPoint.y = top;
                Handles.DrawLine(bottomPoint, topPoint);
            }
        }
    }
}
