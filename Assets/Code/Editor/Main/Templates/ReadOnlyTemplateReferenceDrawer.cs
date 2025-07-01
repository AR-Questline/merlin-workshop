using Awaken.TG.Main.Templates;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Templates {
    [CustomPropertyDrawer(typeof(ReadOnlyTemplateReference))]
    public class ReadOnlyTemplateReferenceDrawer : PropertyDrawer {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            var guid = property.FindPropertyRelative("reference").FindPropertyRelative("_guid").stringValue;
            EditorGUI.ObjectField(position, label, GetTemplate(guid), typeof(Object), false);
        }

        static Object GetTemplate(string guid) {
            if (string.IsNullOrWhiteSpace(guid)) {
                return null;
            }
            return AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));
        }
    }
}