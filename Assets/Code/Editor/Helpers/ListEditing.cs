using Awaken.TG.Editor.Utility;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Utility.Attributes;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Utility.Attributes.List;
using Awaken.Utility.Enums;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Helpers {
    /// <summary>
    /// Code adapted from https://catlikecoding.com/unity/tutorials/editor/custom-list/.
    /// </summary>

    public static class ListEditing {

        private static GUIContent
            s_moveButtonDownContent = new GUIContent("\u25bc", "move down"),
            s_moveButtonUpContent = new GUIContent("\u25b2", "move up"),
            s_duplicateButtonContent = new GUIContent("+", "duplicate"),
            s_deleteButtonContent = new GUIContent("\u2716", "delete"),
            s_addButtonContent = new GUIContent("+", "add element");

        private static GUILayoutOption miniButtonWidth = GUILayout.Width(20f);

        public static void Show (SerializedProperty list, ListEditOption editOptions = ListEditOption.Default) {
            if (!list.isArray) {
                EditorGUILayout.HelpBox(list.name + " is neither an array nor a list!", MessageType.Error);
                return;
            }
            
            bool
                showListLabel = (editOptions & ListEditOption.ListLabel) != 0,
                showListSize = (editOptions & ListEditOption.ListSize) != 0;

            if (showListLabel) {
                EditorGUILayout.PropertyField(list, false);
                EditorGUI.indentLevel += 1;
            }
            if (!showListLabel || list.isExpanded) {
                SerializedProperty size = list.FindPropertyRelative("Array.size");
                if (showListSize) {
                    EditorGUILayout.PropertyField(size);
                }
                if (size.hasMultipleDifferentValues) {
                    EditorGUILayout.HelpBox("Not showing lists with different sizes.", MessageType.Info);
                }
                else {
                    ShowElements(list, editOptions);
                }
            }
            if (showListLabel) {
                EditorGUI.indentLevel -= 1;
            }
        }

        private static void ShowElements (SerializedProperty list, ListEditOption editOptions) {
            bool
                showElementLabels = editOptions.HasFlag(ListEditOption.ElementLabels),
                showFewButtons = editOptions.HasFlag(ListEditOption.FewButtons),
                showButtons = editOptions.HasFlag(ListEditOption.Buttons) | editOptions.HasFlag(ListEditOption.FewButtons),
                newElementNull = editOptions.HasFlag(ListEditOption.NullNewElement);

            for (int i = 0; i < list.arraySize; i++) {
                SerializedProperty property = list.GetArrayElementAtIndex(i);
                bool shouldStartInNewLine = property.boxedValue is ItemSpawningData;
                if (showButtons) {
                    EditorGUILayout.BeginHorizontal();
                    if (shouldStartInNewLine) {
                        GUILayout.FlexibleSpace();
                        ShowButtons(list, i, showFewButtons);
                        EditorGUILayout.EndHorizontal();
                    }
                }
                
                if (showElementLabels) {
                    string label = ExtractLabel(property);

                    if (label != null) {
                        EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i), new GUIContent(label), true);
                    } else {
                        EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i), true);
                    }
                }
                else if (i < list.arraySize) {
                        EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i), GUIContent.none, true);
                }
                if (showButtons && !shouldStartInNewLine) {
                    ShowButtons(list, i, showFewButtons);
                    EditorGUILayout.EndHorizontal();
                }
            }

            if (showButtons) {
                GUILayout.BeginHorizontal();
                GUILayout.Label("", GUIStyle.none, GUILayout.ExpandWidth(true));           
                if (GUILayout.Button(s_addButtonContent, EditorStyles.miniButton, miniButtonWidth)) {
                    int index = list.arraySize;
                    list.InsertArrayElementAtIndex(index);
                    list.serializedObject.ApplyModifiedProperties();
                    if (newElementNull) {
                        var prop = list.GetArrayElementAtIndex(index);
                        prop.SetValue(default);
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.Label("", GUIStyle.none, GUILayout.Height(5));
            }  
        }

        static string ExtractLabel(SerializedProperty property) {
            SerializedProperty richEnumSource = null;
            
            foreach (var child in property.GetChildren()) {
                if (child.ExtractAttribute<RichEnumLabelAttribute>() != null) {
                    richEnumSource = child.Copy();
                }
            }
            
            if (richEnumSource != null) {
                string enumRef = richEnumSource.FindPropertyRelative("_enumRef").stringValue;
                RichEnum richEnum = new RichEnumReference(enumRef).EnumAs<RichEnum>();
                return richEnum.EnumName;
            }

            return null;
        }

        private static void ShowButtons (SerializedProperty list, int index, bool onlyFew = false) {
            if (!onlyFew) {
                if (GUILayout.Button(s_moveButtonUpContent, EditorStyles.miniButtonLeft, miniButtonWidth)) {
                    list.MoveArrayElement(index, index - 1);
                }

                if (GUILayout.Button(s_moveButtonDownContent, EditorStyles.miniButtonMid, miniButtonWidth)) {
                    list.MoveArrayElement(index, index + 1);
                }

                if (GUILayout.Button(s_duplicateButtonContent, EditorStyles.miniButtonMid, miniButtonWidth)) {
                    list.InsertArrayElementAtIndex(index);
                }
            }

            if (GUILayout.Button(s_deleteButtonContent, EditorStyles.miniButtonRight, miniButtonWidth)) {
                int oldSize = list.arraySize;
                list.DeleteArrayElementAtIndex(index);
                if (list.arraySize == oldSize) {
                    list.DeleteArrayElementAtIndex(index);
                }
            }
        }
    }
}