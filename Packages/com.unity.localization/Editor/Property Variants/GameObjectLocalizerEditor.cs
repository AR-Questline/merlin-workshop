#if ENABLE_PROPERTY_VARIANTS || PACKAGE_DOCS_GENERATION

using UnityEngine;
using UnityEngine.Localization.PropertyVariants;

namespace UnityEditor.Localization.UI.PropertyVariants
{
    [CustomEditor(typeof(GameObjectLocalizer))]
    class GameObjectLocalizerEditor : UnityEditor.Editor
    {
        ReorderableListExtended m_TrackedObjectsList;

        /// <summary>
        /// Allows property drawers to know the current target being drawn.
        /// </summary>
        public static Object CurrentTarget { get; private set; }

        void OnEnable()
        {
            var trackedObjects = serializedObject.FindProperty("m_TrackedObjects");
            m_TrackedObjectsList = new ReorderableListExtended(serializedObject, trackedObjects);
            m_TrackedObjectsList.drawElementCallback = DrawElement;
            m_TrackedObjectsList.Header = EditorGUIUtility.TrTextContent("Tracked Objects");
            m_TrackedObjectsList.displayAdd = false;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            m_TrackedObjectsList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }

        void DrawElement(Rect rect, int idx, bool isActive, bool isFocused)
        {
            var element = m_TrackedObjectsList.serializedProperty.GetArrayElementAtIndex(idx);

            var trackedObject = element.FindPropertyRelative("m_Target");
            CurrentTarget = trackedObject.objectReferenceValue;

            var icon = trackedObject.objectReferenceValue != null ? AssetPreview.GetMiniTypeThumbnail(CurrentTarget.GetType()) : null;
            var label = new GUIContent(ManagedReferenceUtility.GetDisplayName(element.managedReferenceFullTypename).text, icon);

            rect.xMin += 8; // Prevent the foldout arrow(>) being drawn over the reorder icon(=) when showing LocalizationSettings in the inspector.
            EditorGUI.PropertyField(rect, element, label, true);
            CurrentTarget = null;
        }
    }
}

#endif
