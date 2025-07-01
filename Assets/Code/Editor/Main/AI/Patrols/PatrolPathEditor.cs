using System;
using Awaken.TG.Main.AI.Idle.Interactions.Patrols;
using Awaken.TG.Main.Grounds;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Main.AI.Patrols {
    public static class PatrolPathEditor {
        /// <returns>should repaint</returns>
        [MustUseReturnValue]
        public static bool OnPreSceneGUI(Object context, ref PatrolPath path, ref int selected) {
            var previousSelection = selected;
            var evt = Event.current;
            switch (evt.type) {
                case EventType.MouseDown when evt.button == 1: {
                    if (evt.shift) {
                        var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                        if (Physics.Raycast(ray, out var hit)) {
                            Undo.RecordObject(context, "Add Waypoint");
                            selected += 1;
                            Array.Resize(ref path.waypoints, path.waypoints.Length + 1);
                            Array.Copy(path.waypoints, selected, path.waypoints, selected + 1, path.waypoints.Length - selected - 1); // shift elements right from the index
                            path.waypoints[selected] = new PatrolWaypoint(hit.point);
                            evt.Use();
                        }
                    } else if (!path.EDITOR_AllHandles) {
                        var newSelected = IndexOfWaypointInRange(path);
                        if (newSelected >= 0) {
                            if (selected == newSelected) {
                                selected = -1;
                            } else {
                                selected = newSelected;
                            }

                            evt.Use();
                        }
                    }

                    break;
                }

                case EventType.KeyDown when evt.keyCode == KeyCode.Delete && selected != -1: {
                    Undo.RecordObject(context, "Remove Waypoint");
                    ArrayUtility.RemoveAt(ref path.waypoints, selected);
                    selected = -1;
                    evt.Use();
                    break;
                }
            }
            
            if (previousSelection != selected) {
                for (int i = 0; i < path.waypoints.Length; i++) {
                    path.waypoints[i].EDITOR_selected = i == selected;
                }

                return true;
            }

            if (PatrolWaypoint.EDITOR_newSelection) {
                int newSelection = -1;
                for (int i = 0; i < path.waypoints.Length; i++) {
                    if (path.waypoints[i].EDITOR_selected && i != selected) {
                        newSelection = i;
                    } else {
                        path.waypoints[i].EDITOR_selected = false;
                    }
                }
                selected = newSelection;
                PatrolWaypoint.EDITOR_newSelection = false;
                return true;
            }

            return false;
        }

        /// <returns>Should repaint</returns>
        [MustUseReturnValue]
        public static bool OnSceneGUI(Object context, ref PatrolPath path, ref int selected) {
            var previousSelection = selected;
            
            if (path.EDITOR_AllHandles) {
                HandleForEachWaypoint(context, ref path, ref selected);
            } else {
                HandleOnlyForSelected(context, ref path, selected);
            }
            
            if (previousSelection != selected) {
                for (int i = 0; i < path.waypoints.Length; i++) {
                    path.waypoints[i].EDITOR_selected = i == selected;
                }
                return true;
            }
            return false;
        }
        
        public static void OnDisable(ref PatrolPath path, ref int selected) {
            if (selected >= 0 && selected < path.waypoints.Length) {
                path.waypoints[selected].EDITOR_selected = false;
                selected = -1;
            }
        }

        static void HandleOnlyForSelected(Object context, ref PatrolPath path, int selected) {
            if (selected >= 0 && selected < path.waypoints.Length) {
                HandleForIndex(context, ref path, selected, out _);
            }
        }
        
        static void HandleForEachWaypoint(Object context, ref PatrolPath path, ref int selected) {
            for (int i = 0; i < path.waypoints.Length; i++) {
                HandleForIndex(context, ref path, i, out var changed);
                if (changed) {
                    selected = i;
                }
            }
        }

        static void HandleForIndex(Object context, ref PatrolPath path, int index, out bool changed) {
            changed = false;
            EditorGUI.BeginChangeCheck();
            var position = Handles.PositionHandle(path.waypoints[index].position, Quaternion.identity);
            if (EditorGUI.EndChangeCheck()) {
                if (path.EDITOR_SnapToGround) {
                    position = Ground.SnapNpcToGround(position);
                }

                Undo.RecordObject(context, "Move Waypoint");
                path.waypoints[index].position = position;
                changed = true;
            }

            if (path.waypoints[index].interactAround) {
                EditorGUI.BeginChangeCheck();

                var previousZTest = Handles.zTest;
                var previousColor = Handles.color;

                float newRange = path.waypoints[index].interactionRange;
                DrawRangeHandle(position, ref newRange, CompareFunction.Greater, Color.gray);
                DrawRangeHandle(position, ref newRange, CompareFunction.Less, Color.white);

                Handles.zTest = previousZTest;
                Handles.color = previousColor;

                if (EditorGUI.EndChangeCheck() && !Application.isPlaying) {
                    Undo.RecordObject(context, "Change idleBehaviour range");
                    path.waypoints[index].interactionRange = newRange;
                    changed = true;
                }
            }
        }

        static int IndexOfWaypointInRange(in PatrolPath path) {
            for (int i = 0; i < path.waypoints.Length; i++) {
                float handleRadius = HandleUtility.GetHandleSize(path.waypoints[i].position);
                float distance = HandleUtility.DistanceToCircle(path.waypoints[i].position, handleRadius);
                if (distance <= 0) {
                    return i;
                }
            }
            return -1;
        }
        
        static void DrawRangeHandle(Vector3 position, ref float range, CompareFunction zTest, Color color) {
            Handles.zTest = zTest;
            Handles.color = color;
            range = Handles.RadiusHandle(Quaternion.identity, position, range);
        }
    }
}