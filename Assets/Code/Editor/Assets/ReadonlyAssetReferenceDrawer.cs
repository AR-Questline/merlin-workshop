using Awaken.TG.Assets;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Assets {
    [CustomPropertyDrawer(typeof(ReadOnlyAssetReference))]
    public class ReadonlyAssetReferenceDrawer : PropertyDrawer {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUIUtility.singleLineHeight;
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            var arReference = (ARAssetReference) property.FindPropertyRelative("reference").boxedValue;
            var entry = AddressableHelper.GetEntry(arReference);
            EditorGUI.ObjectField(position, label, entry?.MainAsset, typeof(Object), false);
        }
    }
}