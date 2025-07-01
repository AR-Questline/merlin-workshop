// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only] A custom Inspector for <see cref="AnimancerComponent"/>s.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/AnimancerComponentEditor
    /// 
    [CustomEditor(typeof(AnimancerComponent), true), CanEditMultipleObjects]
    public class AnimancerComponentEditor : BaseAnimancerComponentEditor
    {
        /************************************************************************************************************************/

        private bool _ShowResetOnDisableWarning;

        protected override bool DoOverridePropertyGUI(string path, SerializedProperty property, GUIContent label)
        {
            return default;
        }

        /************************************************************************************************************************/

        private void DoAnimatorGUI(SerializedProperty property, GUIContent label)
        {
        }

        /************************************************************************************************************************/

        private void DoActionOnDisableGUI(SerializedProperty property, GUIContent label)
        {
        }

        /************************************************************************************************************************/

        private bool AreAllResettingTargetsAboveTheirAnimator()
        {
            return default;
        }

        /************************************************************************************************************************/

        private void MoveResettingTargetsAboveTheirAnimator()
        {
        }

        /************************************************************************************************************************/
    }
}

#endif

