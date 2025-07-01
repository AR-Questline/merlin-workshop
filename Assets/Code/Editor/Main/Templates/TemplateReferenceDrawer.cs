using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Awaken.TG.Editor.Assets.Templates;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Debugging;
using Awaken.Utility.Editor.MoreGUI;
using Awaken.Utility.UI;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Main.Templates {
    [CustomPropertyDrawer(typeof(TemplateReference))]
    public sealed class TemplateReferenceDrawer : PropertyDrawer {
        static readonly Dictionary<string, string> ShrankPaths = new();

        // cache to avoid allocations
        readonly List<string> _paths = new(20);
        readonly List<GUIContent> _shrunkPaths = new(20);
        
        FieldInfo FieldInfo { get; set; }
        double _timeOfNextRegenerate;
        public TemplateReferenceDrawer() { }

        public TemplateReferenceDrawer(FieldInfo fieldInfo) {
            this.FieldInfo = fieldInfo;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            SerializedProperty propGuid = property.FindPropertyRelative("_guid");
            FieldInfo ??= fieldInfo;
            label ??= GUIContent.none;
            // validation
            ValidateCurrentGUID(propGuid);
            // prepare block
            EditorGUI.BeginProperty(position, label, property);

            var propertyDrawerRects = new PropertyDrawerRects(position);

            // label
            if (DrawPrefixLabel(label)) {
                position = EditorGUI.PrefixLabel(propertyDrawerRects.AllocateTop((int)EditorGUIUtility.singleLineHeight),
                    GUIUtility.GetControlID(FocusType.Passive), label);
            }

            //Check if we know type of template
            var type = FieldInfo?.GetCustomAttribute<TemplateTypeAttribute>();
            if (type != null && EditorApplication.timeSinceStartup > _timeOfNextRegenerate) {
                RegeneratePaths(type);
                _timeOfNextRegenerate = EditorApplication.timeSinceStartup + 1f;
            }

            ManageDragAndDrop(propGuid, (Rect)propertyDrawerRects, type?.Type ?? typeof(ITemplate));

            if (type != null && _shrunkPaths.Count != 0) {
                // Allocate rects
                var allocatedRow1 = propertyDrawerRects.AllocateTop((int)EditorGUIUtility.singleLineHeight);
                Rect row2 = (Rect)propertyDrawerRects;
                allocatedRow1.height = EditorGUIUtility.singleLineHeight;
                allocatedRow1.y -= 2;
                var drawerRectRow1 = new PropertyDrawerRects(allocatedRow1);
                var row1Instance = (Rect)drawerRectRow1;
                
                // Draw Instance Field
                DrawInstanceField(row1Instance, GUIContent.none, propGuid, type.Type);
                
                EditorGUI.BeginChangeCheck();
                var shrunkPathsArray = _shrunkPaths.ToArray();
                var currentPath = AssetDatabase.GUIDToAssetPath(propGuid.stringValue);
                var index = _paths.IndexOf(currentPath);
                index = AREditorPopup.Draw(in row2, index, shrunkPathsArray, shrunkPathsArray, "None");
                if (EditorGUI.EndChangeCheck()) {
                    if (index >= 0) {
                        string assetPath = _paths.ElementAt(index);
                        propGuid.stringValue = AssetDatabase.AssetPathToGUID(assetPath);
                    }else if (index == -1) {
                        propGuid.stringValue = null;
                    }
                }
            } else {
                // actual UI
                DrawInstanceField(position, label, propGuid, typeof(Object));
            }

            // done 
            EditorGUI.EndProperty();
        }

        public static string GetDropdownLabel(string guid, Type templateType) {
            string dropdownLabel;
            if (!string.IsNullOrEmpty(guid)) {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string assetName = Path.GetFileNameWithoutExtension(assetPath);
                dropdownLabel = $"{assetName} ({templateType.Name})";
            } else {
                dropdownLabel = "Choose from templates";
            }

            return dropdownLabel;
        }

        public static GenericMenu CreateGenericMenu(List<string> shrunkPaths, string currentGuid, Func<int, bool> isOn, Action<int> onChoose) {
            GenericMenu menu = new();
            for (int i = 0; i < shrunkPaths.Count; i++) {
                string path = shrunkPaths[i];
                int capturedIndex = i;
                menu.AddItem(new GUIContent(path), isOn(i), () => onChoose(capturedIndex));
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("None"), string.IsNullOrWhiteSpace(currentGuid), () => onChoose(-2));
            return menu;
        }

        void RegeneratePaths(TemplateTypeAttribute type) {
            _paths.Clear();
            _shrunkPaths.Clear();
            _paths.AddRange(ObtainPaths(type.Type));

            // default case, take shrank paths from cache
            for (int i = 0; i < _paths.Count; i++) {
                _shrunkPaths.Insert(i, new GUIContent(ShrankPaths[_paths[i]]));
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            float height = EditorGUIUtility.singleLineHeight; //Prefix and maybe instance field
            if (DrawPrefixLabel(label)) {
                height += EditorGUIUtility.singleLineHeight;
            }

            height += ((FieldInfo?.GetCustomAttribute<TemplateTypeAttribute>() != null) ? EditorGUIUtility.singleLineHeight : 0); // Search and instance field
            height += 2; // Const spacing
            return height;
        }

        bool DrawPrefixLabel(GUIContent label) => label != null
                                                  && FieldInfo != null
                                                  && FieldInfo.GetCustomAttribute<HeaderAttribute>() == null
                                                  && !string.IsNullOrWhiteSpace(label.text)
                                                  && FieldInfo.GetCustomAttribute<TemplateTypeAttribute>() != null
                                                  && FieldInfo.GetCustomAttribute<HideLabelAttribute>() == null;

        static void DrawInstanceField(Rect position, GUIContent label, SerializedProperty propGuid, Type assetType) {
            EditorGUI.BeginChangeCheck();
            Object asset = null;
            if (!string.IsNullOrWhiteSpace(propGuid.stringValue)) {
                asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(propGuid.stringValue));
            }

            asset = EditorGUI.ObjectField(position, label, asset, assetType, false);
            if (EditorGUI.EndChangeCheck()) {
                string assetPath = AssetDatabase.GetAssetPath(asset);
                propGuid.stringValue = string.IsNullOrEmpty(assetPath) ? null : AssetDatabase.AssetPathToGUID(assetPath);
            }
        }

        void ValidateCurrentGUID(SerializedProperty propGuid) {
            // everything is null, nothing to check
            if (string.IsNullOrWhiteSpace(propGuid.stringValue)) {
                return;
            }

            string path = AssetDatabase.GUIDToAssetPath(propGuid.stringValue);
            if (path.IsNullOrWhitespace()) {
                Log.Important?.Error(
                    $"Invalid GUID ({propGuid.stringValue}) was assigned to template reference in {propGuid.serializedObject.targetObject.GetType()}");
                propGuid.stringValue = null;
            }
        }

        public static int CurrentIndex(string guid, List<string> paths) {
            if (!string.IsNullOrWhiteSpace(guid)) {
                string propPath = AssetDatabase.GUIDToAssetPath(guid);
                return paths.IndexOf(propPath);
            }

            return -1;
        }

        public static List<string> ObtainPaths(Type type) {
            List<string> paths;
            try {
                paths = TemplatesSearcher.GetPathsForType(type);
            } catch {
                paths = new List<string>();
            }

            using IEnumerator<string> shrunkPaths = ShrinkPaths(paths).GetEnumerator();

            int i = 0;
            while (shrunkPaths.MoveNext()) {
                string path = shrunkPaths.Current;
                ShrankPaths[paths[i]] = path;
                i++;
            }

            return paths;
        }

        public static IEnumerable<string> ShrinkPaths(List<string> paths) {
            if (paths.Count < 1) {
                return Array.Empty<string>();
            }

            var commonGreaterIndex = FindCommonGreaterIndex(paths);
            return paths.Select(p => p.Substring(commonGreaterIndex, p.Length - commonGreaterIndex));
        }

        static int FindCommonGreaterIndex(List<string> paths) {
            int slashIndex = 0;

            var firstPath = paths[0];
            for (int i = 0; i < firstPath.Length; i++) {
                char c = firstPath[i];
                for (int j = 1; j < paths.Count; j++) {
                    var otherPath = paths[j];
                    if (i >= otherPath.Length || otherPath[i] != c) {
                        return slashIndex;
                    }
                }

                if (c == '/') {
                    slashIndex = i + 1;
                }
            }

            return slashIndex;
        }

        // Drag&Drop
        static void ManageDragAndDrop(SerializedProperty propGuid, Rect position, Type assetType) {
            if (Event.current.type == EventType.DragUpdated && position.Contains(Event.current.mousePosition)) {
                DragAndDrop.visualMode = CanAcceptDrag(assetType) ? DragAndDropVisualMode.Link : DragAndDropVisualMode.Rejected;
                Event.current.Use();
            } else if (Event.current.type == EventType.DragPerform && position.Contains(Event.current.mousePosition)) {
                if (CanAcceptDrag(assetType)) {
                    Object templateObject = DragAndDrop.objectReferences[0];
                    if (ValidateDraggedObject(templateObject)) {
                        string assetPath = AssetDatabase.GetAssetPath(templateObject);
                        if (string.IsNullOrEmpty(assetPath)) {
                            propGuid.stringValue = null;
                        } else {
                            propGuid.stringValue = AssetDatabase.AssetPathToGUID(assetPath);
                        }

                        DragAndDrop.AcceptDrag();
                    }
                }
            }
        }

        public static bool CanAcceptDrag(Type assetType) {
            bool onlyOneAsset = DragAndDrop.objectReferences.Length == 1;
            ITemplate template = TemplatesUtil.ObjectToTemplateUnsafe(DragAndDrop.objectReferences[0]);
            return onlyOneAsset && template != null && assetType.IsInstanceOfType(template);
        }

        public static bool ValidateDraggedObject(Object templateObject) {
            ITemplate template = TemplatesUtil.ObjectToTemplateUnsafe(templateObject);

            if (template != null) {
                AddressableTemplatesCreator.CreateOrUpdateAsset(templateObject);
                return true;
            }

            EditorUtility.DisplayDialog("Invalid object", "Chosen object is not template!", "Ok");
            throw new Exception("Chosen object is not template!");
        }
    }
}