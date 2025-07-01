using Awaken.Tests.Performance;
using Awaken.Tests.Performance.TestCases;
using Awaken.TG.Editor.Utility;
using Awaken.Utility.Editor;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.Tests.Editor.Performance {
    [CustomEditor(typeof(PerformanceTestRegistry))]
    public class PerformanceTestRegistryEditor : OdinEditor {
        bool _editing;
        
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            if (_editing) {
                if (GUILayout.Button("Stop Editing")) {
                    StopEditing();
                }
            } else {
                if (GUILayout.Button("Start Editing")) {
                    StartEditing();
                }
            }
        }

        void OnDestroy() {
            StopEditing();
        }

        void StartEditing() {
            _editing = true;
            SceneView.duringSceneGui += DuringSceneGUI;
        }

        void StopEditing() {
            _editing = false;
            SceneView.duringSceneGui -= DuringSceneGUI;
        }
        
        void DuringSceneGUI(SceneView sceneView) {
            if (target && target is PerformanceTestRegistry registry) {
                var color = Handles.color;
                bool changed = false;
                var registryAccessor = new PerformanceTestRegistry.EDITOR_Accessor(registry);
                foreach (var test in registryAccessor.SimpleTests) {
                    DrawFor(new SimplePerformanceTestCase.EDITOR_Accessor(test), ref changed);
                }
                if (changed) {
                    EditorUtility.SetDirty(registry);
                }
                Handles.color = color;
            } else {
                StopEditing();
            }
        }

        static void DrawFor(in SimplePerformanceTestCase.EDITOR_Accessor accessor, ref bool changed) {
            if (!accessor.EditorData.draw) {
                return;
            }
            var waypoints = accessor.Waypoints;
            if (waypoints.Length == 0) {
                return;
            }
            ref readonly var data = ref accessor.EditorData;
            DrawWaypoint(data, ref waypoints[0], ref changed);
            for (var i = 1; i < waypoints.Length; i++) {
                ref var waypoint = ref waypoints[i];
                DrawWaypoint(data, ref waypoint, ref changed);
                Handles.color = waypoint.capture ? data.capturingConnectionColor : data.defaultConnectionColor;
                if (waypoint.teleport) {
                    Handles.DrawDottedLine(waypoints[i-1].position, waypoint.position, 5);
                } else {
                    Handles.DrawLine(waypoints[i-1].position, waypoint.position);
                }
            }
        }

        static void DrawWaypoint(in SimplePerformanceTestCase.EDITOR_Data data, ref SimplePerformanceTestCase.Waypoint waypoint, ref bool changed) {
            Handles.color = waypoint.capture ? data.capturingPointColor : data.defaultPointColor;
            Handles.DrawWireCube(waypoint.position, Vector3.one * data.size);
            if (data.drawFrustum) {
                HandlesUtils.DrawFrustum(waypoint.position, waypoint.rotation, 10 * data.size, 90, 16f/9f);
            } else {
                Handles.DrawLine(waypoint.position, waypoint.position + waypoint.rotation * Vector3.forward * (data.size * 2));
            }

            if (data.drawTransforms) {
                var position = waypoint.position;
                var rotation = waypoint.rotation;
                var scale = 0.5f;
                Handles.color = Color.white;
                Handles.TransformHandle(ref position, ref rotation, ref scale);
                if (position != waypoint.position) {
                    changed = true;
                    waypoint.position = position;
                }

                if (rotation != waypoint.rotation) {
                    changed = true;
                    waypoint.rotation = rotation;
                }
            }
        }
    }
}