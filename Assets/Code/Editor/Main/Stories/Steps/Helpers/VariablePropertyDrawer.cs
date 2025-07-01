using System.Linq;
using Awaken.TG.Debugging;
using Awaken.TG.Editor.Helpers;
using Awaken.TG.Editor.Utility;
using Awaken.TG.Editor.Utility.StoryGraphs;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.Utility.Collections;
using Awaken.Utility.UI;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Stories.Steps.Helpers {
    [CustomPropertyDrawer(typeof(Variable))]
    public class VariablePropertyDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);
            // remove indent
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            if (property.ExtractAttribute<SetterAttribute>() != null) {
                DrawSetVariable(position, property);
            } else {
                DrawGetVariable(position, property);
            }

            // clean up
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        void DrawSetVariable(Rect position, SerializedProperty property) {
            // Calculate rects
            PropertyDrawerRects rects = position;
            PropertyDrawerRects firstLine = rects.AllocateTop((int)EditorGUIUtility.singleLineHeight);
            PropertyDrawerRects secondLine = rects.AllocateTop((int)EditorGUIUtility.singleLineHeight);

            Rect nameLabel = firstLine.AllocateLeft(50);
            Rect nameRect = firstLine.AllocateLeft(100);
            Rect valueLabel = secondLine.AllocateLeft(50);
            Rect typeRect = secondLine.AllocateLeft(80);
            Rect plusRect = secondLine.AllocateLeft(20);
            Rect valueRect = secondLine.AllocateLeft(30);
            // get props
            var nameRef = property.FindPropertyRelative("name");
            var valueRef = property.FindPropertyRelative("value");
            // draw fields
            VariableType type = (VariableType)property.FindPropertyRelative("type").enumValueIndex;
            EditorGUI.LabelField(nameLabel, "Name");
            if (type == VariableType.Defined) {
                DrawDefinedEnum(property, nameRect, nameRef);
            } else {
                EditorGUI.PropertyField(nameRect, nameRef, GUIContent.none);
            }

            EditorGUI.LabelField(valueLabel, "Value");
            type = DrawType(property, typeRect);

            if (type == VariableType.Custom || type == VariableType.Const || type == VariableType.Defined) {
                EditorGUI.PropertyField(valueRect, valueRef, GUIContent.none);
            } else {
                GUIStyle style = new GUIStyle();
                style.alignment = TextAnchor.MiddleCenter;
                style.normal.textColor = Color.white;
                EditorGUI.LabelField(plusRect, "+", style);
                EditorGUI.PropertyField(valueRect, valueRef, GUIContent.none);
            }
        }

        void DrawGetVariable(Rect position, SerializedProperty property) {
            // Calculate rects
            PropertyDrawerRects rects = position;
            Rect typeRect = rects.AllocateLeft(80);
            // get props
            var nameRef = property.FindPropertyRelative("name");
            var valueRef = property.FindPropertyRelative("value");
            // draw fields
            VariableType type = DrawType(property, typeRect);

            if (type == VariableType.Custom) {
                Rect nameRect = rects.AllocateLeft(100);
                EditorGUI.PropertyField(nameRect, nameRef, GUIContent.none);
            } else if (type == VariableType.Const) {
                Rect valueRect = rects.AllocateLeft(50);
                EditorGUI.PropertyField(valueRect, valueRef, GUIContent.none);
            } else if (type == VariableType.Defined) {
                Rect enumRect = rects.AllocateLeft(100);
                DrawDefinedEnum(property, enumRect, nameRef);
            }
        }

        VariableType DrawType(SerializedProperty property, Rect typeRect) {
            var typeRef = property.FindPropertyRelative("type");
            
            ForceTypeAttribute forceType = property.ExtractAttribute<ForceTypeAttribute>();
            VariableType type;
            if (forceType != null) {
                // we have forced type
                VariableType[] availableTypes = forceType.Types;
                if (availableTypes.Length == 1) {
                    // if only one possibility, disable GUI
                    type = availableTypes[0];
                    typeRef.enumValueIndex = (int) type;
                    bool wasEnabled = GUI.enabled;
                    GUI.enabled = false;
                    EditorGUI.PropertyField(typeRect, typeRef, GUIContent.none);
                    GUI.enabled = wasEnabled;
                } else {
                    // let choose any from allowed
                    type = (VariableType) typeRef.enumValueIndex;
                    int current = availableTypes.IndexOf(type);
                    using var changeScope = new TGGUILayout.CheckChangeScope();
                    current = EditorGUI.Popup(typeRect, current, availableTypes.Select(t => t.ToString()).ToArray());
                    if (changeScope || current < 0) {
                        if (current < 0) {
                            current = 0;
                        }

                        type = availableTypes[current];
                        typeRef.enumValueIndex = (int) type;
                    }
                }
            } else {
                // no forcing, just draw normal enum popup
                EditorGUI.PropertyField(typeRect, typeRef, GUIContent.none);
                type = (VariableType) typeRef.enumValueIndex;
            }
            
            return type;
        }

        void DrawDefinedEnum(SerializedProperty property, Rect enumRect, SerializedProperty nameRef) {
            StoryGraph graph = NodeGUIUtil.Graph(property) as StoryGraph;
            if (graph == null) {
                EditorGUI.HelpBox(enumRect, "Can't find graph for variable", MessageType.Error);
            } else {
                string[] availableNames = graph.variables.Select(v => v.name).ToArray();
                if (!availableNames.Any()) {
                    return;
                }
                int chosen = availableNames.IndexOf(nameRef.stringValue);

                using var checkScope = new TGGUILayout.CheckChangeScope();
                chosen = EditorGUI.Popup(enumRect, chosen, availableNames);
                if (checkScope || chosen < 0) {
                    if (chosen < 0) {
                        chosen = 0;
                    }

                    nameRef.stringValue = availableNames[chosen];
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            bool isSet = property.ExtractAttribute<SetterAttribute>() != null;
            float def = base.GetPropertyHeight(property, label);
            return isSet ? def * 2f : def;
        }
    }
}