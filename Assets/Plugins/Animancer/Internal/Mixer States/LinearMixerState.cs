// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

using System;
using System.Text;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace Animancer
{
    /// <summary>[Pro-Only]
    /// An <see cref="AnimancerState"/> which blends an array of other states together using linear interpolation
    /// between the specified thresholds.
    /// </summary>
    /// <remarks>
    /// This mixer type is similar to the 1D Blend Type in Mecanim Blend Trees.
    /// <para></para>
    /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/blending/mixers">Mixers</see>
    /// </remarks>
    /// https://kybernetik.com.au/animancer/api/Animancer/LinearMixerState
    /// 
    public class LinearMixerState : MixerState<float>, ICopyable<LinearMixerState>
    {
        /************************************************************************************************************************/

        /// <summary>An <see cref="ITransition{TState}"/> that creates a <see cref="LinearMixerState"/>.</summary>
        public new interface ITransition : ITransition<LinearMixerState> { }

        /************************************************************************************************************************/

        private bool _ExtrapolateSpeed = false;

        /// <summary>
        /// Should setting the <see cref="MixerState{TParameter}.Parameter"/> above the highest threshold increase the
        /// <see cref="AnimancerNode.Speed"/> of this mixer proportionally?
        /// </summary>
        public bool ExtrapolateSpeed
        {
            get => _ExtrapolateSpeed;
            set
            {
                if (_ExtrapolateSpeed == value)
                    return;

                _ExtrapolateSpeed = value;

                if (!_Playable.IsValid())
                    return;

                var speed = Speed;

                var childCount = ChildCount;
                if (value && childCount > 0)
                {
                    var threshold = GetThreshold(childCount - 1);
                    if (Parameter > threshold)
                        speed *= Parameter / threshold;
                }

                _Playable.SetSpeed(speed);
            }
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override string GetParameterError(float value)
            => value.IsFinite() ? null : Strings.MustBeFinite;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override AnimancerState Clone(AnimancerPlayable root)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        void ICopyable<LinearMixerState>.CopyFrom(LinearMixerState copyFrom)
        {
        }

        /************************************************************************************************************************/
#if UNITY_ASSERTIONS
        /************************************************************************************************************************/

        private bool _NeedToCheckThresholdSorting;

        /// <summary>
        /// Called whenever the thresholds are changed. Indicates that <see cref="AssertThresholdsSorted"/> needs to
        /// be called by the next <see cref="ForceRecalculateWeights"/> if UNITY_ASSERTIONS is defined, then calls
        /// <see cref="MixerState{TParameter}.OnThresholdsChanged"/>.
        /// </summary>
        public override void OnThresholdsChanged()
        {
        }

        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> if the thresholds are not sorted from lowest to highest without
        /// any duplicates.
        /// </summary>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="InvalidOperationException">The thresholds have not been initialized.</exception>
        public void AssertThresholdsSorted()
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Recalculates the weights of all <see cref="ManualMixerState.ChildStates"/> based on the
        /// <see cref="MixerState{TParameter}.Parameter"/> and the thresholds.
        /// </summary>
        protected override void ForceRecalculateWeights()
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Assigns the thresholds to be evenly spaced between the specified min and max (inclusive).
        /// </summary>
        public LinearMixerState AssignLinearThresholds(float min = 0, float max = 1)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void AppendDetails(StringBuilder text, string separator)
        {
        }

        /************************************************************************************************************************/
        #region Inspector
        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override int ParameterCount => 1;

        /// <inheritdoc/>
        protected override string GetParameterName(int index) => "Parameter";

        /// <inheritdoc/>
        protected override AnimatorControllerParameterType GetParameterType(int index) => AnimatorControllerParameterType.Float;

        /// <inheritdoc/>
        protected override object GetParameterValue(int index) => Parameter;

        /// <inheritdoc/>
        protected override void SetParameterValue(int index, object value) => Parameter = (float)value;

        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Returns a <see cref="Drawer"/> for this state.</summary>
        protected internal override Editor.IAnimancerNodeDrawer CreateDrawer() => new Drawer(this);

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public class Drawer : Drawer<LinearMixerState>
        {
            /************************************************************************************************************************/

            /// <summary>
            /// Creates a new <see cref="Drawer"/> to manage the Inspector GUI for the `state`.
            /// </summary>
            public Drawer(LinearMixerState state) : base(state)
            {
            }

            /************************************************************************************************************************/

            /// <inheritdoc/>
            protected override void AddContextMenuFunctions(UnityEditor.GenericMenu menu)
            {
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

