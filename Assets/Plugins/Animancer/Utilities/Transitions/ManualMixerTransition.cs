// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using Animancer.Editor;
using UnityEditor;
using UnityEditorInternal;
using static Animancer.Editor.AnimancerGUI;
#endif

namespace Animancer
{
    /// <inheritdoc/>
    /// https://kybernetik.com.au/animancer/api/Animancer/ManualMixerTransition
    [Serializable]
    public class ManualMixerTransition : ManualMixerTransition<ManualMixerState>,
        ManualMixerState.ITransition, ICopyable<ManualMixerTransition>
    {
        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override ManualMixerState CreateState()
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public virtual void CopyFrom(ManualMixerTransition copyFrom)
        {
        }

        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/

        /// <inheritdoc/>
        [CustomPropertyDrawer(typeof(ManualMixerTransition), true)]
        public class Drawer : TransitionDrawer
        {
            /************************************************************************************************************************/

            /// <summary>Should two lines be used to draw each child?</summary>
            public static readonly BoolPref
                TwoLineMode = new BoolPref(
                    nameof(ManualMixerTransition) + "." + nameof(Drawer) + "." + nameof(TwoLineMode),
                    "Two Line Mode",
                    true);

            /************************************************************************************************************************/

            /// <summary>The property this drawer is currently drawing.</summary>
            /// <remarks>Normally each property has its own drawer, but arrays share a single drawer for all elements.</remarks>
            public static SerializedProperty CurrentProperty { get; private set; }

            /// <summary>The <see cref="ManualMixerTransition{TState}.Animations"/> field.</summary>
            public static SerializedProperty CurrentAnimations { get; private set; }

            /// <summary>The <see cref="ManualMixerTransition{TState}.Speeds"/> field.</summary>
            public static SerializedProperty CurrentSpeeds { get; private set; }

            /// <summary>The <see cref="ManualMixerTransition{TState}.SynchronizeChildren"/> field.</summary>
            public static SerializedProperty CurrentSynchronizeChildren { get; private set; }

            private readonly Dictionary<string, ReorderableList>
                PropertyPathToStates = new Dictionary<string, ReorderableList>();

            private ReorderableList _MultiSelectDummyList;

            /************************************************************************************************************************/

            /// <summary>Gather the details of the `property`.</summary>
            /// <remarks>
            /// This method gets called by every <see cref="GetPropertyHeight"/> and <see cref="OnGUI"/> call since
            /// Unity uses the same <see cref="PropertyDrawer"/> instance for each element in a collection, so it
            /// needs to gather the details associated with the current property.
            /// </remarks>
            protected virtual ReorderableList GatherDetails(SerializedProperty property)
            {
                return default;
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Called every time a `property` is drawn to find the relevant child properties and store them to be
            /// used in <see cref="GetPropertyHeight"/> and <see cref="OnGUI"/>.
            /// </summary>
            protected virtual void GatherSubProperties(SerializedProperty property)
            {
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Adds a menu item that will call <see cref="GatherSubProperties"/> then run the specified
            /// `function`.
            /// </summary>
            protected void AddPropertyModifierFunction(GenericMenu menu, string label,
                MenuFunctionState state, Action<SerializedProperty> function)
            {
            }

            /// <summary>
            /// Adds a menu item that will call <see cref="GatherSubProperties"/> then run the specified
            /// `function`.
            /// </summary>
            protected void AddPropertyModifierFunction(GenericMenu menu, string label,
                Action<SerializedProperty> function)
            {
            }

            /************************************************************************************************************************/

            /// <inheritdoc/>
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return default;
            }

            /************************************************************************************************************************/

            private SerializedProperty _RootProperty;
            private ReorderableList _CurrentChildList;

            /// <inheritdoc/>
            public override void OnGUI(Rect area, SerializedProperty property, GUIContent label)
            {
            }

            /************************************************************************************************************************/

            /// <inheritdoc/>
            protected override void DoChildPropertyGUI(ref Rect area,
                SerializedProperty rootProperty, SerializedProperty property, GUIContent label)
            {
            }

            /************************************************************************************************************************/

            private static float _SpeedLabelWidth;
            private static float _SyncLabelWidth;

            /// <summary>Splits the specified `area` into separate sections.</summary>
            protected static void SplitListRect(Rect area, bool isHeader,
                out Rect animation, out Rect speed, out Rect sync)
            {
                animation = default(Rect);
                speed = default(Rect);
                sync = default(Rect);
            }

            /************************************************************************************************************************/
            #region Headers
            /************************************************************************************************************************/

            /// <summary>Draws the headdings of the child list.</summary>
            protected virtual void DoChildListHeaderGUI(Rect area)
            {
            }

            /************************************************************************************************************************/

            /// <summary>Draws an "Animation" header.</summary>
            protected void DoAnimationHeaderGUI(Rect area)
            {
            }

            /************************************************************************************************************************/
            #region Speeds
            /************************************************************************************************************************/

            /// <summary>Draws a "Speed" header.</summary>
            protected void DoSpeedHeaderGUI(Rect area)
            {
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Recalculates the <see cref="CurrentSpeeds"/> depending on the <see cref="AnimationClip.length"/> of
            /// their animations so that they all take the same amount of time to play fully.
            /// </summary>
            private static void NormalizeDurations(SerializedProperty property)
            {
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Initializes every element in the <see cref="CurrentSpeeds"/> array from the `start` to the end of
            /// the array to contain a value of 1.
            /// </summary>
            public static void InitializeSpeeds(int start)
            {
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
            #region Sync
            /************************************************************************************************************************/

            /// <summary>Draws a "Sync" header.</summary>
            protected void DoSyncHeaderGUI(Rect area)
            {
            }

            /************************************************************************************************************************/

            private static void SyncNone()
            {
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/

            /// <summary>Draws the GUI for a header dropdown button.</summary>
            public static void DoHeaderDropdownGUI(Rect area, SerializedProperty property, GUIContent content,
                Action<GenericMenu> populateMenu)
            {
            }

            /************************************************************************************************************************/

            /// <summary>Draws the footer of the child list.</summary>
            protected virtual void DoChildListFooterGUI(Rect area)
            {
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/

            /// <summary>Calculates the height of the state at the specified `index`.</summary>
            protected virtual float GetElementHeight(int index)
                => TwoLineMode
                ? LineHeight * 2
                : LineHeight;

            /************************************************************************************************************************/

            /// <summary>Draws the GUI of the state at the specified `index`.</summary>
            private void DoElementGUI(Rect area, int index, bool isActive, bool isFocused)
            {
            }

            /************************************************************************************************************************/

            /// <summary>Draws the GUI of the animation at the specified `index`.</summary>
            protected virtual void DoElementGUI(Rect area, int index,
                SerializedProperty animation, SerializedProperty speed)
            {
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Draws an <see cref="EditorGUI.ObjectField(Rect, GUIContent, Object, Type, bool)"/> that accepts
            /// <see cref="AnimationClip"/>s and <see cref="ITransition"/>s
            /// </summary>
            public static void DoAnimationField(Rect area, SerializedProperty property)
            {
            }

            /// <summary>Is the `clipOrTransition` an <see cref="AnimationClip"/> or <see cref="ITransition"/>?</summary>
            public static bool IsClipOrTransition(object clipOrTransition)
                => clipOrTransition is AnimationClip || clipOrTransition is ITransition;

            /************************************************************************************************************************/

            /// <summary>
            /// Draws a toggle to enable or disable <see cref="ManualMixerState.SynchronizedChildren"/> for the child at
            /// the specified `index`.
            /// </summary>
            protected void DoSpeedFieldGUI(Rect area, SerializedProperty speed, int index)
            {
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Draws a toggle to enable or disable <see cref="ManualMixerState.SynchronizedChildren"/> for the child at
            /// the specified `index`.
            /// </summary>
            protected void DoSyncToggleGUI(Rect area, int index)
            {
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Called when adding a new state to the list to ensure that any other relevant arrays have new
            /// elements added as well.
            /// </summary>
            private void OnAddElement(ReorderableList list)
            {
            }

            /// <summary>
            /// Called when adding a new state to the list to ensure that any other relevant arrays have new
            /// elements added as well.
            /// </summary>
            protected virtual void OnAddElement(int index)
            {
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Called when removing a state from the list to ensure that any other relevant arrays have elements
            /// removed as well.
            /// </summary>
            protected virtual void OnRemoveElement(ReorderableList list)
            {
            }

            /************************************************************************************************************************/

            /// <summary>Sets the number of items in the child list.</summary>
            protected virtual void ResizeList(int size)
            {
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Called when reordering states in the list to ensure that any other relevant arrays have their
            /// corresponding elements reordered as well.
            /// </summary>
            protected virtual void OnReorderList(ReorderableList list, int oldIndex, int newIndex)
            {
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Calls <see cref="TryCollapseSpeeds"/> and <see cref="TryCollapseSync"/>.
            /// </summary>
            public static void TryCollapseArrays()
            {
            }

            /************************************************************************************************************************/

            /// <summary>
            /// If every element in the <see cref="CurrentSpeeds"/> array is 1, this method sets the array size to 0.
            /// </summary>
            public static void TryCollapseSpeeds()
            {
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Removes any true elements from the end of the <see cref="CurrentSynchronizeChildren"/> array.
            /// </summary>
            public static void TryCollapseSync()
            {
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/
    }
}
