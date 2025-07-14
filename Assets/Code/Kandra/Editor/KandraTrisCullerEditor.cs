using Awaken.Utility.UI;
using UnityEditor;
using UnityEngine;

namespace Awaken.Kandra.Editor {
    [CustomEditor(typeof(KandraTrisCuller))]
    public class KandraTrisCullerEditor : UnityEditor.Editor {
        bool _showDebug;

        KandraTrisCullee _cullTarget;
        KandraTrisCullee _uncullTarget;

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            _showDebug = EditorGUILayout.Foldout(_showDebug, "Debug", true);
            if (!_showDebug) {
                return;
            }
            ++EditorGUI.indentLevel;

            var culler = (KandraTrisCuller)target;

            var cullees = KandraTrisCuller.EditorAccess.Cullees(culler);
            EditorGUILayout.LabelField($"Cullees: {cullees.Count}", EditorStyles.boldLabel);
            foreach (var cullee in cullees) {
                EditorGUILayout.ObjectField(cullee, typeof(KandraTrisCullee), true);
            }

            EditorGUILayout.BeginHorizontal();
            {
                _cullTarget = (KandraTrisCullee)EditorGUILayout.ObjectField("Cull Target:", _cullTarget, typeof(KandraTrisCullee), true);

                using var disabledScope = new EditorGUI.DisabledScope(_cullTarget == null);
                if (GUILayout.Button("Cull")) {
                    culler.Cull(_cullTarget);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                _uncullTarget = (KandraTrisCullee)EditorGUILayout.ObjectField("Uncull Target:", _uncullTarget, typeof(KandraTrisCullee), true);

                using var disabledScope = new EditorGUI.DisabledScope(_uncullTarget == null);
                if (GUILayout.Button("Uncull")) {
                    culler.Uncull(_uncullTarget);
                }
            }
            EditorGUILayout.EndHorizontal();

            -- EditorGUI.indentLevel;
        }
    }

    [CustomPropertyDrawer(typeof(KandraTrisCuller.CulledRange))]
    public class KandraTrisCullerCulledRangeDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            var propertyRect = (PropertyDrawerRects)position;
            var startRect = propertyRect.AllocateLeftNormalized(0.5f);
            var lengthRect = (Rect)propertyRect;

            var startProperty = property.FindPropertyRelative(nameof(KandraTrisCuller.CulledRange.start));
            var lengthProperty = property.FindPropertyRelative(nameof(KandraTrisCuller.CulledRange.length));

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.PropertyField(startRect, startProperty, new GUIContent("Start"), true);
            EditorGUI.PropertyField(lengthRect, lengthProperty, new GUIContent("Length"), true);
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}