using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Locations.Paths;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Paths {
    [CustomEditor(typeof(VertexPathSpec))]
    public class VertexPathSpecEditor : OdinEditor {
        
        VertexPathSpec PathSpec => target as VertexPathSpec;
        int _selected = -1;

        void OnPreSceneGUI() {
            var evt = Event.current;
            switch (evt.type) {
                case EventType.MouseDown when evt.button == 1:
                    if (evt.shift) {
                        var ray = HandleUtility.GUIPointToWorldRay( Event.current.mousePosition );
                        if(Physics.Raycast(ray, out var hit)) {
                            Undo.RecordObject(PathSpec, "Add Waypoint");
                            PathSpec.waypoints.Add(hit.point);
                            _selected = PathSpec.waypoints.Count - 1;
                            evt.Use();
                        } 
                    } else {
                        var newSelected = IndexOfWaypointInRange();
                        if (newSelected >= 0) {
                            if (_selected == newSelected) {
                                _selected = -1;
                            } else {
                                _selected = newSelected;
                            }
                            evt.Use();
                        }
                    }
                    break;

                case EventType.KeyDown when evt.keyCode == KeyCode.Delete && _selected != -1:
                    Undo.RecordObject(PathSpec, "Remove Waypoint");
                    PathSpec.waypoints.RemoveAt(_selected);
                    _selected = -1;
                    evt.Use();
                    break;
            }
        }

        void OnSceneGUI() {
            if (_selected != -1) {
                var position = Handles.PositionHandle(PathSpec.waypoints[_selected], Quaternion.identity);
                if (PathSpec.snapToGround) {
                    position = Ground.SnapToGround(position);
                }
                Undo.RecordObject(PathSpec, "Move Waypoint");
                PathSpec.waypoints[_selected] = position;
            }
        }

        int IndexOfWaypointInRange() {
            for (int i = 0; i < PathSpec.waypoints.Count; i++) {

                float handleRadius = HandleUtility.GetHandleSize(PathSpec.waypoints[i]);
                float distance = HandleUtility.DistanceToCircle(PathSpec.waypoints[i], handleRadius);
                if (distance <= 0) {
                    return i;
                }
            }
            return -1;
        }
    }
}