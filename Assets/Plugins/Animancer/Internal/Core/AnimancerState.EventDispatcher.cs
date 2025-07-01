// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Animancer
{
    /// https://kybernetik.com.au/animancer/api/Animancer/AnimancerState
    partial class AnimancerState
    {
        /************************************************************************************************************************/

        /// <summary>The <see cref="IUpdatable"/> that manages the events of this state.</summary>
        /// <remarks>
        /// This field is null by default, acquires its reference from an <see cref="ObjectPool"/> when accessed, and
        /// if it contains no events at the end of an update it releases the reference back to the pool.
        /// </remarks>
        private EventDispatcher _EventDispatcher;

        /************************************************************************************************************************/

        /// <summary>
        /// A list of <see cref="AnimancerEvent"/>s that will occur while this state plays as well as one that
        /// specifically defines when this state ends.
        /// </summary>
        /// <remarks>
        /// Accessing this property will acquire a spare <see cref="AnimancerEvent.Sequence"/> from the
        /// <see cref="ObjectPool"/> if none was already assigned. You can use <see cref="HasEvents"/> to check
        /// beforehand.
        /// <para></para>
        /// These events will automatically be cleared by <see cref="Play"/>, <see cref="Stop"/>, and
        /// <see cref="OnStartFade"/> (unless <see cref="AutomaticallyClearEvents"/> is disabled).
        /// <para></para>
        /// <em>Animancer Lite does not allow the use of events in runtime builds, except for
        /// <see cref="AnimancerEvent.Sequence.OnEnd"/>.</em>
        /// <para></para>
        /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/events/animancer">Animancer Events</see>
        /// </remarks>
        public AnimancerEvent.Sequence Events
        {
            get
            {
                EventDispatcher.Acquire(this);
                return _EventDispatcher.Events;
            }
            set
            {
                if (value != null)
                {
                    EventDispatcher.Acquire(this);
                    _EventDispatcher.Events = value;
                }
                else if (_EventDispatcher != null)
                {
                    _EventDispatcher.Events = null;
                }
            }
        }

        /************************************************************************************************************************/

        /// <summary>Does this state have an <see cref="AnimancerEvent.Sequence"/>?</summary>
        /// <remarks>Accessing <see cref="Events"/> would automatically get one from the <see cref="ObjectPool"/>.</remarks>
        public bool HasEvents => _EventDispatcher != null && _EventDispatcher.HasEvents;

        /************************************************************************************************************************/

        /// <summary>
        /// Should the <see cref="Events"/> be cleared automatically whenever <see cref="Play"/>, <see cref="Stop"/>,
        /// or <see cref="OnStartFade"/> are called? Default true.
        /// </summary>
        /// <remarks>
        /// Disabling this property is not usually recommended since it would allow events to continue being triggered
        /// while a state is fading out. For example, if a <em>Flinch</em> animation interrupts an <em>Attack</em>, you
        /// probably don't want the <em>Attack</em>'s <em>Hit</em> event to still get triggered while it's fading out.
        /// <para></para>
        /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/events/animancer#clear-automatically">
        /// Clear Automatically</see>
        /// </remarks>
        public static bool AutomaticallyClearEvents { get; set; } = true;

        /************************************************************************************************************************/

#if UNITY_ASSERTIONS
        /// <summary>[Assert-Only]
        /// Returns <c>null</c> if Animancer Events will work properly on this type of state, or a message explaining
        /// why they might not work.
        /// </summary>
        protected virtual string UnsupportedEventsMessage => null;
#endif

        /************************************************************************************************************************/

        /// <summary>An <see cref="IUpdatable"/> which triggers events in an <see cref="AnimancerEvent.Sequence"/>.</summary>
        /// https://kybernetik.com.au/animancer/api/Animancer/EventDispatcher
        /// 
        public class EventDispatcher : Key, IUpdatable
        {
            /************************************************************************************************************************/
            #region Pooling
            /************************************************************************************************************************/

            /// <summary>
            /// If the `state` has no <see cref="EventDispatcher"/>, this method gets one from the
            /// <see cref="ObjectPool"/>.
            /// </summary>
            internal static void Acquire(AnimancerState state)
            {
            }

            /************************************************************************************************************************/

            /// <summary>Returns this <see cref="EventDispatcher"/> to the <see cref="ObjectPool"/>.</summary>
            private void Release()
            {
            }

            /************************************************************************************************************************/

            /// <summary>
            /// If the <see cref="AnimancerEvent.Sequence"/> was acquired from the <see cref="ObjectPool"/>, this
            /// method clears it. Otherwise it simply discards the reference.
            /// </summary>
            internal static void TryClear(EventDispatcher events)
            {
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/

            private AnimancerState _State;
            private AnimancerEvent.Sequence _Events;
            private bool _GotEventsFromPool;
            private bool _IsLooping;
            private float _PreviousTime;
            private int _NextEventIndex = RecalculateEventIndex;
            private int _SequenceVersion;
            private bool _WasPlayingForwards;

            /// <summary>
            /// A special value used by the <see cref="_NextEventIndex"/> to indicate that it needs to be recalculated.
            /// </summary>
            private const int RecalculateEventIndex = int.MinValue;

            /// <summary>
            /// This system accounts for external modifications to the sequence, but modifying it while checking which
            /// of its events to update is not allowed because it would be impossible to efficiently keep track of
            /// which events have been checked/invoked and which still need to be checked.
            /// </summary>
            private const string SequenceVersionException =
                nameof(AnimancerState) + "." + nameof(AnimancerState.Events) + " sequence was modified while iterating through it." +
                " Events in a sequence must not modify that sequence.";

            /************************************************************************************************************************/

            /// <summary>Does this dispatcher have an <see cref="AnimancerEvent.Sequence"/>?</summary>
            /// <remarks>Accessing <see cref="Events"/> would automatically get one from the <see cref="ObjectPool"/>.</remarks>
            public bool HasEvents => _Events != null;

            /************************************************************************************************************************/

            /// <summary>The events managed by this dispatcher.</summary>
            /// <remarks>If <c>null</c>, a new sequence will be acquired from the <see cref="ObjectPool"/>.</remarks>
            internal AnimancerEvent.Sequence Events
            {
                get
                {
                    if (_Events == null)
                    {
                        ObjectPool.Acquire(out _Events);
                        _GotEventsFromPool = true;

#if UNITY_ASSERTIONS
                        if (!_Events.IsEmpty)
                            Debug.LogError(_Events + " is not in its default state even though it was in the list of spares.",
                            _State?.Root?.Component as Object);
#endif
                    }

                    return _Events;
                }
                set
                {
                    if (_GotEventsFromPool)
                    {
                        _Events.Clear();
                        ObjectPool.Release(_Events);
                        _GotEventsFromPool = false;
                    }

                    _Events = value;
                    _NextEventIndex = RecalculateEventIndex;
                }
            }

            /************************************************************************************************************************/

            void IUpdatable.Update()
            {
            }

            /************************************************************************************************************************/
            #region End Event Validation
            /************************************************************************************************************************/

#if UNITY_ASSERTIONS
            private bool _LoggedEndEventInterrupt;

            private static AnimancerLayer _BeforeEndLayer;
            private static int _BeforeEndCommandCount;
#endif

            /************************************************************************************************************************/

            /// <summary>[Assert-Conditional]
            /// Called after the <see cref="AnimancerEvent.Sequence.EndEvent"/> is triggered to log a warning if the
            /// <see cref="_State"/> was not interrupted or the `callback` contains multiple calls to the same method.
            /// </summary>
            /// <remarks>
            /// It would be better if we could validate the callback when it is assigned to get a useful stack trace,
            /// but that is unfortunately not possible since <see cref="AnimancerEvent.Sequence.EndEvent"/> needs to be
            /// a field for efficiency.
            /// </remarks>
            [System.Diagnostics.Conditional(Strings.Assertions)]
            private void ValidateBeforeEndEvent()
            {
            }

            /************************************************************************************************************************/

            /// <summary>[Assert-Conditional]
            /// Called after the <see cref="AnimancerEvent.Sequence.EndEvent"/> is triggered to log a warning if the
            /// <see cref="_State"/> was not interrupted or the `callback` contains multiple calls to the same method.
            /// </summary>
            /// <remarks>
            /// It would be better if we could validate the callback when it is assigned to get a useful stack trace,
            /// but that is unfortunately not possible since <see cref="AnimancerEvent.Sequence.EndEvent"/> needs to be
            /// a field for efficiency.
            /// </remarks>
            [System.Diagnostics.Conditional(Strings.Assertions)]
            private void ValidateAfterEndEvent(Action callback)
            {
            }

            /************************************************************************************************************************/

#if UNITY_ASSERTIONS
            /// <summary>Should <see cref="OptionalWarning.EndEventInterrupt"/> be logged?</summary>
            private bool ShouldLogEndEventInterrupt(Action callback)
            {
                return default;
            }
#endif

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/

            /// <summary>Notifies this dispatcher that the target's <see cref="Time"/> has changed.</summary>
            internal void OnTimeChanged()
            {
            }

            internal void OnTimeChangedWithEventCheck()
            {
            }

            /************************************************************************************************************************/

            /// <summary>If the state has zero length, trigger its end event every frame.</summary>
            private void UpdateZeroLength()
            {
            }

            /************************************************************************************************************************/

            private void CheckGeneralEvents(float currentTime)
            {
            }

            /************************************************************************************************************************/

            private void ValidateNextEventIndex(ref float currentTime,
                out float playDirectionFloat, out int playDirectionInt)
            {
                playDirectionFloat = default(float);
                playDirectionInt = default(int);
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Calculates the number of times an event at `eventTime` should be invoked when the
            /// <see cref="NormalizedTime"/> goes from `previousTime` to `nextTime` on a looping animation.
            /// </summary>
            private static int GetLoopDelta(float previousTime, float nextTime, float eventTime)
            {
                return default;
            }

            /************************************************************************************************************************/

            private bool InvokeAllEvents(int count, int playDirectionInt)
            {
                return default;
            }

            /************************************************************************************************************************/

            private bool NextEvent(int playDirectionInt)
            {
                return default;
            }

            /************************************************************************************************************************/

            private bool NextEventLooped(int playDirectionInt)
            {
                return default;
            }

            /************************************************************************************************************************/

            /// <summary>Returns "<see cref="EventDispatcher"/> (Target State)".</summary>
            public override string ToString()
            {
                return default;
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
    }
}

