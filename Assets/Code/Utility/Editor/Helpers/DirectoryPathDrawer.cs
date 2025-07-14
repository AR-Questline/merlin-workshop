using System;
using System.IO;
using Awaken.Utility.UI;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.Utility.Editor.Helpers {
    [CustomPropertyDrawer(typeof(DirectoryPathAttribute))]
    public class DirectoryPathDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);
            var propertyRect = new PropertyDrawerRects(position);
            var buttonRect = propertyRect.AllocateLeft(32);
            propertyRect.LeaveSpace(4);
            var textFieldRect = propertyRect.AllocateWithRest(36);
            propertyRect.LeaveSpace(4);
            var selectButtonRect = (Rect)propertyRect;

            var currentPath = property.stringValue;

            var isValidPath = !string.IsNullOrEmpty(currentPath) && AssetDatabase.IsValidFolder(currentPath);

            var folderIcon = EditorGUIUtility.IconContent("FolderOpened Icon");
            if (GUI.Button(buttonRect, folderIcon)) {
                var newDirectoryPath = EditorUtility.OpenFolderPanel("Select Directory", currentPath, "");
                if (!string.IsNullOrEmpty(newDirectoryPath)) {
                    var projectPath = Application.dataPath;
                    var rootPath = projectPath.Substring(0, projectPath.Length - "/Assets".Length);
                    if (newDirectoryPath.StartsWith(rootPath)) {
                        newDirectoryPath = newDirectoryPath.Substring(rootPath.Length + 1);
                    } else {
                        newDirectoryPath = string.Empty;
                    }
                    property.stringValue = newDirectoryPath;
                }
            }

            using var colorScope = new ColorGUIScope(isValidPath ? Color.white : Color.red);

            EditorGUI.BeginChangeCheck();
            currentPath = EditorGUI.TextField(textFieldRect, currentPath);
            if (EditorGUI.EndChangeCheck()) {
                property.stringValue = currentPath;
            }

            var selectIcon = EditorGUIUtility.IconContent("Folder Icon");
            EditorGUI.BeginDisabledGroup(!isValidPath);
            if (GUI.Button(selectButtonRect, selectIcon)) {
                Object folder = AssetDatabase.LoadAssetAtPath<Object>(currentPath);
                if (folder) {
                    Selection.activeObject = folder;
                    EditorGUIUtility.PingObject(folder);
                } else {
                    Debug.LogError($"The folder at path '{currentPath}' does not exist or is not a valid asset folder.");
                }
            }
            EditorGUI.EndDisabledGroup();

            Event currentEvent = Event.current;
            if (position.Contains(currentEvent.mousePosition)) {
                if (currentEvent.type == EventType.DragUpdated) {
                    // Check if th dragged object is directory.
                    if (DragAndDrop.objectReferences.Length == 1 && AssetDatabase.Contains(DragAndDrop.objectReferences[0])) {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        currentEvent.Use();
                    }
                } else if (currentEvent.type == EventType.DragPerform) {
                    DragAndDrop.AcceptDrag();

                    var path = AssetDatabase.GetAssetPath(DragAndDrop.objectReferences[0]);
                    if (Path.HasExtension(path)) {
                        path = Path.GetDirectoryName(path);
                    }
                    property.stringValue = path;
                    currentEvent.Use();
                }
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}