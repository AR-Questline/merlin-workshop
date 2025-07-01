// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

using System;
using System.Text;
using UnityEngine;

namespace Animancer
{
    /// <summary>[Pro-Only]
    /// An <see cref="AnimancerState"/> which blends an array of other states together based on a two dimensional
    /// parameter and thresholds using Gradient Band Interpolation.
    /// </summary>
    /// <remarks>
    /// This mixer type is similar to the 2D Freeform Cartesian Blend Type in Mecanim Blend Trees.
    /// <para></para>
    /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/blending/mixers">Mixers</see>
    /// </remarks>
    /// https://kybernetik.com.au/animancer/api/Animancer/CartesianMixerState
    /// 
    public class CartesianMixerState : MixerState<Vector2>, ICopyable<CartesianMixerState>
    {
        /************************************************************************************************************************/

        /// <summary><see cref="MixerState{TParameter}.Parameter"/>.x.</summary>
        public float ParameterX
        {
            get => Parameter.x;
            set => Parameter = new Vector2(value, Parameter.y);
        }

        /// <summary><see cref="MixerState{TParameter}.Parameter"/>.y.</summary>
        public float ParameterY
        {
            get => Parameter.y;
            set => Parameter = new Vector2(Parameter.x, value);
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override string GetParameterError(Vector2 value)
            => value.IsFinite() ? null : $"value.x and value.y {Strings.MustBeFinite}";

        /************************************************************************************************************************/

        /// <summary>Precalculated values to speed up the recalculation of weights.</summary>
        private Vector2[][] _BlendFactors;

        /// <summary>Indicates whether the <see cref="_BlendFactors"/> need to be recalculated.</summary>
        private bool _BlendFactorsDirty = true;

        /************************************************************************************************************************/

        /// <summary>
        /// Called whenever the thresholds are changed. Indicates that the internal blend factors need to be
        /// recalculated and calls <see cref="ForceRecalculateWeights"/>.
        /// </summary>
        public override void OnThresholdsChanged()
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Recalculates the weights of all <see cref="ManualMixerState.ChildStates"/> based on the current value of the
        /// <see cref="MixerState{TParameter}.Parameter"/> and the <see cref="MixerState{TParameter}._Thresholds"/>.
        /// </summary>
        protected override void ForceRecalculateWeights()
        {
        }

        /************************************************************************************************************************/

        private void CalculateBlendFactors(int childCount)
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override AnimancerState Clone(AnimancerPlayable root)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        void ICopyable<CartesianMixerState>.CopyFrom(CartesianMixerState copyFrom)
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void AppendParameter(StringBuilder text, Vector2 parameter)
        {
        }

        /************************************************************************************************************************/
        #region Inspector
        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override int ParameterCount => 2;

        /// <inheritdoc/>
        protected override string GetParameterName(int index)
        {
            return default;
        }

        /// <inheritdoc/>
        protected override AnimatorControllerParameterType GetParameterType(int index) => AnimatorControllerParameterType.Float;

        /// <inheritdoc/>
        protected override object GetParameterValue(int index)
        {
            return default;
        }

        /// <inheritdoc/>
        protected override void SetParameterValue(int index, object value)
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

