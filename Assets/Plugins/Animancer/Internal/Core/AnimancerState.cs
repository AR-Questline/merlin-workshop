// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
using Animancer.Editor;
#endif

namespace Animancer
{
    /// <summary>
    /// Base class for all states in an <see cref="AnimancerPlayable"/> graph which manages one or more
    /// <see cref="Playable"/>s.
    /// </summary>
    /// 
    /// <remarks>
    /// This class can be used as a custom yield instruction to wait until the animation either stops playing or
    /// reaches its end.
    /// <para></para>
    /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/playing/states">States</see>
    /// </remarks>
    /// https://kybernetik.com.au/animancer/api/Animancer/AnimancerState
    /// 
    public abstract partial class AnimancerState : AnimancerNode,
        IAnimationClipCollection,
        ICopyable<AnimancerState>
    {
        /************************************************************************************************************************/
        #region Graph
        /************************************************************************************************************************/

        /// <summary>The <see cref="AnimancerPlayable"/> at the root of the graph.</summary>
        /// <exception cref="InvalidOperationException">
        /// The <see cref="Parent"/> has a different <see cref="AnimancerNode.Root"/>.
        /// Setting the <see cref="Parent"/>'s <see cref="AnimancerNode.Root"/> will apply to its children recursively
        /// because they must always match.
        /// </exception>
        public void SetRoot(AnimancerPlayable root)
        {
        }

        /************************************************************************************************************************/

        private AnimancerNode _Parent;

        /// <summary>The object which receives the output of the <see cref="Playable"/>.</summary>
        public sealed override IPlayableWrapper Parent => _Parent;

        /// <summary>Connects this state to the `parent` state at the specified `index`.</summary>
        /// <remarks>
        /// If the `parent` is null, this state will be disconnected from everything.
        /// <para></para>
        /// Use <see cref="AnimancerLayer.AddChild(AnimancerState)"/> instead of this method to connect to a layer.
        /// </remarks>
        public void SetParent(AnimancerNode parent, int index)
        {
        }

        /// <summary>[Internal] Directly sets the <see cref="Parent"/> without triggering any other connection methods.</summary>
        internal void SetParentInternal(AnimancerNode parent, int index = -1)
        {
        }

        /************************************************************************************************************************/
        // Layer.
        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override AnimancerLayer Layer => _Parent?.Layer;

        /// <summary>
        /// The index of the <see cref="AnimancerLayer"/> this state is connected to (determined by the
        /// <see cref="Parent"/>). Returns <c>-1</c> if this state is not connected to a layer.
        /// </summary>
        public int LayerIndex
        {
            get
            {
                if (_Parent == null)
                    return -1;

                var layer = _Parent.Layer;
                if (layer == null)
                    return -1;

                return layer.Index;
            }
            set
            {
                Root.Layers[value].AddChild(this);
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Key and Clip
        /************************************************************************************************************************/

        internal object _Key;

        /// <summary>
        /// The object used to identify this state in the root <see cref="AnimancerPlayable.States"/> dictionary.
        /// Can be null.
        /// </summary>
        public object Key
        {
            get => _Key;
            set
            {
                if (Root == null)
                {
                    _Key = value;
                }
                else
                {
                    Root.States.Unregister(this);
                    _Key = value;
                    Root.States.Register(this);
                }
            }
        }

        /************************************************************************************************************************/

        /// <summary>The <see cref="AnimationClip"/> which this state plays (if any).</summary>
        /// <exception cref="NotSupportedException">This state type doesn't have a clip and you try to set it.</exception>
        public virtual AnimationClip Clip
        {
            get => null;
            set => throw new NotSupportedException($"{GetType()} does not support setting the {nameof(Clip)}.");
        }

        /// <summary>The main object to show in the Inspector for this state (if any).</summary>
        /// <exception cref="NotSupportedException">This state type doesn't have a main object and you try to set it.</exception>
        /// <exception cref="InvalidCastException">This state can't use the assigned value.</exception>
        public virtual Object MainObject
        {
            get => null;
            set => throw new NotSupportedException($"{GetType()} does not support setting the {nameof(MainObject)}.");
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Sets the `currentObject` and calls <see cref="AnimancerNode.RecreatePlayable"/>. If the `currentObject` was
        /// being used as the <see cref="Key"/> then it is changed as well.
        /// </summary>
        /// <exception cref="ArgumentNullException">The `newObject` is null.</exception>
        protected void ChangeMainObject<T>(ref T currentObject, T newObject) where T : Object
        {
        }

        /************************************************************************************************************************/

        /// <summary>The average velocity of the root motion caused by this state.</summary>
        public virtual Vector3 AverageVelocity => default;

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Playing
        /************************************************************************************************************************/

        /// <summary>Is the <see cref="Time"/> automatically advancing?</summary>
        private bool _IsPlaying;

        /// <summary>Has <see cref="_IsPlaying"/> changed since it was last applied to the <see cref="Playable"/>.</summary>
        /// <remarks>
        /// Playables start playing by default so we start dirty to pause it during the first update (unless
        /// <see cref="IsPlaying"/> is set to true before that).
        /// </remarks>
        private bool _IsPlayingDirty = true;

        /************************************************************************************************************************/

        /// <summary>Is the <see cref="Time"/> automatically advancing?</summary>
        ///
        /// <example><code>
        /// void IsPlayingExample(AnimancerComponent animancer, AnimationClip clip)
        /// {
        ///     var state = animancer.States.GetOrCreate(clip);
        ///
        ///     if (state.IsPlaying)
        ///         Debug.Log(clip + " is playing");
        ///     else
        ///         Debug.Log(clip + " is paused");
        ///
        ///     state.IsPlaying = false;// Pause the animation.
        ///
        ///     state.IsPlaying = true;// Unpause the animation.
        /// }
        /// </code></example>
        public bool IsPlaying
        {
            get => _IsPlaying;
            set
            {
                if (_IsPlaying == value)
                    return;

                _IsPlaying = value;

                // If it was already dirty then we just returned to the previous state so it is no longer dirty.
                if (_IsPlayingDirty)
                {
                    _IsPlayingDirty = false;
                    // We may still need to be updated for other reasons (such as Weight),
                    // but if not then we will be removed from the update list next update.
                }
                else// Otherwise we are now dirty so we need to be updated.
                {
                    _IsPlayingDirty = true;
                    RequireUpdate();
                }

                OnSetIsPlaying();
            }
        }

        /// <summary>Called when the value of <see cref="IsPlaying"/> is changed.</summary>
        protected virtual void OnSetIsPlaying() {
        }

        /// <summary>Creates and assigns the <see cref="Playable"/> managed by this state.</summary>
        /// <remarks>This method also applies the <see cref="AnimancerNode.Speed"/> and <see cref="IsPlaying"/>.</remarks>
        public sealed override void CreatePlayable()
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns true if this state is playing and is at or fading towards a non-zero
        /// <see cref="AnimancerNode.Weight"/>.
        /// </summary>
        public bool IsActive => _IsPlaying && TargetWeight > 0;

        /// <summary>
        /// Returns true if this state isn't playing and is at 0 <see cref="AnimancerNode.Weight"/>.
        /// </summary>
        public bool IsStopped => !_IsPlaying && Weight == 0;

        /************************************************************************************************************************/

        /// <summary>
        /// Plays this state immediately, without any blending.
        /// <para></para>
        /// Unlike <see cref="AnimancerPlayable.Play(AnimancerState)"/>, this method only affects this state and won't
        /// stop any others that are playing.
        /// </summary>
        /// <remarks>
        /// Sets <see cref="IsPlaying"/> = true, <see cref="AnimancerNode.Weight"/> = 1, and clears the
        /// <see cref="Events"/> (unless <see cref="AutomaticallyClearEvents"/> is disabled).
        /// <para></para>
        /// Doesn't change the <see cref="Time"/> so it will continue from its current value.
        /// </remarks>
        public void Play()
        {
        }

        /************************************************************************************************************************/

        /// <summary>Stops the animation and makes it inactive immediately so it no longer affects the output.</summary>
        /// <remarks>
        /// Sets <see cref="AnimancerNode.Weight"/> = 0, <see cref="IsPlaying"/> = false, <see cref="Time"/> = 0, and
        /// clears the <see cref="Events"/> (unless <see cref="AutomaticallyClearEvents"/> is disabled).
        /// <para></para>
        /// To freeze the animation in place without ending it, you only need to set <see cref="IsPlaying"/> = false
        /// instead. Or to freeze all animations, you can call <see cref="AnimancerPlayable.PauseGraph"/>.
        /// </remarks>
        public override void Stop()
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Called by <see cref="AnimancerNode.StartFade(float, float)"/>.
        /// Clears the <see cref="Events"/> (unless <see cref="AutomaticallyClearEvents"/> is disabled).
        /// </summary>
        protected internal override void OnStartFade()
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Timing
        /************************************************************************************************************************/
        // Time.
        /************************************************************************************************************************/

        /// <summary>
        /// Indicates whether the <see cref="_Time"/> needs to be assigned to the <see cref="Playable"/> next update.
        /// </summary>
        /// <remarks>
        /// <see cref="EventDispatcher"/> executes after all other playables, at which point changes can still be made to
        /// their time but not their weight which means that if we set the time immediately then it can be out of sync
        /// with the weight. For example, if an animation ends and you play another, the first animation would be
        /// stopped and rewinded to the start but would still be at full weight so it would show its first frame before
        /// the new animation actually takes effect (even if the previous animation was not looping).
        /// <para></para>
        /// So instead, we simply delay setting the actual playable time until the next update so that time and weight
        /// are always in sync.
        /// </remarks>
        private bool _MustSetTime;

        /// <summary>
        /// The current time of the <see cref="Playable"/>, retrieved by <see cref="Time"/> whenever the
        /// <see cref="_TimeFrameID"/> is different from the <see cref="AnimancerPlayable.FrameID"/>.
        /// </summary>
        private double _Time;

        /// <summary>
        /// The <see cref="AnimancerPlayable.FrameID"/> from when the <see cref="Time"/> was last retrieved from the
        /// <see cref="Playable"/>.
        /// </summary>
        private ulong _TimeFrameID;

        /************************************************************************************************************************/

        /// <summary>The number of seconds that have passed since the start of this animation.</summary>
        ///
        /// <remarks>
        /// This value will continue increasing after the animation passes the end of its <see cref="Length"/> while
        /// the animated object either freezes in place or starts again from the beginning according to whether it is
        /// looping or not.
        /// <para></para>
        /// Events and root motion between the old and new time will be skipped when setting this value. Use
        /// <see cref="MoveTime(float, bool)"/> instead if you don't want that behaviour.
        /// <para></para>
        /// This property internally uses <see cref="RawTime"/> whenever the value is out of date or gets changed.
        /// <para></para>
        /// <em>Animancer Lite does not allow this value to be changed in runtime builds (except resetting it to 0).</em>
        /// </remarks>
        ///
        /// <example><code>
        /// void PlayAnimation(AnimancerComponent animancer, AnimationClip clip)
        /// {
        ///     var state = animancer.Play(clip);
        ///
        ///     // Skip 0.5 seconds into the animation:
        ///     state.Time = 0.5f;
        ///
        ///     // Skip 50% of the way through the animation (0.5 in a range of 0 to 1):
        ///     state.NormalizedTime = 0.5f;
        ///
        ///     // Skip to the end of the animation and play backwards.
        ///     state.NormalizedTime = 1;
        ///     state.Speed = -1;
        /// }
        /// </code></example>
        public float Time
        {
            get => (float)TimeD;
            set => TimeD = value;
        }

        /// <summary>The underlying <see cref="double"/> value of <see cref="Time"/>.</summary>
        public double TimeD
        {
            get
            {
                var root = Root;
                if (root == null || _MustSetTime)
                    return _Time;

                var frameID = root.FrameID;
                if (_TimeFrameID != frameID)
                {
                    _TimeFrameID = frameID;
                    _Time = RawTime;
                }

                return _Time;
            }
            set
            {
#if UNITY_ASSERTIONS
                if (!value.IsFinite())
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        $"{nameof(Time)} {Strings.MustBeFinite}");
#endif

                _Time = value;

                var root = Root;
                if (root == null)
                {
                    _MustSetTime = true;
                }
                else
                {
                    _TimeFrameID = root.FrameID;

                    // Don't allow the time to be changed during a post update because it would take effect this frame
                    // but Weight changes wouldn't so the Time and Weight would be out of sync. For example, if an
                    // event plays a state, the old state would be stopped back at Time 0 but its Weight would not yet
                    // be 0 so it would show its first frame before the new animation takes effect.

                    if (AnimancerPlayable.IsRunningPostUpdate(root))
                    {
                        _MustSetTime = true;
                        root.RequirePreUpdate(this);
                    }
                    else
                    {
                        RawTime = value;
                    }
                }

                if (_updateEventsOnTimeSet) {
                    _EventDispatcher?.OnTimeChangedWithEventCheck();
                } else {
                    _EventDispatcher?.OnTimeChanged();
                }
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// The internal implementation of <see cref="Time"/> which directly gets and sets the underlying value.
        /// </summary>
        /// <remarks>
        /// Setting this value actually calls <see cref="PlayableExtensions.SetTime"/> twice to ensure that animation
        /// events aren't triggered incorrectly. Calling it only once would trigger any animation events between the
        /// previous time and the new time. So if an animation plays to the end and you set the time back to 0 (such as
        /// by calling <see cref="Stop"/> or playing a different animation), the next time that animation played it
        /// would immediately trigger all of its events, then play through and trigger them normally as well.
        /// </remarks>
        public virtual double RawTime
        {
            get
            {
                Validate.AssertPlayable(this);
                return _Playable.GetTime();
            }
            set
            {
                Validate.AssertPlayable(this);
                var time = value;
                _Playable.SetTime(time);
                _Playable.SetTime(time);
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// The <see cref="Time"/> of this state as a portion of the animation's <see cref="Length"/>, meaning the
        /// value goes from 0 to 1 as it plays from start to end, regardless of how long that actually takes.
        /// </summary>
        /// 
        /// <remarks>
        /// This value will continue increasing after the animation passes the end of its <see cref="Length"/> while
        /// the animated object either freezes in place or starts again from the beginning according to whether it is
        /// looping or not.
        /// <para></para>
        /// The fractional part of the value (<c>NormalizedTime % 1</c>) is the percentage (0-1) of progress in the
        /// current loop while the integer part (<c>(int)NormalizedTime</c>) is the number of times the animation has
        /// been looped.
        /// <para></para>
        /// Events and root motion between the old and new time will be skipped when setting this value. Use
        /// <see cref="MoveTime(float, bool)"/> instead if you don't want that behaviour.
        /// <para></para>
        /// <em>Animancer Lite does not allow this value to be changed in runtime builds (except resetting it to 0).</em>
        /// </remarks>
        ///
        /// <example><code>
        /// void PlayAnimation(AnimancerComponent animancer, AnimationClip clip)
        /// {
        ///     var state = animancer.Play(clip);
        ///
        ///     // Skip 0.5 seconds into the animation:
        ///     state.Time = 0.5f;
        ///
        ///     // Skip 50% of the way through the animation (0.5 in a range of 0 to 1):
        ///     state.NormalizedTime = 0.5f;
        ///
        ///     // Skip to the end of the animation and play backwards.
        ///     state.NormalizedTime = 1;
        ///     state.Speed = -1;
        /// }
        /// </code></example>
        public float NormalizedTime
        {
            get => (float)NormalizedTimeD;
            set => NormalizedTimeD = value;
        }

        /// <summary>The underlying <see cref="double"/> value of <see cref="NormalizedTime"/>.</summary>
        public double NormalizedTimeD
        {
            get
            {
                var length = Length;
                if (length != 0)
                    return TimeD / Length;
                else
                    return 0;
            }
            set => TimeD = value * Length;
        }

        bool _updateEventsOnTimeSet;
        public void SetNormalizedTimeWithEventsInvoke(float newNormalizedTime) {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Sets the <see cref="Time"/> or <see cref="NormalizedTime"/>, but unlike those properties this method
        /// applies any Root Motion and Animation Events (but not Animancer Events) between the old and new time.
        /// </summary>
        public void MoveTime(float time, bool normalized)
            => MoveTime((double)time, normalized);

        /// <summary>
        /// Sets the <see cref="Time"/> or <see cref="NormalizedTime"/>, but unlike those properties this method
        /// applies any Root Motion and Animation Events (but not Animancer Events) between the old and new time.
        /// </summary>
        public virtual void MoveTime(double time, bool normalized)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Prevents the <see cref="RawTime"/> from being applied.</summary>
        protected void CancelSetTime() => _MustSetTime = false;

        /************************************************************************************************************************/
        // Duration.
        /************************************************************************************************************************/

        /// <summary>[Pro-Only]
        /// The <see cref="NormalizedTime"/> after which the <see cref="AnimancerEvent.Sequence.OnEnd"/> callback will
        /// be invoked every frame.
        /// </summary>
        /// <remarks>
        /// This is a wrapper around <see cref="AnimancerEvent.Sequence.NormalizedEndTime"/> so that if the value has
        /// not been set (<see cref="float.NaN"/>) it can be determined based on the
        /// <see cref="AnimancerNode.EffectiveSpeed"/>: positive speed ends at 1 and negative speed ends at 0.
        /// <para></para>
        /// <em>Animancer Lite does not allow this value to be changed in runtime builds.</em>
        /// </remarks>
        public float NormalizedEndTime
        {
            get
            {
                if (_EventDispatcher != null)
                {
                    var time = _EventDispatcher.Events.NormalizedEndTime;
                    if (!float.IsNaN(time))
                        return time;
                }

                return AnimancerEvent.Sequence.GetDefaultNormalizedEndTime(EffectiveSpeed);
            }
            set => Events.NormalizedEndTime = value;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// The number of seconds the animation will take to play fully at its current
        /// <see cref="AnimancerNode.EffectiveSpeed"/>.
        /// </summary>
        /// 
        /// <remarks>
        /// For the time remaining from now until it reaches the end, use <see cref="RemainingDuration"/> instead.
        /// <para></para>
        /// Setting this value modifies the <see cref="AnimancerNode.EffectiveSpeed"/>, not the <see cref="Length"/>.
        /// <para></para>
        /// <em>Animancer Lite does not allow this value to be changed in runtime builds.</em>
        /// </remarks>
        ///
        /// <example><code>
        /// void PlayAnimation(AnimancerComponent animancer, AnimationClip clip)
        /// {
        ///     var state = animancer.Play(clip);
        ///
        ///     state.Duration = 1;// Play fully in 1 second.
        ///     state.Duration = 2;// Play fully in 2 seconds.
        ///     state.Duration = 0.5f;// Play fully in half a second.
        ///     state.Duration = -1;// Play backwards fully in 1 second.
        ///     state.NormalizedTime = 1; state.Duration = -1;// Play backwards from the end in 1 second.
        /// }
        /// </code></example>
        public float Duration
        {
            get
            {
                var speed = EffectiveSpeed;
                if (_EventDispatcher != null)
                {
                    var endTime = _EventDispatcher.Events.NormalizedEndTime;
                    if (!float.IsNaN(endTime))
                    {
                        if (speed > 0)
                            return Length * endTime / speed;
                        else
                            return Length * (1 - endTime) / -speed;
                    }
                }

                return Length / Math.Abs(speed);
            }
            set
            {
                var length = Length;
                if (_EventDispatcher != null)
                {
                    var endTime = _EventDispatcher.Events.NormalizedEndTime;
                    if (!float.IsNaN(endTime))
                    {
                        if (EffectiveSpeed > 0)
                            length *= endTime;
                        else
                            length *= 1 - endTime;
                    }
                }

                EffectiveSpeed = length / value;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// The number of seconds this state will take to go from its current <see cref="NormalizedTime"/> to the
        /// <see cref="NormalizedEndTime"/> at its current <see cref="AnimancerNode.EffectiveSpeed"/>.
        /// </summary>
        /// 
        /// <remarks>
        /// For the time it would take to play fully from the start, use the <see cref="Duration"/> instead.
        /// <para></para>
        /// Setting this value modifies the <see cref="AnimancerNode.EffectiveSpeed"/>, not the <see cref="Length"/>.
        /// <para></para>
        /// <em>Animancer Lite does not allow this value to be changed in runtime builds.</em>
        /// </remarks>
        ///
        /// <example><code>
        /// void PlayAnimation(AnimancerComponent animancer, AnimationClip clip)
        /// {
        ///     var state = animancer.Play(clip);
        ///
        ///     state.RemainingDuration = 1;// Play from the current time to the end in 1 second.
        ///     state.RemainingDuration = 2;// Play from the current time to the end in 2 seconds.
        ///     state.RemainingDuration = 0.5f;// Play from the current time to the end in half a second.
        ///     state.RemainingDuration = -1;// Play from the current time away from the end.
        /// }
        /// </code></example>
        public float RemainingDuration
        {
            get => (Length * NormalizedEndTime - Time) / EffectiveSpeed;
            set => EffectiveSpeed = (Length * NormalizedEndTime - Time) / value;
        }

        /************************************************************************************************************************/
        // Length.
        /************************************************************************************************************************/

        /// <summary>The total time this state would take to play in seconds when <see cref="AnimancerNode.Speed"/> = 1.</summary>
        public abstract float Length { get; }

        /// <summary>Will this state loop back to the start when it reaches the end?</summary>
        public virtual bool IsLooping => false;

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Methods
        /************************************************************************************************************************/

        /// <summary>
        /// Updates the <see cref="AnimancerNode.Weight"/> for fading, applies it to this state's port on the parent
        /// mixer, and plays or pauses the <see cref="Playable"/> if its state is dirty.
        /// </summary>
        /// <remarks>
        /// If the <see cref="Parent"/>'s <see cref="AnimancerNode.KeepChildrenConnected"/> is set to false, this
        /// method will also connect/disconnect this node from the <see cref="Parent"/> in the playable graph.
        /// </remarks>
        protected internal override void Update(out bool needsMoreUpdates)
        {
            needsMoreUpdates = default(bool);
        }

        /************************************************************************************************************************/

        /// <summary>Destroys the <see cref="Playable"/> and cleans up this state.</summary>
        /// <remarks>
        /// This method is NOT called automatically, so when implementing a custom state type you must use
        /// <see cref="AnimancerPlayable.Disposables"/> if you need to guarantee that things will get cleaned up.
        /// </remarks>
        public virtual void Destroy()
        {
        }

        /************************************************************************************************************************/

        /// <summary>Creates a copy of this state with the same <see cref="AnimancerNode.Root"/>.</summary>
        public AnimancerState Clone()
            => Clone(Root);

        /// <summary>Creates a copy of this state with the specified <see cref="AnimancerNode.Root"/>.</summary>
        public abstract AnimancerState Clone(AnimancerPlayable root);

        /// <summary>Sets the <see cref="AnimancerNode.Root"/>.</summary>
        /// <remarks>
        /// This method skips several steps of <see cref="SetRoot"/> and is intended to only be called on states
        /// immediately after their creation.
        /// </remarks>
        protected void SetNewCloneRoot(AnimancerPlayable root)
        {
        }

        /// <inheritdoc/>
        void ICopyable<AnimancerState>.CopyFrom(AnimancerState copyFrom)
        {
        }

        /************************************************************************************************************************/

        /// <summary>[<see cref="IAnimationClipCollection"/>] Gathers all the animations in this state.</summary>
        public virtual void GatherAnimationClips(ICollection<AnimationClip> clips)
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns true if the animation is playing and has not yet passed the
        /// <see cref="AnimancerEvent.Sequence.EndEvent"/>.
        /// </summary>
        /// <remarks>
        /// This method is called by <see cref="IEnumerator.MoveNext"/> so this object can be used as a custom yield
        /// instruction to wait until it finishes.
        /// </remarks>
        public override bool IsPlayingAndNotEnding()
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns the <see cref="AnimancerNode.DebugName"/> if one is set, otherwise a string describing the type of this
        /// state and the name of the <see cref="MainObject"/>.
        /// </summary>
        public override string ToString()
        {
            return default;
        }

        /************************************************************************************************************************/
        #region Descriptions
        /************************************************************************************************************************/

#if UNITY_EDITOR
        /// <summary>[Editor-Only] Returns a custom drawer for this state.</summary>
        protected internal virtual IAnimancerNodeDrawer CreateDrawer()
            => new AnimancerStateDrawer<AnimancerState>(this);
#endif

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void AppendDetails(StringBuilder text, string separator)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Returns the hierarchy path of this state through its <see cref="Parent"/>s.</summary>
        public string GetPath()
        {
            return default;
        }

        /// <summary>Appends the hierarchy path of this state through its <see cref="Parent"/>s.</summary>
        private static void AppendPath(StringBuilder path, AnimancerNode parent)
        {
        }

        /// <summary>Appends "[Index] -> GetType().Name".</summary>
        private void AppendPortAndType(StringBuilder path)
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

