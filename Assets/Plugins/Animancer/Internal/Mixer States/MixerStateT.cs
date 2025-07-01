// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

using System;
using System.Text;
using UnityEngine;
using UnityEngine.Animations;

namespace Animancer
{
    /// <summary>[Pro-Only]
    /// Base class for mixers which blend an array of child states together based on a <see cref="Parameter"/>.
    /// </summary>
    /// <remarks>
    /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/blending/mixers">Mixers</see>
    /// </remarks>
    /// https://kybernetik.com.au/animancer/api/Animancer/MixerState_1
    /// 
    public abstract class MixerState<TParameter> : ManualMixerState, ICopyable<MixerState<TParameter>>
    {
        /************************************************************************************************************************/
        #region Properties
        /************************************************************************************************************************/

        /// <summary>The parameter values at which each of the child states are used and blended.</summary>
        private TParameter[] _Thresholds = Array.Empty<TParameter>();

        /************************************************************************************************************************/

        private TParameter _Parameter;

        /// <summary>The value used to calculate the weights of the child states.</summary>
        /// <remarks>
        /// Setting this value takes effect immediately (during the next animation update) without any
        /// <see href="https://kybernetik.com.au/animancer/docs/manual/blending/mixers#smoothing">Smoothing</see>.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">The value is NaN or Infinity.</exception>
        public TParameter Parameter
        {
            get => _Parameter;
            set
            {
#if UNITY_ASSERTIONS
                var error = GetParameterError(value);
                if (error != null)
                    throw new ArgumentOutOfRangeException(nameof(value), error);
#endif

                _Parameter = value;
                WeightsAreDirty = true;
                RequireUpdate();
            }
        }

        /// <summary>
        /// Returns an error message if the given `parameter` value can't be assigned to the <see cref="Parameter"/>.
        /// Otherwise returns null.
        /// </summary>
        public abstract string GetParameterError(TParameter parameter);

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Thresholds
        /************************************************************************************************************************/

        /// <summary>
        /// Has the array of thresholds been initialized with a size at least equal to the
        /// <see cref="ManualMixerState.ChildCount"/>.
        /// </summary>
        public bool HasThresholds
            => _Thresholds.Length >= ChildCount;

        /************************************************************************************************************************/

        /// <summary>Returns the value of the threshold associated with the specified `index`.</summary>
        public TParameter GetThreshold(int index)
            => _Thresholds[index];

        /************************************************************************************************************************/

        /// <summary>Sets the value of the threshold associated with the specified `index`.</summary>
        public void SetThreshold(int index, TParameter threshold)
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Assigns the specified array as the thresholds to use for blending.
        /// <para></para>
        /// WARNING: if you keep a reference to the `thresholds` array you must call <see cref="OnThresholdsChanged"/>
        /// whenever any changes are made to it, otherwise this mixer may not blend correctly.
        /// </summary>
        public void SetThresholds(params TParameter[] thresholds)
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// If the <see cref="Array.Length"/> of the <see cref="_Thresholds"/> is below the
        /// <see cref="AnimancerNode.ChildCount"/>, this method assigns a new array with size equal to the
        /// <see cref="ManualMixerState.ChildCapacity"/> and returns true.
        /// </summary>
        public bool ValidateThresholdCount()
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Called whenever the thresholds are changed. By default this method simply indicates that the blend weights
        /// need recalculating but it can be overridden by child classes to perform validation checks or optimisations.
        /// </summary>
        public virtual void OnThresholdsChanged()
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Calls `calculate` for each of the <see cref="ManualMixerState.ChildStates"/> and stores the returned value
        /// as the threshold for that state.
        /// </summary>
        public void CalculateThresholds(Func<AnimancerState, TParameter> calculate)
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Stores the values of all parameters, calls <see cref="AnimancerNode.DestroyPlayable"/>, then restores the
        /// parameter values.
        /// </summary>
        public override void RecreatePlayable()
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Initialization
        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void OnChildCapacityChanged()
        {
        }

        /************************************************************************************************************************/

        /// <summary>Assigns the `state` as a child of this mixer and assigns the `threshold` for it.</summary>
        public void Add(AnimancerState state, TParameter threshold)
        {
        }

        /// <summary>
        /// Creates and returns a new <see cref="ClipState"/> to play the `clip` as a child of this mixer, and assigns
        /// the `threshold` for it.
        /// </summary>
        public ClipState Add(AnimationClip clip, TParameter threshold)
        {
            return default;
        }

        /// <summary>
        /// Calls <see cref="AnimancerUtilities.CreateStateAndApply"/> then 
        /// <see cref="Add(AnimancerState, TParameter)"/>.
        /// </summary>
        public AnimancerState Add(Animancer.ITransition transition, TParameter threshold)
        {
            return default;
        }

        /// <summary>Calls one of the other <see cref="Add(object, TParameter)"/> overloads as appropriate.</summary>
        public AnimancerState Add(object child, TParameter threshold)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        void ICopyable<MixerState<TParameter>>.CopyFrom(MixerState<TParameter> copyFrom)
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Descriptions
        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override string GetDisplayKey(AnimancerState state) => $"[{state.Index}] {_Thresholds[state.Index]}";

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void AppendDetails(StringBuilder text, string separator)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Appends the `parameter` in a viewer-friendly format.</summary>
        public virtual void AppendParameter(StringBuilder description, TParameter parameter)
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

