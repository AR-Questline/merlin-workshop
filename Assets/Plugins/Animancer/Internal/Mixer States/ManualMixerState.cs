// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value.

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Animancer
{
    /// <summary>[Pro-Only]
    /// An <see cref="AnimancerState"/> which blends multiple child states by allowing you to control their
    /// <see cref="AnimancerNode.Weight"/> manually.
    /// </summary>
    /// <remarks>
    /// This mixer type is similar to the Direct Blend Type in Mecanim Blend Trees.
    /// The official <see href="https://learn.unity.com/tutorial/5c5152bcedbc2a001fd5c696">Direct Blend Trees</see>
    /// tutorial explains their general concepts and purpose which apply to <see cref="ManualMixerState"/>s as well.
    /// <para></para>
    /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/blending/mixers">Mixers</see>
    /// </remarks>
    /// https://kybernetik.com.au/animancer/api/Animancer/ManualMixerState
    /// 
    public partial class ManualMixerState : AnimancerState, ICopyable<ManualMixerState>
    {
        /************************************************************************************************************************/

        /// <summary>An <see cref="ITransition{TState}"/> that creates a <see cref="ManualMixerState"/>.</summary>
        public interface ITransition : ITransition<ManualMixerState> { }

        /// <summary>
        /// An <see cref="ITransition{TState}"/> that creates a <see cref="MixerState{TParameter}"/> for
        /// <see cref="Vector2"/>.
        /// </summary>
        public interface ITransition2D : ITransition<MixerState<Vector2>> { }

        /************************************************************************************************************************/
        #region Properties
        /************************************************************************************************************************/

        /// <summary>Returns true because mixers should always keep child playables connected to the graph.</summary>
        public override bool KeepChildrenConnected => true;

        /// <summary>A <see cref="ManualMixerState"/> has no <see cref="AnimationClip"/>.</summary>
        public override AnimationClip Clip => null;

        /************************************************************************************************************************/

        /// <summary>The states connected to this mixer.</summary>
        /// <remarks>Only states up to the <see cref="ChildCount"/> should be assigned.</remarks>
        protected AnimancerState[] ChildStates { get; private set; } = Array.Empty<AnimancerState>();

        /************************************************************************************************************************/

        private int _ChildCount;

        /// <inheritdoc/>
        public sealed override int ChildCount
            => _ChildCount;

        /************************************************************************************************************************/

        /// <summary>The size of the internal array of <see cref="ChildStates"/>.</summary>
        /// <remarks>This value starts at 0 then expands to <see cref="ChildCapacity"/> when the first child is added.</remarks>
        public int ChildCapacity
        {
            get => ChildStates.Length;
            set
            {
                if (value == ChildStates.Length)
                    return;

#if UNITY_ASSERTIONS
                if (value <= 1 && OptionalWarning.MixerMinChildren.IsEnabled())
                    OptionalWarning.MixerMinChildren.Log(
                        $"The {nameof(ChildCapacity)} of '{this}' is being set to {value}." +
                        $" The purpose of a mixer is to mix multiple child states so this may be a mistake.",
                        Root?.Component);
#endif

                var newChildStates = new AnimancerState[value];
                if (value > _ChildCount)// Increase size.
                {
                    Array.Copy(ChildStates, newChildStates, _ChildCount);
                }
                else// Decrease size.
                {
                    for (int i = value; i < _ChildCount; i++)
                        ChildStates[i].Destroy();

                    Array.Copy(ChildStates, newChildStates, value);
                    _ChildCount = value;
                }

                ChildStates = newChildStates;

                if (_Playable.IsValid())
                {
                    _Playable.SetInputCount(value);
                }
                else if (Root != null)
                {
                    CreatePlayable();
                }

                OnChildCapacityChanged();
            }
        }

        /// <summary>Called when the <see cref="ChildCapacity"/> is changed.</summary>
        protected virtual void OnChildCapacityChanged() {
        }

        /// <summary><see cref="ChildCapacity"/> starts at 0 then expands to this value when the first child is added.</summary>
        public static int DefaultChildCapacity { get; set; } = 8;

        /// <summary>
        /// Ensures that the remaining unused <see cref="ChildCapacity"/> is greater than or equal to the
        /// `minimumCapacity`.
        /// </summary>
        public void EnsureRemainingChildCapacity(int minimumCapacity)
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public sealed override AnimancerState GetChild(int index)
            => ChildStates[index];

        /// <inheritdoc/>
        public sealed override FastEnumerator<AnimancerState> GetEnumerator()
            => new FastEnumerator<AnimancerState>(ChildStates, _ChildCount);

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void OnSetIsPlaying()
        {
        }

        /************************************************************************************************************************/

        /// <summary>Are any child states looping?</summary>
        public override bool IsLooping
        {
            get
            {
                for (int i = _ChildCount - 1; i >= 0; i--)
                    if (ChildStates[i].IsLooping)
                        return true;

                return false;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// The weighted average <see cref="AnimancerState.Time"/> of each child state according to their
        /// <see cref="AnimancerNode.Weight"/>.
        /// </summary>
        /// <remarks>
        /// If there are any <see cref="SynchronizedChildren"/>, only those states will be included in the getter
        /// calculation.
        /// </remarks>
        public override double RawTime
        {
            get
            {
                RecalculateWeights();

                if (!GetSynchronizedTimeDetails(out var totalWeight, out var normalizedTime, out var length))
                    GetTimeDetails(out totalWeight, out normalizedTime, out length);

                if (totalWeight == 0)
                    return base.RawTime;

                totalWeight *= totalWeight;
                return normalizedTime * length / totalWeight;
            }
            set
            {
                if (value == 0)
                    goto ZeroTime;

                var length = Length;
                if (length == 0)
                    goto ZeroTime;

                value /= length;// Normalize.

                for (int i = _ChildCount - 1; i >= 0; i--)
                    ChildStates[i].NormalizedTimeD = value;

                return;

                // If the value is 0, we can set the child times more efficiently.
                ZeroTime:
                for (int i = _ChildCount - 1; i >= 0; i--)
                    ChildStates[i].TimeD = 0;
            }
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void MoveTime(double time, bool normalized)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Gets the time details based on the <see cref="SynchronizedChildren"/>.</summary>
        private bool GetSynchronizedTimeDetails(out float totalWeight, out float normalizedTime, out float length)
        {
            totalWeight = default(float);
            normalizedTime = default(float);
            length = default(float);
            return default;
        }

        /// <summary>Gets the time details based on all child states.</summary>
        private void GetTimeDetails(out float totalWeight, out float normalizedTime, out float length)
        {
            totalWeight = default(float);
            normalizedTime = default(float);
            length = default(float);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// The weighted average <see cref="AnimancerState.Length"/> of each child state according to their
        /// <see cref="AnimancerNode.Weight"/>.
        /// </summary>
        public override float Length
        {
            get
            {
                RecalculateWeights();

                var length = 0f;
                var totalChildWeight = 0f;

                if (_SynchronizedChildren != null)
                {
                    for (int i = _SynchronizedChildren.Count - 1; i >= 0; i--)
                    {
                        var state = _SynchronizedChildren[i];
                        var weight = state.Weight;
                        if (weight == 0)
                            continue;

                        var stateLength = state.Length;
                        if (stateLength == 0)
                            continue;

                        totalChildWeight += weight;
                        length += stateLength * weight;
                    }
                }

                if (totalChildWeight > 0)
                    return length / totalChildWeight;

                totalChildWeight = CalculateTotalWeight(ChildStates, _ChildCount);
                if (totalChildWeight <= 0)
                    return 0;

                for (int i = _ChildCount - 1; i >= 0; i--)
                {
                    var state = ChildStates[i];
                    length += state.Length * state.Weight;
                }

                return length / totalChildWeight;
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Initialization
        /************************************************************************************************************************/

        /// <summary>Creates and assigns the <see cref="AnimationMixerPlayable"/> managed by this state.</summary>
        protected override void CreatePlayable(out Playable playable)
        {
            playable = default(Playable);
        }

        /************************************************************************************************************************/

        /// <summary>Connects the `state` to this mixer at its <see cref="AnimancerNode.Index"/>.</summary>
        protected internal override void OnAddChild(AnimancerState state)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Disconnects the `state` from this mixer at its <see cref="AnimancerNode.Index"/>.</summary>
        protected internal override void OnRemoveChild(AnimancerState state)
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void Destroy()
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override AnimancerState Clone(AnimancerPlayable root)
        {
            return default;
        }

        /// <inheritdoc/>
        void ICopyable<ManualMixerState>.CopyFrom(ManualMixerState copyFrom)
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Child Configuration
        /************************************************************************************************************************/

        /// <summary>Assigns the `state` as a child of this mixer.</summary>
        /// <remarks>The `state` must not be null. To remove a child, call <see cref="Remove(int, bool)"/> instead.</remarks>
        public void Add(AnimancerState state)
        {
        }

        /// <summary>Creates and returns a new <see cref="ClipState"/> to play the `clip` as a child of this mixer.</summary>
        public ClipState Add(AnimationClip clip)
        {
            return default;
        }

        /// <summary>Calls <see cref="AnimancerUtilities.CreateStateAndApply"/> then <see cref="Add(AnimancerState)"/>.</summary>
        public AnimancerState Add(Animancer.ITransition transition)
        {
            return default;
        }

        /// <summary>Calls one of the other <see cref="Add(object)"/> overloads as appropriate for the `child`.</summary>
        public AnimancerState Add(object child)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Calls <see cref="Add(AnimationClip)"/> for each of the `clips`.</summary>
        public void AddRange(IList<AnimationClip> clips)
        {
        }

        /// <summary>Calls <see cref="Add(AnimationClip)"/> for each of the `clips`.</summary>
        public void AddRange(params AnimationClip[] clips)
            => AddRange((IList<AnimationClip>)clips);

        /************************************************************************************************************************/

        /// <summary>Calls <see cref="Add(Animancer.ITransition)"/> for each of the `transitions`.</summary>
        public void AddRange(IList<Animancer.ITransition> transitions)
        {
        }

        /// <summary>Calls <see cref="Add(Animancer.ITransition)"/> for each of the `clips`.</summary>
        public void AddRange(params Animancer.ITransition[] clips)
            => AddRange((IList<Animancer.ITransition>)clips);

        /************************************************************************************************************************/

        /// <summary>Calls <see cref="Add(object)"/> for each of the `children`.</summary>
        public void AddRange(IList<object> children)
        {
        }

        /// <summary>Calls <see cref="Add(object)"/> for each of the `clips`.</summary>
        public void AddRange(params object[] clips)
            => AddRange((IList<object>)clips);

        /************************************************************************************************************************/

        /// <summary>Removes the child at the specified `index`.</summary>
        public void Remove(int index, bool destroy)
            => Remove(ChildStates[index], destroy);

        /// <summary>Removes the specified `child`.</summary>
        public void Remove(AnimancerState child, bool destroy)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Replaces the `child` at the specified `index`.</summary>
        public void Set(int index, AnimancerState child, bool destroyPrevious)
        {
        }

        /// <summary>Replaces the child at the specified `index` with a new <see cref="ClipState"/>.</summary>
        public ClipState Set(int index, AnimationClip clip, bool destroyPrevious)
        {
            return default;
        }

        /// <summary>Replaces the child at the specified `index` with a <see cref="Animancer.ITransition.CreateState"/>.</summary>
        public AnimancerState Set(int index, Animancer.ITransition transition, bool destroyPrevious)
        {
            return default;
        }

        /// <summary>Calls one of the other <see cref="Set(int, object, bool)"/> overloads as appropriate for the `child`.</summary>
        public AnimancerState Set(int index, object child, bool destroyPrevious)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Returns the index of the specified `child` state.</summary>
        public int IndexOf(AnimancerState child)
            => Array.IndexOf(ChildStates, child, 0, _ChildCount);

        /************************************************************************************************************************/

        /// <summary>
        /// Destroys all <see cref="ChildStates"/> connected to this mixer. This operation cannot be undone.
        /// </summary>
        public void DestroyChildren()
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Jobs
        /************************************************************************************************************************/

        /// <summary>
        /// Creates an <see cref="AnimationScriptPlayable"/> to run the specified Animation Job instead of the usual
        /// <see cref="AnimationMixerPlayable"/>.
        /// </summary>
        /// <example><code>
        /// AnimancerComponent animancer = ...;
        /// var job = new MyJob();// A struct that implements IAnimationJob.
        /// var mixer = new WhateverMixerState();// e.g. LinearMixerState.
        /// mixer.CreatePlayable(animancer, job);
        /// // Use mixer.Initialize, CreateChild, and SetChild to configure the children as normal.
        /// </code>
        /// See also: <seealso cref="CreatePlayable{T}(out Playable, T, bool)"/>
        /// </example>
        public AnimationScriptPlayable CreatePlayable<T>(AnimancerPlayable root, T job, bool processInputs = false)
            where T : struct, IAnimationJob
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Creates an <see cref="AnimationScriptPlayable"/> to run the specified Animation Job instead of the usual
        /// <see cref="AnimationMixerPlayable"/>.
        /// </summary>
        /// 
        /// <remarks>
        /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/source/creating-custom-states">Creating Custom States</see>
        /// </remarks>
        /// 
        /// <example><code>
        /// public class MyMixer : LinearMixerState
        /// {
        ///     protected override void CreatePlayable(out Playable playable)
        ///     {
        ///         CreatePlayable(out playable, new MyJob());
        ///     }
        /// 
        ///     private struct MyJob : IAnimationJob
        ///     {
        ///         public void ProcessAnimation(AnimationStream stream)
        ///         {
        ///         }
        /// 
        ///         public void ProcessRootMotion(AnimationStream stream)
        ///         {
        ///         }
        ///     }
        /// }
        /// </code>
        /// See also: <seealso cref="CreatePlayable{T}(AnimancerPlayable, T, bool)"/>
        /// </example>
        protected void CreatePlayable<T>(out Playable playable, T job, bool processInputs = false)
            where T : struct, IAnimationJob
        {
            playable = default(Playable);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Gets the Animation Job data from the <see cref="AnimationScriptPlayable"/>.
        /// </summary>
        /// <exception cref="InvalidCastException">
        /// This mixer was not initialized using <see cref="CreatePlayable{T}(AnimancerPlayable, T, bool)"/>
        /// or <see cref="CreatePlayable{T}(out Playable, T, bool)"/>.
        /// </exception>
        public T GetJobData<T>()
            where T : struct, IAnimationJob
            => ((AnimationScriptPlayable)_Playable).GetJobData<T>();

        /// <summary>
        /// Sets the Animation Job data in the <see cref="AnimationScriptPlayable"/>.
        /// </summary>
        /// <exception cref="InvalidCastException">
        /// This mixer was not initialized using <see cref="CreatePlayable{T}(AnimancerPlayable, T, bool)"/>
        /// or <see cref="CreatePlayable{T}(out Playable, T, bool)"/>.
        /// </exception>
        public void SetJobData<T>(T value)
            where T : struct, IAnimationJob
            => ((AnimationScriptPlayable)_Playable).SetJobData<T>(value);

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Updates
        /************************************************************************************************************************/

        /// <summary>Updates the time of this mixer and all of its child states.</summary>
        protected internal override void Update(out bool needsMoreUpdates)
        {
            needsMoreUpdates = default(bool);
        }

        /************************************************************************************************************************/

        /// <summary>Should the weights of all child states be recalculated?</summary>
        public bool WeightsAreDirty { get; set; }

        /************************************************************************************************************************/

        /// <summary>
        /// If <see cref="WeightsAreDirty"/> this method recalculates the weights of all child states and returns true.
        /// </summary>
        public bool RecalculateWeights()
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Recalculates the weights of all child states based on the current value of the
        /// <see cref="MixerState{TParameter}.Parameter"/> and the thresholds.
        /// </summary>
        /// <remarks>Overrides of this method must set <see cref="WeightsAreDirty"/> = false.</remarks>
        protected virtual void ForceRecalculateWeights()
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Synchronize Children
        /************************************************************************************************************************/

        /// <summary>Should newly added children be automatically added to the synchronization list? Default true.</summary>
        public static bool SynchronizeNewChildren { get; set; } = true;

        /// <summary>The minimum total weight of all children for their times to be synchronized. Default 0.01.</summary>
        public static float MinimumSynchronizeChildrenWeight { get; set; } = 0.01f;

        /************************************************************************************************************************/

        private List<AnimancerState> _SynchronizedChildren;

        /// <summary>A copy of the internal list of child states that will have their times synchronized.</summary>
        /// <remarks>
        /// If this mixer is a child of another mixer, its synchronized children will be managed by the parent.
        /// <para></para>
        /// The getter allocates a new array if <see cref="SynchronizedChildCount"/> is greater than zero.
        /// </remarks>
        public AnimancerState[] SynchronizedChildren
        {
            get => SynchronizedChildCount > 0 ? _SynchronizedChildren.ToArray() : Array.Empty<AnimancerState>();
            set
            {
                if (_SynchronizedChildren == null)
                    _SynchronizedChildren = new List<AnimancerState>();
                else
                    _SynchronizedChildren.Clear();

                for (int i = 0; i < value.Length; i++)
                    Synchronize(value[i]);
            }
        }

        /// <summary>The number of <see cref="SynchronizedChildren"/>.</summary>
        public int SynchronizedChildCount => _SynchronizedChildren != null ? _SynchronizedChildren.Count : 0;

        /************************************************************************************************************************/

        /// <summary>Is the `state` in the <see cref="SynchronizedChildren"/>?</summary>
        public bool IsSynchronized(AnimancerState state)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Adds the `state` to the <see cref="SynchronizedChildren"/>.</summary>
        /// <remarks>
        /// The `state` must be a child of this mixer.
        /// <para></para>
        /// If this mixer is a child of another mixer, the `state` will be added to the parent's
        /// <see cref="SynchronizedChildren"/> instead.
        /// </remarks>
        public void Synchronize(AnimancerState state)
        {
        }

        /// <summary>The internal implementation of <see cref="Synchronize"/>.</summary>
        private void SynchronizeDirect(AnimancerState state)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Removes the `state` from the <see cref="SynchronizedChildren"/>.</summary>
        public void DontSynchronize(AnimancerState state)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Removes all children of this mixer from the <see cref="SynchronizedChildren"/>.</summary>
        public void DontSynchronizeChildren()
        {
        }

        /************************************************************************************************************************/

        /// <summary>Initializes the internal <see cref="SynchronizedChildren"/> list.</summary>
        /// <remarks>
        /// The array can be null or empty. Any elements not in the array will be treated as <c>true</c>.
        /// <para></para>
        /// This method can only be called before any <see cref="SynchronizedChildren"/> are added and also before this
        /// mixer is made the child of another mixer.
        /// </remarks>
        public void InitializeSynchronizedChildren(params bool[] synchronizeChildren)
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns the highest <see cref="ManualMixerState"/> in the hierarchy above this mixer or this mixer itself if
        /// there are none above it.
        /// </summary>
        public ManualMixerState GetParentMixer()
        {
            return default;
        }

        /// <summary>Returns the highest <see cref="ManualMixerState"/> in the hierarchy above the `state` (inclusive).</summary>
        public static ManualMixerState GetParentMixer(IPlayableWrapper node)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Is the `child` a child of the `parent`?</summary>
        public static bool IsChildOf(IPlayableWrapper child, IPlayableWrapper parent)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Synchronizes the <see cref="AnimancerState.NormalizedTime"/>s of the <see cref="SynchronizedChildren"/> by
        /// modifying their internal playable speeds.
        /// </summary>
        protected void ApplySynchronizeChildren(ref bool needsMoreUpdates)
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// The multiplied <see cref="PlayableExtensions.GetSpeed"/> of this mixer and its parents down the
        /// hierarchy to determine the actual speed its output is being played at.
        /// </summary>
        /// <remarks>
        /// This can be different from the <see cref="AnimancerNode.EffectiveSpeed"/> because the
        /// <see cref="SynchronizedChildren"/> have their playable speed modified without setting their
        /// <see cref="AnimancerNode.Speed"/>.
        /// </remarks>
        public float CalculateRealEffectiveSpeed()
        {
            return default;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Inverse Kinematics
        /************************************************************************************************************************/

        private bool _ApplyAnimatorIK;

        /// <inheritdoc/>
        public override bool ApplyAnimatorIK
        {
            get => _ApplyAnimatorIK;
            set => base.ApplyAnimatorIK = _ApplyAnimatorIK = value;
        }

        /************************************************************************************************************************/

        private bool _ApplyFootIK;

        /// <inheritdoc/>
        public override bool ApplyFootIK
        {
            get => _ApplyFootIK;
            set => base.ApplyFootIK = _ApplyFootIK = value;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Other Methods
        /************************************************************************************************************************/

        /// <summary>Calculates the sum of the <see cref="AnimancerNode.Weight"/> of all `states`.</summary>
        public static float CalculateTotalWeight(AnimancerState[] states, int count)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Sets <see cref="AnimancerState.Time"/> for all <see cref="ChildStates"/>.
        /// </summary>
        public void SetChildrenTime(float value, bool normalized = false)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Sets the weight of all states after the `previousIndex` to 0.</summary>
        protected void DisableRemainingStates(int previousIndex)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Divides the weight of all child states by the `totalWeight`.</summary>
        /// <remarks>
        /// If the `totalWeight` is equal to the total <see cref="AnimancerNode.Weight"/> of all child states, then the
        /// new total will become 1.
        /// </remarks>
        public void NormalizeWeights(float totalWeight)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Gets a user-friendly key to identify the `state` in the Inspector.</summary>
        public virtual string GetDisplayKey(AnimancerState state)
            => $"[{state.Index}]";

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override Vector3 AverageVelocity
        {
            get
            {
                var velocity = default(Vector3);

                RecalculateWeights();

                for (int i = _ChildCount - 1; i >= 0; i--)
                {
                    var state = ChildStates[i];
                    velocity += state.AverageVelocity * state.Weight;
                }

                return velocity;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Recalculates the <see cref="AnimancerState.Duration"/> of all child states so that they add up to 1.
        /// </summary>
        /// <exception cref="NullReferenceException">There are any states with no <see cref="Clip"/>.</exception>
        public void NormalizeDurations()
        {
        }

        /************************************************************************************************************************/

#if UNITY_ASSERTIONS
        /// <summary>Has the <see cref="AnimancerNode.DebugName"/> been generated from the child states?</summary>
        private bool _IsGeneratedName;
#endif

        /// <summary>
        /// Returns a string describing the type of this mixer and the name of <see cref="Clip"/>s connected to it.
        /// </summary>
        public override string ToString()
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void AppendDetails(StringBuilder text, string separator)
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void GatherAnimationClips(ICollection<AnimationClip> clips)
            => clips.GatherFromSource(ChildStates);

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

