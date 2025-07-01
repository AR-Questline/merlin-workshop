// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value.

using System;
using UnityEngine;

#if UNITY_EDITOR
using Animancer.Editor;
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Animancer
{
    /// <inheritdoc/>
    /// https://kybernetik.com.au/animancer/api/Animancer/MixerTransition_2
    [Serializable]
    public abstract class MixerTransition<TMixer, TParameter> : ManualMixerTransition<TMixer>,
        ICopyable<MixerTransition<TMixer, TParameter>>
        where TMixer : MixerState<TParameter>
    {
        /************************************************************************************************************************/

        [SerializeField]
        private TParameter[] _Thresholds;

        /// <summary>[<see cref="SerializeField"/>]
        /// The parameter values at which each of the states are used and blended.
        /// </summary>
        public ref TParameter[] Thresholds => ref _Thresholds;

        /// <summary>The name of the serialized backing field of <see cref="Thresholds"/>.</summary>
        public const string ThresholdsField = nameof(_Thresholds);

        /************************************************************************************************************************/

        [SerializeField]
        private TParameter _DefaultParameter;

        /// <summary>[<see cref="SerializeField"/>]
        /// The initial parameter value to give the mixer when it is first created.
        /// </summary>
        public ref TParameter DefaultParameter => ref _DefaultParameter;

        /// <summary>The name of the serialized backing field of <see cref="DefaultParameter"/>.</summary>
        public const string DefaultParameterField = nameof(_DefaultParameter);

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void InitializeState()
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public virtual void CopyFrom(MixerTransition<TMixer, TParameter> copyFrom)
        {
        }

        /************************************************************************************************************************/
    }

    /************************************************************************************************************************/

#if UNITY_EDITOR
    /// <summary>[Editor-Only] Draws the Inspector GUI for a <see cref="Transition{TMixer, TParameter}"/>.</summary>
    /// <remarks>
    /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/transitions">Transitions</see>
    /// and <see href="https://kybernetik.com.au/animancer/docs/manual/blending/mixers">Mixers</see>
    /// </remarks>
    /// https://kybernetik.com.au/animancer/api/Animancer/MixerTransitionDrawer
    /// 
    public class MixerTransitionDrawer : ManualMixerTransition.Drawer
    {
        /************************************************************************************************************************/

        /// <summary>The number of horizontal pixels the "Threshold" label occupies.</summary>
        private readonly float ThresholdWidth;

        /************************************************************************************************************************/

        private static float _StandardThresholdWidth;

        /// <summary>
        /// The number of horizontal pixels the word "Threshold" occupies when drawn with the
        /// <see cref="EditorStyles.popup"/> style.
        /// </summary>
        protected static float StandardThresholdWidth
        {
            get
            {
                if (_StandardThresholdWidth == 0)
                    _StandardThresholdWidth = AnimancerGUI.CalculateWidth(EditorStyles.popup, "Threshold");
                return _StandardThresholdWidth;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Creates a new <see cref="MixerTransitionDrawer"/> using the default <see cref="StandardThresholdWidth"/>.
        /// </summary>
        public MixerTransitionDrawer()
            : this(StandardThresholdWidth)
        {
        }

        /// <summary>
        /// Creates a new <see cref="MixerTransitionDrawer"/> using a custom width for its threshold labels.
        /// </summary>
        protected MixerTransitionDrawer(float thresholdWidth)
            => ThresholdWidth = thresholdWidth;

        /************************************************************************************************************************/

        /// <summary>
        /// The serialized <see cref="MixerTransition{TMixer, TParameter}.Thresholds"/> of the
        /// <see cref="ManualMixerTransition.Drawer.CurrentProperty"/>.
        /// </summary>
        protected static SerializedProperty CurrentThresholds { get; private set; }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void GatherSubProperties(SerializedProperty property)
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void DoChildPropertyGUI(ref Rect area, SerializedProperty rootProperty, SerializedProperty property, GUIContent label)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Splits the specified `area` into separate sections.</summary>
        protected void SplitListRect(Rect area, bool isHeader,
            out Rect animation, out Rect threshold, out Rect speed, out Rect sync)
        {
            animation = default(Rect);
            threshold = default(Rect);
            speed = default(Rect);
            sync = default(Rect);
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void DoChildListHeaderGUI(Rect area)
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void DoElementGUI(Rect area, int index,
            SerializedProperty animation, SerializedProperty speed)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Draws the GUI of the threshold at the specified `index`.</summary>
        protected virtual void DoThresholdGUI(Rect area, int index)
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void OnAddElement(int index)
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void OnRemoveElement(ReorderableList list)
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void ResizeList(int size)
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void OnReorderList(ReorderableList list, int oldIndex, int newIndex)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Adds functions to the `menu` relating to the thresholds.</summary>
        protected virtual void AddThresholdFunctionsToMenu(GenericMenu menu) {
        }

        /************************************************************************************************************************/
    }
#endif
}
