// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Animancer
{
    /// <summary>
    /// A layer on which animations can play with their states managed independantly of other layers while blending the
    /// output with those layers.
    /// </summary>
    ///
    /// <remarks>
    /// This class can be used as a custom yield instruction to wait until all animations finish playing.
    /// <para></para>
    /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/blending/layers">Layers</see>
    /// </remarks>
    /// https://kybernetik.com.au/animancer/api/Animancer/AnimancerLayer
    /// 
    public sealed class AnimancerLayer : AnimancerNode, IAnimationClipCollection
    {
        /************************************************************************************************************************/
        #region Fields and Properties
        /************************************************************************************************************************/

        /// <summary>[Internal] Creates a new <see cref="AnimancerLayer"/>.</summary>
        internal AnimancerLayer(AnimancerPlayable root, int index)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Creates and assigns the <see cref="AnimationMixerPlayable"/> managed by this layer.</summary>
        protected override void CreatePlayable(out Playable playable)
            => playable = AnimationMixerPlayable.Create(Root._Graph);

        /************************************************************************************************************************/

        /// <summary>A layer is its own root.</summary>
        public override AnimancerLayer Layer => this;

        /// <summary>The <see cref="AnimancerNode.Root"/> receives the output of the <see cref="Playable"/>.</summary>
        public override IPlayableWrapper Parent => Root;

        /// <summary>Indicates whether child playables should stay connected to this layer at all times.</summary>
        public override bool KeepChildrenConnected => Root.KeepChildrenConnected;

        /************************************************************************************************************************/

        /// <summary>All of the animation states connected to this layer.</summary>
        private readonly List<AnimancerState> States = new List<AnimancerState>();

        /************************************************************************************************************************/

        private AnimancerState _CurrentState;

        /// <summary>The state of the animation currently being played.</summary>
        /// <remarks>
        /// Specifically, this is the state that was most recently started using any of the Play or CrossFade methods
        /// on this layer. States controlled individually via methods in the <see cref="AnimancerState"/> itself will
        /// not register in this property.
        /// <para></para>
        /// Each time this property changes, the <see cref="CommandCount"/> is incremented.
        /// </remarks>
        public AnimancerState CurrentState
        {
            get => _CurrentState;
            private set
            {
                _CurrentState = value;
                CommandCount++;
            }
        }

        /// <summary>
        /// The number of times the <see cref="CurrentState"/> has changed. By storing this value and later comparing
        /// the stored value to the current value, you can determine whether the state has been changed since then,
        /// even it has changed back to the same state.
        /// </summary>
        public int CommandCount { get; private set; }

#if UNITY_EDITOR
        /// <summary>[Editor-Only] [Internal] Increases the <see cref="CommandCount"/> by 1.</summary>
        internal void IncrementCommandCount() => CommandCount++;
#endif

        /************************************************************************************************************************/

        /// <summary>[Pro-Only]
        /// Determines whether this layer is set to additive blending. Otherwise it will override any earlier layers.
        /// </summary>
        public bool IsAdditive
        {
            get => Root.Layers.IsAdditive(Index);
            set => Root.Layers.SetAdditive(Index, value);
        }

        /************************************************************************************************************************/

        /// <summary>[Pro-Only]
        /// Sets an <see cref="AvatarMask"/> to determine which bones this layer will affect.
        /// </summary>
        public void SetMask(AvatarMask mask)
        {
        }

#if UNITY_ASSERTIONS
        /// <summary>[Assert-Only] The <see cref="AvatarMask"/> that determines which bones this layer will affect.</summary>
        internal AvatarMask _Mask;
#endif

        /************************************************************************************************************************/

        /// <summary>
        /// The average velocity of the root motion of all currently playing animations, taking their current
        /// <see cref="AnimancerNode.Weight"/> into account.
        /// </summary>
        public Vector3 AverageVelocity
        {
            get
            {
                var velocity = default(Vector3);

                for (int i = States.Count - 1; i >= 0; i--)
                {
                    var state = States[i];
                    velocity += state.AverageVelocity * state.Weight;
                }

                return velocity;
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Child States
        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override int ChildCount => States.Count;

        /// <summary>Returns the state connected to the specified `index` as a child of this layer.</summary>
        /// <remarks>This method is identical to <see cref="this[int]"/>.</remarks>
        public override AnimancerState GetChild(int index) => States[index];

        /// <summary>Returns the state connected to the specified `index` as a child of this layer.</summary>
        /// <remarks>This indexer is identical to <see cref="GetChild(int)"/>.</remarks>
        public AnimancerState this[int index] => States[index];

        /************************************************************************************************************************/

        /// <summary>Adds a new port and uses <see cref="AnimancerState.SetParent"/> to connect the `state` to it.</summary>
        public void AddChild(AnimancerState state)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Connects the `state` to this layer at its <see cref="AnimancerNode.Index"/>.</summary>
        protected internal override void OnAddChild(AnimancerState state) => OnAddChild(States, state);

        /************************************************************************************************************************/

        /// <summary>Disconnects the `state` from this layer at its <see cref="AnimancerNode.Index"/>.</summary>
        protected internal override void OnRemoveChild(AnimancerState state)
        {
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override FastEnumerator<AnimancerState> GetEnumerator()
            => new FastEnumerator<AnimancerState>(States);

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Create State
        /************************************************************************************************************************/

        /// <summary>Creates and returns a new <see cref="ClipState"/> to play the `clip`.</summary>
        /// <remarks>
        /// <see cref="AnimancerPlayable.GetKey"/> is used to determine the <see cref="AnimancerState.Key"/>.
        /// </remarks>
        public ClipState CreateState(AnimationClip clip)
            => CreateState(Root.GetKey(clip), clip);

        /// <summary>
        /// Creates and returns a new <see cref="ClipState"/> to play the `clip` and registers it with the `key`.
        /// </summary>
        public ClipState CreateState(object key, AnimationClip clip)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Returns a state registered with the `key` and attached to this layer or null if none exist.</summary>
        /// <exception cref="ArgumentNullException">The `key` is null.</exception>
        /// <remarks>
        /// If a state is registered with the `key` but on a different layer, this method will use that state as the
        /// key and try to look up another state with it. This allows it to associate multiple states with the same
        /// original key.
        /// </remarks>
        public AnimancerState GetState(ref object key)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Calls <see cref="GetOrCreateState(AnimationClip, bool)"/> for each of the specified clips.
        /// <para></para>
        /// If you only want to create a single state, use <see cref="CreateState(AnimationClip)"/>.
        /// </summary>
        public void CreateIfNew(AnimationClip clip0, AnimationClip clip1)
        {
        }

        /// <summary>
        /// Calls <see cref="GetOrCreateState(AnimationClip, bool)"/> for each of the specified clips.
        /// <para></para>
        /// If you only want to create a single state, use <see cref="CreateState(AnimationClip)"/>.
        /// </summary>
        public void CreateIfNew(AnimationClip clip0, AnimationClip clip1, AnimationClip clip2)
        {
        }

        /// <summary>
        /// Calls <see cref="GetOrCreateState(AnimationClip, bool)"/> for each of the specified clips.
        /// <para></para>
        /// If you only want to create a single state, use <see cref="CreateState(AnimationClip)"/>.
        /// </summary>
        public void CreateIfNew(AnimationClip clip0, AnimationClip clip1, AnimationClip clip2, AnimationClip clip3)
        {
        }

        /// <summary>
        /// Calls <see cref="GetOrCreateState(AnimationClip, bool)"/> for each of the specified clips.
        /// <para></para>
        /// If you only want to create a single state, use <see cref="CreateState(AnimationClip)"/>.
        /// </summary>
        public void CreateIfNew(params AnimationClip[] clips)
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Calls <see cref="AnimancerPlayable.GetKey"/> and returns the state registered with that key or
        /// creates one if it doesn't exist.
        /// <para></para>
        /// If the state already exists but has the wrong <see cref="AnimancerState.Clip"/>, the `allowSetClip`
        /// parameter determines what will happen. False causes it to throw an <see cref="ArgumentException"/> while
        /// true allows it to change the <see cref="AnimancerState.Clip"/>. Note that the change is somewhat costly to
        /// performance to use with caution.
        /// </summary>
        /// <exception cref="ArgumentException"/>
        public AnimancerState GetOrCreateState(AnimationClip clip, bool allowSetClip = false)
        {
            return default;
        }

        /// <summary>
        /// Returns the state registered with the <see cref="IHasKey.Key"/> if there is one. Otherwise
        /// this method uses <see cref="ITransition.CreateState"/> to create a new one and registers it with
        /// that key before returning it.
        /// </summary>
        public AnimancerState GetOrCreateState(ITransition transition)
        {
            return default;
        }

        /// <summary>Returns the state registered with the `key` or creates one if it doesn't exist.</summary>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException">The `key` is null.</exception>
        /// <remarks>
        /// If the state already exists but has the wrong <see cref="AnimancerState.Clip"/>, the `allowSetClip`
        /// parameter determines what will happen. False causes it to throw an <see cref="ArgumentException"/> while
        /// true allows it to change the <see cref="AnimancerState.Clip"/>. Note that the change is somewhat costly to
        /// performance to use with caution.
        /// <para></para>
        /// See also: <see cref="AnimancerPlayable.StateDictionary.GetOrCreate(object, AnimationClip, bool)"/>.
        /// </remarks>
        public AnimancerState GetOrCreateState(object key, AnimationClip clip, bool allowSetClip = false)
        {
            return default;
        }

        /// <summary>Returns the `state` if it's a child of this layer. Otherwise makes a clone of it.</summary>
        public AnimancerState GetOrCreateState(AnimancerState state)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// The maximum <see cref="AnimancerNode.Weight"/> that <see cref="GetOrCreateWeightlessState"/> will treat as
        /// being weightless. Default = 0.1.
        /// </summary>
        /// <remarks>This allows states with very small weights to be reused instead of needing to create new ones.</remarks>
        public static float WeightlessThreshold { get; set; } = 0.1f;

        /// <summary>
        /// The maximum number of duplicate states that can be created for a single clip when trying to get a
        /// weightless state. Exceeding this limit will cause it to just use the state with the lowest weight.
        /// Default = 3.
        /// </summary>
        public static int MaxCloneCount { get; private set; } = 3;

        /// <summary>
        /// If the `state`'s <see cref="AnimancerNode.Weight"/> is not currently low, this method finds or creates a
        /// copy of it which is low. he returned <see cref="AnimancerState.Time"/> is also set to 0.
        /// </summary>
        /// <remarks>
        /// If this method would exceed the <see cref="MaxCloneCount"/>, it returns the clone with the lowest weight.
        /// <para></para>
        /// "Low" weight is defined as less than or equal to the <see cref="WeightlessThreshold"/>.
        /// <para></para>
        /// The <see href="https://kybernetik.com.au/animancer/docs/manual/blending/fading/modes">Fade Modes</see> page
        /// explains why clones are created.
        /// </remarks>
        public AnimancerState GetOrCreateWeightlessState(AnimancerState state)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Destroys all states connected to this layer.</summary>
        /// <remarks>This operation cannot be undone.</remarks>
        public void DestroyStates()
        {
        }

        public void RemoveState(object key)
        {
        }

        public void RemoveState(AnimancerState state)
        {
        }

        public void DestroyCurrentState()
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Play Management
        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected internal override void OnStartFade()
        {
        }

        /************************************************************************************************************************/
        // Play Immediately.
        /************************************************************************************************************************/

        /// <summary>Stops all other animations on this layer, plays the `clip`, and returns its state.</summary>
        /// <remarks>
        /// The animation will continue playing from its current <see cref="AnimancerState.Time"/>.
        /// To restart it from the beginning you can use <c>...Play(clip).Time = 0;</c>.
        /// <para></para>
        /// This method is safe to call repeatedly without checking whether the `clip` was already playing.
        /// </remarks>
        public AnimancerState Play(AnimationClip clip)
            => Play(GetOrCreateState(clip));

        /// <summary>Stops all other animations on the same layer, plays the `state`, and returns it.</summary>
        /// <remarks>
        /// The animation will continue playing from its current <see cref="AnimancerState.Time"/>.
        /// To restart it from the beginning you can use <c>...Play(state).Time = 0;</c>.
        /// <para></para>
        /// This method is safe to call repeatedly without checking whether the `state` was already playing.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// The <see cref="AnimancerState.Parent"/> is another state (likely a <see cref="ManualMixerState"/>).
        /// It must be either null or a layer.
        /// </exception>
        public AnimancerState Play(AnimancerState state)
        {
            return default;
        }

        /************************************************************************************************************************/
        // Cross Fade.
        /************************************************************************************************************************/

        /// <summary>
        /// Starts fading in the `clip` over the course of the `fadeDuration` while fading out all others in the same
        /// layer. Returns its state.
        /// </summary>
        /// <remarks>
        /// If the `state` was already playing and fading in with less time remaining than the `fadeDuration`, this
        /// method will allow it to complete the existing fade rather than starting a slower one.
        /// <para></para>
        /// If the layer currently has 0 <see cref="AnimancerNode.Weight"/>, this method will fade in the layer itself
        /// and simply <see cref="AnimancerState.Play"/> the `state`.
        /// <para></para>
        /// This method is safe to call repeatedly without checking whether the `state` was already playing.
        /// <para></para>
        /// <em>Animancer Lite only allows the default `fadeDuration` (0.25 seconds) in runtime builds.</em>
        /// </remarks>
        public AnimancerState Play(AnimationClip clip, float fadeDuration, FadeMode mode = default)
            => Play(GetOrCreateState(clip), fadeDuration, mode);

        /// <summary>
        /// Starts fading in the `state` over the course of the `fadeDuration` while fading out all others in this
        /// layer. Returns the `state`.
        /// </summary>
        /// <remarks>
        /// If the `state` was already playing and fading in with less time remaining than the `fadeDuration`, this
        /// method will allow it to complete the existing fade rather than starting a slower one.
        /// <para></para>
        /// If the layer currently has 0 <see cref="AnimancerNode.Weight"/>, this method will fade in the layer itself
        /// and simply <see cref="AnimancerState.Play"/> the `state`.
        /// <para></para>
        /// This method is safe to call repeatedly without checking whether the `state` was already playing.
        /// <para></para>
        /// <em>Animancer Lite only allows the default `fadeDuration` (0.25 seconds) in runtime builds.</em>
        /// </remarks>
        public AnimancerState Play(AnimancerState state, float fadeDuration, FadeMode mode = default)
        {
            return default;
        }

        /************************************************************************************************************************/
        // Transition.
        /************************************************************************************************************************/

        /// <summary>
        /// Creates a state for the `transition` if it didn't already exist, then calls
        /// <see cref="Play(AnimancerState)"/> or <see cref="Play(AnimancerState, float, FadeMode)"/>
        /// depending on the <see cref="ITransition.FadeDuration"/>.
        /// </summary>
        /// <remarks>
        /// This method is safe to call repeatedly without checking whether the `transition` was already playing.
        /// </remarks>
        public AnimancerState Play(ITransition transition)
            => Play(transition, transition.FadeDuration, transition.FadeMode);

        /// <summary>
        /// Creates a state for the `transition` if it didn't already exist, then calls
        /// <see cref="Play(AnimancerState)"/> or <see cref="Play(AnimancerState, float, FadeMode)"/>
        /// depending on the <see cref="ITransition.FadeDuration"/>.
        /// </summary>
        /// <remarks>
        /// This method is safe to call repeatedly without checking whether the `transition` was already playing.
        /// </remarks>
        public AnimancerState Play(ITransition transition, float fadeDuration, FadeMode mode = default)
        {
            return default;
        }

        /************************************************************************************************************************/
        // Try Play.
        /************************************************************************************************************************/

        /// <summary>
        /// Stops all other animations on the same layer, plays the animation registered with the `key`, and returns
        /// that state. Or if no state is registered with that `key`, this method does nothing and returns null.
        /// </summary>
        /// <remarks>
        /// The animation will continue playing from its current <see cref="AnimancerState.Time"/>.
        /// To restart it from the beginning you can simply set the returned state's time to 0.
        /// <para></para>
        /// This method is safe to call repeatedly without checking whether the animation was already playing.
        /// </remarks>
        public AnimancerState TryPlay(object key)
            => Root.States.TryGet(key, out var state) ? Play(state) : null;

        /// <summary>
        /// Starts fading in the animation registered with the `key` while fading out all others in the same layer
        /// over the course of the `fadeDuration`. Or if no state is registered with that `key`, this method does
        /// nothing and returns null.
        /// </summary>
        /// <remarks>
        /// If the `state` was already playing and fading in with less time remaining than the `fadeDuration`, this
        /// method will allow it to complete the existing fade rather than starting a slower one.
        /// <para></para>
        /// If the layer currently has 0 <see cref="AnimancerNode.Weight"/>, this method will fade in the layer itself
        /// and simply <see cref="AnimancerState.Play"/> the `state`.
        /// <para></para>
        /// This method is safe to call repeatedly without checking whether the animation was already playing.
        /// <para></para>
        /// <em>Animancer Lite only allows the default `fadeDuration` (0.25 seconds) in runtime builds.</em>
        /// </remarks>
        public AnimancerState TryPlay(object key, float fadeDuration, FadeMode mode = default)
            => Root.States.TryGet(key, out var state) ? Play(state, fadeDuration, mode) : null;

        /************************************************************************************************************************/

        /// <summary>Manipulates the other parameters according to the `mode`.</summary>
        /// <exception cref="ArgumentException">
        /// The <see cref="AnimancerState.Clip"/> is null when using <see cref="FadeMode.FromStart"/> or
        /// <see cref="FadeMode.NormalizedFromStart"/>.
        /// </exception>
        private void EvaluateFadeMode(FadeMode mode, ref AnimancerState state, ref float fadeDuration, out float layerFadeDuration)
        {
            layerFadeDuration = default(float);
        }

        /************************************************************************************************************************/
        // Stopping
        /************************************************************************************************************************/

        /// <summary>
        /// Sets <see cref="AnimancerNode.Weight"/> = 0 and calls <see cref="AnimancerState.Stop"/> on all animations
        /// to stop them from playing and rewind them to the start.
        /// </summary>
        public override void Stop()
        {
        }

        /************************************************************************************************************************/
        // Checking
        /************************************************************************************************************************/

        /// <summary>
        /// Returns true if the `clip` is currently being played by at least one state.
        /// </summary>
        public bool IsPlayingClip(AnimationClip clip)
        {
            return default;
        }

        /// <summary>
        /// Returns true if at least one animation is being played.
        /// </summary>
        public bool IsAnyStatePlaying()
        {
            return default;
        }

        /// <summary>
        /// Returns true if the <see cref="CurrentState"/> is playing and hasn't yet reached its end.
        /// <para></para>
        /// This method is called by <see cref="IEnumerator.MoveNext"/> so this object can be used as a custom yield
        /// instruction to wait until it finishes.
        /// </summary>
        public override bool IsPlayingAndNotEnding()
            => _CurrentState != null && _CurrentState.IsPlayingAndNotEnding();

        /************************************************************************************************************************/

        /// <summary>
        /// Calculates the total <see cref="AnimancerNode.Weight"/> of all states in this layer.
        /// </summary>
        public float GetTotalWeight()
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
        #region Inspector
        /************************************************************************************************************************/

        /// <summary>[<see cref="IAnimationClipCollection"/>]
        /// Gathers all the animations in this layer.
        /// </summary>
        public void GatherAnimationClips(ICollection<AnimationClip> clips) => clips.GatherFromSource(States);

        /************************************************************************************************************************/

        /// <summary>The Inspector display name of this layer.</summary>
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
        #endregion
        /************************************************************************************************************************/
    }
}

