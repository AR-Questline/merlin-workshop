// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

using System;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Animancer
{
    /// <summary>
    /// A <see cref="callback"/> delegate paired with a <see cref="normalizedTime"/> to determine when to invoke it.
    /// </summary>
    /// <remarks>
    /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/events/animancer">Animancer Events</see>
    /// </remarks>
    /// https://kybernetik.com.au/animancer/api/Animancer/AnimancerEvent
    /// 
    public partial struct AnimancerEvent : IEquatable<AnimancerEvent>
    {
        /************************************************************************************************************************/
        #region Event
        /************************************************************************************************************************/

        /// <summary>The <see cref="AnimancerState.NormalizedTime"/> at which to invoke the <see cref="callback"/>.</summary>
        public float normalizedTime;

        /// <summary>The delegate to invoke when the <see cref="normalizedTime"/> passes.</summary>
        public Action callback;

        /************************************************************************************************************************/

        /// <summary>The largest possible float value less than 1.</summary>
        /// <remarks>
        /// This value is useful for placing events at the end of a looping animation since they do not allow the
        /// <see cref="normalizedTime"/> to be greater than or equal to 1.
        /// </remarks>
        public const float AlmostOne = 0.99999994f;

        /************************************************************************************************************************/

        /// <summary>Does nothing.</summary>
        /// <remarks>This delegate is used for events which would otherwise have a <c>null</c> <see cref="callback"/>.</remarks>
        public static readonly Action DummyCallback = Dummy;

        /// <summary>Does nothing.</summary>
        /// <remarks>Used by <see cref="DummyCallback"/>.</remarks>
        private static void Dummy() {
        }

        /// <summary>Is the `callback` <c>null</c> or the <see cref="DummyCallback"/>?</summary>
        public static bool IsNullOrDummy(Action callback) => callback == null || callback == DummyCallback;

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="AnimancerEvent"/>.</summary>
        public AnimancerEvent(float normalizedTime, Action callback) : this()
        {
        }

        /************************************************************************************************************************/

        /// <summary>Returns a string describing the details of this event.</summary>
        public override string ToString()
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Appends the details of this event to the `text`.</summary>
        public void AppendDetails(StringBuilder text)
        {
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Invocation
        /************************************************************************************************************************/

        /// <summary>The <see cref="AnimancerState"/> currently triggering an event via <see cref="Invoke"/>.</summary>
        public static AnimancerState CurrentState => _CurrentState;
        private static AnimancerState _CurrentState;

        /************************************************************************************************************************/

        /// <summary>The <see cref="AnimancerEvent"/> currently being triggered via <see cref="Invoke"/>.</summary>
        public static ref readonly AnimancerEvent CurrentEvent => ref _CurrentEvent;
        private static AnimancerEvent _CurrentEvent;

        /************************************************************************************************************************/

        /// <summary>
        /// Sets the <see cref="CurrentState"/> and <see cref="CurrentEvent"/> then invokes the <see cref="callback"/>.
        /// </summary>
        /// <remarks>This method catches and logs any exception thrown by the <see cref="callback"/>.</remarks>
        /// <exception cref="NullReferenceException">The <see cref="callback"/> is null.</exception>
        public void Invoke(AnimancerState state)
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns either the <see cref="AnimancerPlayable.DefaultFadeDuration"/> or the
        /// <see cref="AnimancerState.RemainingDuration"/> of the <see cref="CurrentState"/> (whichever is higher).
        /// </summary>
        public static float GetFadeOutDuration()
            => GetFadeOutDuration(CurrentState, AnimancerPlayable.DefaultFadeDuration);

        /// <summary>
        /// Returns either the `minDuration` or the <see cref="AnimancerState.RemainingDuration"/> of the
        /// <see cref="CurrentState"/> (whichever is higher).
        /// </summary>
        public static float GetFadeOutDuration(float minDuration)
            => GetFadeOutDuration(CurrentState, minDuration);

        /// <summary>
        /// Returns either the `minDuration` or the <see cref="AnimancerState.RemainingDuration"/> of the
        /// `state` (whichever is higher).
        /// </summary>
        public static float GetFadeOutDuration(AnimancerState state, float minDuration)
        {
            return default;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Operators
        /************************************************************************************************************************/

        /// <summary>Are the <see cref="normalizedTime"/> and <see cref="callback"/> equal?</summary>
        public static bool operator ==(AnimancerEvent a, AnimancerEvent b)
            => a.Equals(b);

        /// <summary>Are the <see cref="normalizedTime"/> and <see cref="callback"/> not equal?</summary>
        public static bool operator !=(AnimancerEvent a, AnimancerEvent b)
            => !a.Equals(b);

        /************************************************************************************************************************/

        /// <summary>[<see cref="IEquatable{AnimancerEvent}"/>]
        /// Are the <see cref="normalizedTime"/> and <see cref="callback"/> of this event equal to `other`?
        /// </summary>
        public bool Equals(AnimancerEvent other)
            => callback == other.callback
            && (normalizedTime == other.normalizedTime || (float.IsNaN(normalizedTime) && float.IsNaN(other.normalizedTime)));

        /// <inheritdoc/>
        public override bool Equals(object obj)
            => obj is AnimancerEvent animancerEvent
            && Equals(animancerEvent);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return default;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

