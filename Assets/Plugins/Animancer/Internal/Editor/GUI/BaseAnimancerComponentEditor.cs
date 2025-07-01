// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only] A custom Inspector for <see cref="IAnimancerComponent"/>s.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/BaseAnimancerComponentEditor
    /// 
    public abstract class BaseAnimancerComponentEditor : UnityEditor.Editor
    {
        /************************************************************************************************************************/

        [NonSerialized]
        private IAnimancerComponent[] _Targets;
        /// <summary><see cref="UnityEditor.Editor.targets"/> casted to <see cref="IAnimancerComponent"/>.</summary>
        public IAnimancerComponent[] Targets => _Targets;

        /// <summary>The drawer for the <see cref="IAnimancerComponent.Playable"/>.</summary>
        private readonly AnimancerPlayableDrawer
            PlayableDrawer = new AnimancerPlayableDrawer();

        /************************************************************************************************************************/

        /// <summary>Initializes this <see cref="UnityEditor.Editor"/>.</summary>
        protected virtual void OnEnable()
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Copies the <see cref="UnityEditor.Editor.targets"/> into the <see cref="_Targets"/> array.
        /// </summary>
        private void GatherTargets()
        {
        }

        /************************************************************************************************************************/

        /// <summary>Called by the Unity editor to draw the custom Inspector GUI elements.</summary>
        public override void OnInspectorGUI()
        {
        }

        /************************************************************************************************************************/

        [NonSerialized]
        private double _LastRepaintTime = double.NegativeInfinity;

        /// <summary>
        /// If we have only one object selected and are in Play Mode, we need to constantly repaint to keep the
        /// Inspector up to date with the latest details.
        /// </summary>
        public override bool RequiresConstantRepaint()
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Draws the rest of the Inspector fields after the Animator field.</summary>
        protected void DoOtherFieldsGUI()
        {
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Draws any custom GUI for the `property`.
        /// The return value indicates whether the GUI should replace the regular call to
        /// <see cref="EditorGUILayout.PropertyField(SerializedProperty, GUIContent, bool, GUILayoutOption[])"/> or
        /// not. True = GUI was drawn, so don't draw the regular GUI. False = Draw the regular GUI.
        /// </summary>
        protected virtual bool DoOverridePropertyGUI(string path, SerializedProperty property, GUIContent label) => false;

        /************************************************************************************************************************/
    }
}

#endif

