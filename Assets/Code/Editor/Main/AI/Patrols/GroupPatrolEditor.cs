using System;
using Awaken.TG.Main.AI.Idle.Interactions.Patrols;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.AI.Patrols {
    [CustomEditor(typeof(GroupPatrol))]
    public class GroupPatrolEditor : OdinEditor {
        int _selected = -1;
        
        GroupPatrol GroupPatrol => (GroupPatrol) target;
        ref PatrolPath Path => ref GroupPatrol.PatrolPath;

        void OnPreSceneGUI() {
            if (PatrolPathEditor.OnPreSceneGUI(target, ref Path, ref _selected)) {
                Repaint();
            }
        }

        void OnSceneGUI() {
            if (PatrolPathEditor.OnSceneGUI(target, ref Path, ref _selected)) {
                Repaint();
            }
        }

        protected override void OnDisable() {
            PatrolPathEditor.OnDisable(ref Path, ref _selected);
            base.OnDisable();
        }

        public override void OnInspectorGUI() {
            var formationProperty = serializedObject.FindProperty("formation");
            int currentValue = formationProperty.intValue;
            int newValue = EditorGUILayout.Popup(currentValue, formationProperty.enumDisplayNames);
            if (newValue != currentValue) {
                formationProperty.intValue = newValue;
                serializedObject.ApplyModifiedProperties();
                var go = GroupPatrol.gameObject;
                RemoveAllSpots(go);

                switch ((GroupPatrol.Formation) newValue) {
                    case GroupPatrol.Formation._0:
                        break;
                    case GroupPatrol.Formation._1:
                        AddSpot(go, 0, "First", new Vector2(0, 0));
                        break;
                    case GroupPatrol.Formation._2_SingleFile:
                        AddSpot(go, 1, "First", new Vector2(0, 0.5f));
                        AddSpot(go, 0, "Second", new Vector2(0, -0.7f));
                        break;
                    case GroupPatrol.Formation._2_SideBySide:
                        AddSpot(go, 0, "Left", new Vector2(-0.5f, 0));
                        AddSpot(go, 0, "Right", new Vector2(0.5f, 0));
                        break;
                    case GroupPatrol.Formation._3_SingleFile:
                        AddSpot(go, 0, "First", new Vector2(0, 1));
                        AddSpot(go, 1, "Middle", new Vector2(0, -0.1f));
                        AddSpot(go, 0, "Last", new Vector2(0, -1.2f));
                        break;
                    case GroupPatrol.Formation._3_SideBySide:
                        AddSpot(go, 0, "Left", new Vector2(-1, 0));
                        AddSpot(go, 1, "Middle", new Vector2(0, 0));
                        AddSpot(go, 0, "Right", new Vector2(1, 0));
                        break;
                    case GroupPatrol.Formation._3_Arrow:
                        AddSpot(go, 1, "First", new Vector2(0, 0.9f));
                        AddSpot(go, 0, "Left", new Vector2(-0.6f, 0));
                        AddSpot(go, 0, "Right", new Vector2(0.6f, 0));
                        break;
                    case GroupPatrol.Formation._3_InversedArrow:
                        AddSpot(go, 1, "Left", new Vector2(-0.6f, 0));
                        AddSpot(go, 1, "Right", new Vector2(0.6f, 0));
                        AddSpot(go, 0, "Last", new Vector2(0, -0.9f));
                        break;
                    case GroupPatrol.Formation._4_PeopleSquare:
                        AddSpot(go, 1, "First Left", new Vector2(-0.5f, 0.6f));
                        AddSpot(go, 1, "First Right", new Vector2(0.5f, 0.6f));
                        AddSpot(go, 0, "Second Left" , new Vector2(-0.5f, -0.6f));
                        AddSpot(go, 0, "Second Right" , new Vector2(0.5f, -0.6f));
                        break;
                    case GroupPatrol.Formation._4_PeopleDiamond:
                        AddSpot(go, 2, "First", new Vector2(0, 0.9f));
                        AddSpot(go, 1, "Right", new Vector2(0.6f, 0));
                        AddSpot(go, 1, "Left", new Vector2(-0.6f, 0));
                        AddSpot(go, 0, "Last", new Vector2(0, -0.9f));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            
            base.OnInspectorGUI();
            GUILayout.Space(20);

            if (GUILayout.Button("Align GameObject with first waypoint")) {
                GroupPatrol.gameObject.transform.position = Path.waypoints[0].position;
            }
            
            if (GUILayout.Button("Align first waypoint with GameObject")) {
                Path.waypoints[0].position = GroupPatrol.gameObject.transform.position;
            }
        }

        void RemoveAllSpots(GameObject go) {
            foreach (GroupPatrolSpot spot in go.GetComponents<GroupPatrolSpot>()) {
                DestroyImmediate(spot);
            }
        }
        void AddSpot(GameObject go, int priority, string name, Vector2 offset) {
            var spot = go.AddComponent<GroupPatrolSpot>();
            spot.SetData(priority, offset * 1.5f);
            spot.EDITOR_SetName(name);
        }
    }
}