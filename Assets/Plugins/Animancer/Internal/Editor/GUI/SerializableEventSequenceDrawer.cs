// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using Animancer.Units;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Events;
using Sequence = Animancer.AnimancerEvent.Sequence.Serializable;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only] Draws the Inspector GUI for a <see cref="Sequence"/>.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/SerializableEventSequenceDrawer
    /// 
    [CustomPropertyDrawer(typeof(Sequence), true)]
    public class SerializableEventSequenceDrawer : PropertyDrawer
    {
        /************************************************************************************************************************/

        /// <summary><see cref="AnimancerGUI.RepaintEverything"/></summary>
        public static UnityAction Repaint = AnimancerGUI.RepaintEverything;

        private readonly Dictionary<string, List<AnimBool>>
            EventVisibility = new Dictionary<string, List<AnimBool>>();

        private AnimBool GetVisibility(Context context, int index)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Can't cache because it breaks the <see cref="TimelineGUI"/>.</summary>
        public override bool CanCacheInspectorGUI(SerializedProperty property) => false;

        /************************************************************************************************************************/

        /// <summary>
        /// Calculates the number of vertical pixels the `property` will occupy when it is drawn.
        /// </summary>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return default;
        }

        /************************************************************************************************************************/

        private float CalculateEventHeight(Context context, int index)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Draws the GUI for the `property`.</summary>
        public override void OnGUI(Rect area, SerializedProperty property, GUIContent label)
        {
        }

        /************************************************************************************************************************/

        private void DoHeaderGUI(ref Rect area, GUIContent label, Context context)
        {
        }

        /************************************************************************************************************************/

        private static readonly int EventTimeHash = "EventTime".GetHashCode();

        private static int _HotControlAdjustRoot;
        private static int _SelectedEventToHotControl;

        private void DoAllEventsGUI(ref Rect area, Context context)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Draws the GUI fields for the event at the specified `index`.</summary>
        public void DoEventGUI(ref Rect area, Context context, int index, bool autoSort)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Draws the time field for the event at the specified `index`.</summary>
        public static void DoNameGUI(ref Rect area, Context context, int index, string nameLabel)
        {
        }

        private static string DoEventNameTextField(Rect area, Context context, string text)
        {
            return default;
        }

        /************************************************************************************************************************/

        private static readonly AnimationTimeAttribute
            EventTimeAttribute = new AnimationTimeAttribute(AnimationTimeAttribute.Units.Normalized);

        private static float _PreviousTime = float.NaN;

        /// <summary>Draws the time field for the event at the specified `index`.</summary>
        public static void DoTimeGUI(ref Rect area, Context context, int index, bool autoSort,
            string timeLabel, float defaultTime, bool isEndEvent)
        {
        }

        /// <summary>Draws the time field for the event at the specified `index`.</summary>
        public static void DoTimeGUI(ref Rect area, Context context, int index, bool autoSort)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Updates the <see cref="Sequence.Events"/> to accomodate a changed event time.</summary>
        public static void SyncEventTimeChange(Context context, int index, float normalizedTime)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Draws the GUI fields for the event at the specified `index`.</summary>
        public static void DoCallbackGUI(ref Rect area, Context context, int index, bool autoSort, string callbackLabel)
        {
        }

        /************************************************************************************************************************/

        private static ConversionCache<int, string> _NameLabelCache, _TimeLabelCache, _CallbackLabelCache;

        private static void GetEventLabels(int index, Context context,
            out string nameLabel, out string timeLabel, out string callbackLabel, out float defaultTime, out bool isEndEvent)
        {
            nameLabel = default(string);
            timeLabel = default(string);
            callbackLabel = default(string);
            defaultTime = default(float);
            isEndEvent = default(bool);
        }

        /************************************************************************************************************************/

        private static void WrapEventTime(Context context, ref float normalizedTime)
        {
        }

        /************************************************************************************************************************/
        #region Event Modification
        /************************************************************************************************************************/

        private static GUIStyle _AddRemoveEventStyle;
        private static GUIContent _AddEventContent;

        /// <summary>Draws a button to add a new event or remove the selected one.</summary>
        public void DoAddRemoveEventButtonGUI(Rect area, Context context)
        {
        }

        /************************************************************************************************************************/

        private static bool ShowAddButton(Context context)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Adds an event to the sequence represented by the given `context`.</summary>
        public static void AddEvent(Context context, float normalizedTime)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Removes the event at the specified `index`.</summary>
        public static void RemoveEvent(Context context, int index)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Sorts the events in the `context` according to their times.</summary>
        private static bool SortEvents(Context context)
        {
            return default;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Context
        /************************************************************************************************************************/

        /// <summary>Details of an <see cref="AnimancerEvent.Sequence.Serializable"/>.</summary>
        public class Context : IDisposable
        {
            /************************************************************************************************************************/

            /// <summary>The main property representing the <see cref="Sequence"/> field.</summary>
            public SerializedProperty Property { get; private set; }

            private Sequence _Sequence;

            /// <summary>Underlying value of the <see cref="Property"/>.</summary>
            public Sequence Sequence
            {
                get
                {
                    if (_Sequence == null && Property.serializedObject.targetObjects.Length == 1)
                        _Sequence = Property.GetValue<Sequence>();
                    return _Sequence;
                }
            }

            /// <summary>The property representing the <see cref="Sequence._NormalizedTimes"/> field.</summary>
            public readonly SerializedArrayProperty Times = new SerializedArrayProperty();

            /// <summary>The property representing the <see cref="Sequence._Names"/> field.</summary>
            public readonly SerializedArrayProperty Names = new SerializedArrayProperty();

            /// <summary>The property representing the <see cref="Sequence._Callbacks"/> field.</summary>
            public readonly SerializedArrayProperty Callbacks = new SerializedArrayProperty();

            /************************************************************************************************************************/

            private int _SelectedEvent;

            /// <summary>The index of the currently selected event.</summary>
            public int SelectedEvent
            {
                get => _SelectedEvent;
                set
                {
                    if (Times != null && value >= 0 && (value < Times.Count || Times.Count == 0))
                    {
                        float normalizedTime;
                        if (Times.Count > 0)
                        {
                            normalizedTime = Times.GetElement(value).floatValue;
                        }
                        else
                        {
                            var transition = TransitionContext?.Transition;
                            var speed = transition != null ? transition.Speed : 1;
                            normalizedTime = AnimancerEvent.Sequence.GetDefaultNormalizedEndTime(speed);
                        }

                        TransitionPreviewWindow.PreviewNormalizedTime = normalizedTime;
                    }

                    if (_SelectedEvent == value &&
                        Callbacks != null)
                        return;

                    _SelectedEvent = value;
                    TemporarySettings.SetSelectedEvent(Callbacks.Property, value);
                }
            }

            /************************************************************************************************************************/

            /// <summary>The stack of active contexts.</summary>
            private static readonly LazyStack<Context> Stack = new LazyStack<Context>();

            /// <summary>The currently active instance.</summary>
            public static Context Current { get; private set; }

            /************************************************************************************************************************/

            /// <summary>Adds a new <see cref="Context"/> representing the `property` to the stack and returns it.</summary>
            public static Context Get(SerializedProperty property)
            {
                return default;
            }

            /// <summary>Sets this <see cref="Context"/> as the <see cref="Current"/> and returns it.</summary>
            public Context SetAsCurrent()
            {
                return default;
            }

            /************************************************************************************************************************/

            private void Initialize(SerializedProperty property)
            {
            }

            /************************************************************************************************************************/

            /// <summary>[<see cref="IDisposable"/>] Calls <see cref="SerializedObject.ApplyModifiedProperties"/>.</summary>
            public void Dispose()
            {
            }

            /************************************************************************************************************************/

            /// <summary>Shorthand for <see cref="TransitionDrawer.Context"/>.</summary>
            public TransitionDrawer.DrawerContext TransitionContext => TransitionDrawer.Context;

            /************************************************************************************************************************/

            /// <summary>Creates a copy of this <see cref="Context"/>.</summary>
            public Context Copy()
            {
                return default;
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

