// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only] A custom Inspector for <see cref="NamedAnimancerComponent"/>s.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/NamedAnimancerComponentEditor
    /// 
    [CustomEditor(typeof(NamedAnimancerComponent), true), CanEditMultipleObjects]
    public class NamedAnimancerComponentEditor : AnimancerComponentEditor
    {
        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Draws any custom GUI for the `property`. The return value indicates whether the GUI should replace the
        /// regular call to <see cref="EditorGUILayout.PropertyField"/> or not.
        /// </summary>
        protected override bool DoOverridePropertyGUI(string path, SerializedProperty property, GUIContent label)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// The <see cref="NamedAnimancerComponent.PlayAutomatically"/> and
        /// <see cref="NamedAnimancerComponent.Animations"/> fields are only used on startup, so we don't need to show
        /// them in Play Mode after the object is already enabled.
        /// </summary>
        private bool ShouldShowAnimationFields()
        {
            return default;
        }

        /************************************************************************************************************************/

        private void DoDefaultAnimationField(SerializedProperty playAutomatically)
        {
        }

        /************************************************************************************************************************/

        private ReorderableList _Animations;
        private static int _RemoveAnimationIndex;

        private void DoAnimationsField(SerializedProperty property)
        {
        }

        /************************************************************************************************************************/

        private SerializedProperty _AnimationsArraySize;

        private void DrawAnimationsHeader(Rect area)
        {
        }

        /************************************************************************************************************************/

        private static readonly HashSet<Object>
            PreviousAnimations = new HashSet<Object>();

        private void DrawAnimationElement(Rect area, int index, bool isActive, bool isFocused)
        {
        }

        /************************************************************************************************************************/

        private static void RemoveSelectedElement(ReorderableList list)
        {
        }

        /************************************************************************************************************************/
    }
}

#endif

