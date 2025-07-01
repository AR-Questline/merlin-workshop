// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace Animancer
{
    /// <summary>Base class for <see cref="Playable"/> wrapper objects in an <see cref="AnimancerPlayable"/>.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer/AnimancerNode
    /// 
    public abstract class AnimancerNode : Key,
        IUpdatable,
        IEnumerable<AnimancerState>,
        IEnumerator,
        IPlayableWrapper,
        ICopyable<AnimancerNode>
    {
        /************************************************************************************************************************/
        #region Playable
        /************************************************************************************************************************/

        /// <summary>
        /// The internal object this node manages in the <see cref="PlayableGraph"/>.
        /// <para></para>
        /// Must be set by <see cref="CreatePlayable()"/>. Failure to do so will throw the following exception
        /// throughout the system when using this node: "<see cref="ArgumentException"/>: The playable passed as an
        /// argument is invalid. To create a valid playable, please use the appropriate Create method".
        /// </summary>
        protected internal Playable _Playable;

        /// <summary>The internal <see cref="UnityEngine.Playables.Playable"/> managed by this node.</summary>
        public Playable Playable => _Playable;

        /// <summary>Is the <see cref="Playable"/> usable (properly initialized and not destroyed)?</summary>
        public bool IsValid => _Playable.IsValid();

        /************************************************************************************************************************/

#if UNITY_EDITOR
        /// <summary>[Editor-Only, Internal] Indicates whether the Inspector details for this node are expanded.</summary>
        internal bool _IsInspectorExpanded;
#endif

        /************************************************************************************************************************/

        /// <summary>Creates and assigns the <see cref="Playable"/> managed by this node.</summary>
        /// <remarks>This method also applies the <see cref="Speed"/> if it was set beforehand.</remarks>
        public virtual void CreatePlayable()
        {
        }

        /// <summary>Creates and assigns the <see cref="Playable"/> managed by this node.</summary>
        protected abstract void CreatePlayable(out Playable playable);

        /************************************************************************************************************************/

        /// <summary>Destroys the <see cref="Playable"/>.</summary>
        public void DestroyPlayable()
        {
        }

        /************************************************************************************************************************/

        /// <summary>Calls <see cref="DestroyPlayable"/> and <see cref="CreatePlayable()"/>.</summary>
        public virtual void RecreatePlayable()
        {
        }

        /// <summary>Calls <see cref="RecreatePlayable"/> on this node and all its children recursively.</summary>
        public void RecreatePlayableRecursive()
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        void ICopyable<AnimancerNode>.CopyFrom(AnimancerNode copyFrom)
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Graph
        /************************************************************************************************************************/

        private AnimancerPlayable _Root;

        /// <summary>The <see cref="AnimancerPlayable"/> at the root of the graph.</summary>
        public AnimancerPlayable Root
        {
            get => _Root;
            internal set
            {
                _Root = value;

#if UNITY_ASSERTIONS
                GC.SuppressFinalize(this);
#endif
            }
        }

        /************************************************************************************************************************/

        /// <summary>The root <see cref="AnimancerLayer"/> which this node is connected to.</summary>
        public abstract AnimancerLayer Layer { get; }

        /// <summary>The object which receives the output of this node.</summary>
        public abstract IPlayableWrapper Parent { get; }

        /************************************************************************************************************************/

        /// <summary>The index of the port this node is connected to on the parent's <see cref="Playable"/>.</summary>
        /// <remarks>
        /// A negative value indicates that it is not assigned to a port.
        /// <para></para>
        /// Indices are generally assigned starting from 0, ascending in the order they are connected to their layer.
        /// They will not usually change unless the <see cref="Parent"/> changes or another state on the same layer is
        /// destroyed so the last state is swapped into its place to avoid shuffling everything down to cover the gap.
        /// <para></para>
        /// The setter is internal so user defined states cannot set it incorrectly. Ideally,
        /// <see cref="AnimancerLayer"/> should be able to set the port in its constructor and
        /// <see cref="AnimancerState.SetParent"/> should also be able to set it, but classes that further inherit from
        /// there should not be able to change it without properly calling that method.
        /// </remarks>
        public int Index { get; internal set; } = int.MinValue;

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="AnimancerNode"/>.</summary>
        protected AnimancerNode()
        {
        }

        /************************************************************************************************************************/
#if UNITY_ASSERTIONS
        /************************************************************************************************************************/

        /// <summary>[Assert-Only]
        /// Should a <see cref="System.Diagnostics.StackTrace"/> be captured in the constructor of all new nodes so
        /// <see cref="OptionalWarning.UnusedNode"/> can include it in the warning if that node ends up being unused?
        /// </summary>
        /// <remarks>This has a notable performance cost so it should only be used when trying to identify a problem.</remarks>
        public static bool TraceConstructor { get; set; }

        /************************************************************************************************************************/

        /// <summary>[Assert-Only]
        /// The stack trace of the constructor (or null if <see cref="TraceConstructor"/> was false).
        /// </summary>
        private System.Diagnostics.StackTrace _ConstructorStackTrace;

        /// <summary>[Assert-Only]
        /// Returns the stack trace of the constructor (or null if <see cref="TraceConstructor"/> was false).
        /// </summary>
        public static System.Diagnostics.StackTrace GetConstructorStackTrace(AnimancerNode node)
            => node._ConstructorStackTrace;

        /************************************************************************************************************************/

        /// <summary>[Assert-Only] Checks <see cref="OptionalWarning.UnusedNode"/>.</summary>
        ~AnimancerNode()
        {
        }

        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/

        /// <summary>[Internal] Connects the <see cref="Playable"/> to the <see cref="Parent"/>.</summary>
        internal void ConnectToGraph()
        {
        }

        /// <summary>[Internal] Disconnects the <see cref="Playable"/> from the <see cref="Parent"/>.</summary>
        internal void DisconnectFromGraph()
        {
        }

        /************************************************************************************************************************/

        private void ApplyConnectedState(IPlayableWrapper parent)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Calls <see cref="AnimancerPlayable.RequirePreUpdate"/> if the <see cref="Root"/> is not null.</summary>
        protected void RequireUpdate()
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        void IUpdatable.Update()
        {
        }

        /// <summary>
        /// Updates the <see cref="Weight"/> for fading, applies it to this state's port on the parent mixer, and plays
        /// or pauses the <see cref="Playable"/> if its state is dirty.
        /// <para></para>
        /// If the <see cref="Parent"/>'s <see cref="KeepChildrenConnected"/> is set to false, this method will
        /// also connect/disconnect this node from the <see cref="Parent"/> in the playable graph.
        /// </summary>
        protected internal virtual void Update(out bool needsMoreUpdates)
        {
            needsMoreUpdates = default(bool);
        }

        /************************************************************************************************************************/
        // IEnumerator for yielding in a coroutine to wait until animations have stopped.
        /************************************************************************************************************************/

        /// <summary>Is this node playing and not yet at its end?</summary>
        /// <remarks>
        /// This method is called by <see cref="IEnumerator.MoveNext"/> so this object can be used as a custom yield
        /// instruction to wait until it finishes.
        /// </remarks>
        public abstract bool IsPlayingAndNotEnding();

        bool IEnumerator.MoveNext() => IsPlayingAndNotEnding();

        object IEnumerator.Current => null;

        void IEnumerator.Reset()
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Children
        /************************************************************************************************************************/

        /// <summary>[<see cref="IPlayableWrapper"/>]
        /// The number of states using this node as their <see cref="AnimancerState.Parent"/>.
        /// </summary>
        public virtual int ChildCount => 0;

        /// <summary>Returns the state connected to the specified `index` as a child of this node.</summary>
        /// <exception cref="NotSupportedException">This node can't have children.</exception>
        AnimancerNode IPlayableWrapper.GetChild(int index) => GetChild(index);

        /// <summary>[<see cref="IPlayableWrapper"/>]
        /// Returns the state connected to the specified `index` as a child of this node.
        /// </summary>
        /// <exception cref="NotSupportedException">This node can't have children.</exception>
        public virtual AnimancerState GetChild(int index)
            => throw new NotSupportedException(this + " can't have children.");

        /// <summary>Called when a child is connected with this node as its <see cref="AnimancerState.Parent"/>.</summary>
        /// <exception cref="NotSupportedException">This node can't have children.</exception>
        protected internal virtual void OnAddChild(AnimancerState state)
        {
        }

        /// <summary>Called when a child's <see cref="AnimancerState.Parent"/> is changed from this node.</summary>
        /// <exception cref="NotSupportedException">This node can't have children.</exception>
        protected internal virtual void OnRemoveChild(AnimancerState state)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Connects the `state` to this node at its <see cref="Index"/>.</summary>
        /// <exception cref="InvalidOperationException">The <see cref="Index"/> was already occupied.</exception>
        protected void OnAddChild(IList<AnimancerState> states, AnimancerState state)
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Indicates whether child playables should stay connected to this mixer at all times (default false).
        /// </summary>
        public virtual bool KeepChildrenConnected => false;

        /// <summary>
        /// Ensures that all children of this node are connected to the <see cref="_Playable"/>.
        /// </summary>
        internal void ConnectAllChildrenToGraph()
        {
        }

        /// <summary>
        /// Ensures that all children of this node which have zero weight are disconnected from the
        /// <see cref="_Playable"/>.
        /// </summary>
        internal void DisconnectWeightlessChildrenFromGraph()
        {
        }

        /************************************************************************************************************************/
        // IEnumerable for 'foreach' statements.
        /************************************************************************************************************************/

        /// <summary>Gets an enumerator for all of this node's child states.</summary>
        public virtual FastEnumerator<AnimancerState> GetEnumerator() => default;

        IEnumerator<AnimancerState> IEnumerable<AnimancerState>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Weight
        /************************************************************************************************************************/

        /// <summary>The current blend weight of this node. Accessed via <see cref="Weight"/>.</summary>
        private float _Weight;

        /************************************************************************************************************************/

        /// <summary>The current blend weight of this node which determines how much it affects the final output.</summary>
        /// <remarks>
        /// 0 has no effect while 1 applies the full effect and values inbetween apply a proportional effect.
        /// <para></para>
        /// Setting this property cancels any fade currently in progress. If you don't wish to do that, you can use
        /// <see cref="SetWeight"/> instead.
        /// <para></para>
        /// <em>Animancer Lite only allows this value to be set to 0 or 1 in runtime builds.</em>
        /// </remarks>
        ///
        /// <example>
        /// Calling <see cref="AnimancerPlayable.Play(AnimationClip)"/> immediately sets the weight of all states to 0
        /// and the new state to 1. Note that this is separate from other values like
        /// <see cref="AnimancerState.IsPlaying"/> so a state can be paused at any point and still show its pose on the
        /// character or it could be still playing at 0 weight if you want it to still trigger events (though states
        /// are normally stopped when they reach 0 weight so you would need to explicitly set it to playing again).
        /// <para></para>
        /// Calling <see cref="AnimancerPlayable.Play(AnimationClip, float, FadeMode)"/> does not immediately change
        /// the weights, but instead calls <see cref="StartFade(float, float)"/> on every state to set their
        /// <see cref="TargetWeight"/> and <see cref="FadeSpeed"/>. Then every update each state's weight will move
        /// towards that target value at that speed.
        /// </example>
        public float Weight
        {
            get => _Weight;
            set
            {
                SetWeight(value);
                TargetWeight = value;
                FadeSpeed = 0;
            }
        }

        /// <summary>
        /// Sets the current blend weight of this node which determines how much it affects the final output.
        /// 0 has no effect while 1 applies the full effect of this node.
        /// <para></para>
        /// This method allows any fade currently in progress to continue. If you don't wish to do that, you can set
        /// the <see cref="Weight"/> property instead.
        /// <para></para>
        /// <em>Animancer Lite only allows this value to be set to 0 or 1 in runtime builds.</em>
        /// </summary>
        public void SetWeight(float value)
        {
        }

        /// <summary>Flags this node as having a changed <see cref="Weight"/> that needs to be applied next update.</summary>
        protected internal void SetWeightDirty()
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Applies the <see cref="Weight"/> to the connection between this node and its <see cref="Parent"/>.
        /// </summary>
        public void ApplyWeight()
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// The <see cref="Weight"/> of this state multiplied by the <see cref="Weight"/> of each of its parents down
        /// the hierarchy to determine how much this state affects the final output.
        /// </summary>
        public float EffectiveWeight
        {
            get
            {
                var weight = Weight;

                var parent = Parent;
                while (parent != null)
                {
                    weight *= parent.Weight;
                    parent = parent.Parent;
                }

                return weight;
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Fading
        /************************************************************************************************************************/

        /// <summary>
        /// The desired <see cref="Weight"/> which this node is fading towards according to the
        /// <see cref="FadeSpeed"/>.
        /// </summary>
        public float TargetWeight { get; set; }

        /// <summary>The speed at which this node is fading towards the <see cref="TargetWeight"/>.</summary>
        /// <remarks>This value isn't affected by this node's <see cref="Speed"/>, but is affected by its parents.</remarks>
        public float FadeSpeed { get; set; }

        /************************************************************************************************************************/

        /// <summary>
        /// Calls <see cref="OnStartFade"/> and starts fading the <see cref="Weight"/> over the course
        /// of the <see cref="AnimancerPlayable.DefaultFadeDuration"/> (in seconds).
        /// </summary>
        /// <remarks>
        /// If the `targetWeight` is 0 then <see cref="Stop"/> will be called when the fade is complete.
        /// <para></para>
        /// If the <see cref="Weight"/> is already equal to the `targetWeight` then the fade will end
        /// immediately.
        /// </remarks>
        public void StartFade(float targetWeight)
            => StartFade(targetWeight, AnimancerPlayable.DefaultFadeDuration);

        /// <summary>
        /// Calls <see cref="OnStartFade"/> and starts fading the <see cref="Weight"/> over the course
        /// of the `fadeDuration` (in seconds).
        /// </summary>
        /// <remarks>
        /// If the `targetWeight` is 0 then <see cref="Stop"/> will be called when the fade is complete.
        /// <para></para>
        /// If the <see cref="Weight"/> is already equal to the `targetWeight` then the fade will end
        /// immediately.
        /// <para></para>
        /// <em>Animancer Lite only allows a `targetWeight` of 0 or 1 and the default `fadeDuration` (0.25 seconds) in
        /// runtime builds.</em>
        /// </remarks>
        public void StartFade(float targetWeight, float fadeDuration)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Called by <see cref="StartFade(float, float)"/>.</summary>
        protected internal abstract void OnStartFade();

        /************************************************************************************************************************/

        /// <summary>
        /// Stops the animation and makes it inactive immediately so it no longer affects the output.
        /// Sets <see cref="Weight"/> = 0 by default.
        /// </summary>
        public virtual void Stop()
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Moves the <see cref="Weight"/> towards the <see cref="TargetWeight"/> according to the
        /// <see cref="FadeSpeed"/>.
        /// </summary>
        private void UpdateFade(out bool needsMoreUpdates)
        {
            needsMoreUpdates = default(bool);
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Speed
        /************************************************************************************************************************/

        private float _Speed = 1;

        /// <summary>Indicates whether the weight has changed and should be applied to the parent mixer.</summary>
        private bool _IsWeightDirty = true;

        /// <summary>[Pro-Only] How fast the <see cref="AnimancerState.Time"/> is advancing every frame (default 1).</summary>
        /// 
        /// <remarks>
        /// A negative value will play the animation backwards.
        /// <para></para>
        /// To pause an animation, consider setting <see cref="AnimancerState.IsPlaying"/> to false instead of setting
        /// this value to 0.
        /// <para></para>
        /// <em>Animancer Lite does not allow this value to be changed in runtime builds.</em>
        /// </remarks>
        ///
        /// <example><code>
        /// void PlayAnimation(AnimancerComponent animancer, AnimationClip clip)
        /// {
        ///     var state = animancer.Play(clip);
        ///
        ///     state.Speed = 1;// Normal speed.
        ///     state.Speed = 2;// Double speed.
        ///     state.Speed = 0.5f;// Half speed.
        ///     state.Speed = -1;// Normal speed playing backwards.
        /// }
        /// </code></example>
        /// 
        /// <exception cref="ArgumentOutOfRangeException">The value is not finite.</exception>
        public float Speed
        {
            get => _Speed;
            set
            {
                if (_Speed == value)
                    return;

#if UNITY_ASSERTIONS
                if (!value.IsFinite())
                    throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(Speed)} {Strings.MustBeFinite}");

                OptionalWarning.UnsupportedSpeed.Log(UnsupportedSpeedMessage, Root?.Component);
#endif
                _Speed = value;

                if (_Playable.IsValid())
                    _Playable.SetSpeed(value);
            }
        }

#if UNITY_ASSERTIONS
        /// <summary>[Assert-Only]
        /// Returns null if the <see cref="Speed"/> property will work properly on this type of node, or a message
        /// explaining why it won't work.
        /// </summary>
        protected virtual string UnsupportedSpeedMessage => null;
#endif

        /************************************************************************************************************************/

        /// <summary>
        /// The multiplied <see cref="Speed"/> of each of this node's parents down the hierarchy, excluding the root
        /// <see cref="AnimancerPlayable.Speed"/>.
        /// </summary>
        private float ParentEffectiveSpeed
        {
            get
            {
                var parent = Parent;
                if (parent == null)
                    return 1;

                var speed = parent.Speed;

                while ((parent = parent.Parent) != null)
                {
                    speed *= parent.Speed;
                }

                return speed;
            }
        }

        /// <summary>
        /// The <see cref="Speed"/> of this node multiplied by the <see cref="Speed"/> of each of its parents to
        /// determine the actual speed it's playing at.
        /// </summary>
        public float EffectiveSpeed
        {
            get => Speed * ParentEffectiveSpeed;
            set => Speed = value / ParentEffectiveSpeed;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Inverse Kinematics
        /************************************************************************************************************************/

        /// <summary>
        /// Should setting the <see cref="Parent"/> also set this node's <see cref="ApplyAnimatorIK"/> to match it?
        /// Default is true.
        /// </summary>
        public static bool ApplyParentAnimatorIK { get; set; } = true;

        /// <summary>
        /// Should setting the <see cref="Parent"/> also set this node's <see cref="ApplyFootIK"/> to match it?
        /// Default is true.
        /// </summary>
        public static bool ApplyParentFootIK { get; set; } = true;

        /************************************************************************************************************************/

        /// <summary>
        /// Copies the IK settings from the <see cref="Parent"/>:
        /// <list type="bullet">
        /// <item>If <see cref="ApplyParentAnimatorIK"/> is true, copy <see cref="ApplyAnimatorIK"/>.</item>
        /// <item>If <see cref="ApplyParentFootIK"/> is true, copy <see cref="ApplyFootIK"/>.</item>
        /// </list>
        /// </summary>
        public virtual void CopyIKFlags(AnimancerNode copyFrom)
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public virtual bool ApplyAnimatorIK
        {
            get
            {
                for (int i = ChildCount - 1; i >= 0; i--)
                {
                    var state = GetChild(i);
                    if (state == null)
                        continue;

                    if (state.ApplyAnimatorIK)
                        return true;
                }

                return false;
            }
            set
            {
                for (int i = ChildCount - 1; i >= 0; i--)
                {
                    var state = GetChild(i);
                    if (state == null)
                        continue;

                    state.ApplyAnimatorIK = value;
                }
            }
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public virtual bool ApplyFootIK
        {
            get
            {
                for (int i = ChildCount - 1; i >= 0; i--)
                {
                    var state = GetChild(i);
                    if (state == null)
                        continue;

                    if (state.ApplyFootIK)
                        return true;
                }

                return false;
            }
            set
            {
                for (int i = ChildCount - 1; i >= 0; i--)
                {
                    var state = GetChild(i);
                    if (state == null)
                        continue;

                    state.ApplyFootIK = value;
                }
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Descriptions
        /************************************************************************************************************************/

#if UNITY_ASSERTIONS
        /// <summary>[Assert-Only] The Inspector display name of this node.</summary>
        /// <remarks>Set using <see cref="SetDebugName"/>.</remarks>
        public string DebugName { get; private set; }
#endif

        /// <summary>The Inspector display name of this node.</summary>
        public override string ToString()
        {
            return default;
        }

        /// <summary>[Assert-Conditional]
        /// Sets the Inspector display name of this node. <see cref="ToString"/> returns the name.
        /// </summary>
        [System.Diagnostics.Conditional(Strings.Assertions)]
        public void SetDebugName(string name)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Returns a detailed descrption of the current details of this node.</summary>
        public string GetDescription(string separator = "\n")
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Appends a detailed descrption of the current details of this node.</summary>
        public void AppendDescription(StringBuilder text, string separator = "\n")
        {
        }

        /************************************************************************************************************************/

        /// <summary>Called by <see cref="AppendDescription"/> to append the details of this node.</summary>
        protected virtual void AppendDetails(StringBuilder text, string separator)
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Appends the details of <see cref="IPlayableWrapper.ApplyAnimatorIK"/> and
        /// <see cref="IPlayableWrapper.ApplyFootIK"/>.
        /// </summary>
        public static void AppendIKDetails(StringBuilder text, string separator, IPlayableWrapper node)
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

