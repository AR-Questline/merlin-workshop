// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using Sequence = Animancer.AnimancerEvent.Sequence;
using Object = UnityEngine.Object;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only] Draws the Inspector GUI for a <see cref="Sequence"/>.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/EventSequenceDrawer
    ///
    public class EventSequenceDrawer
    {
        /************************************************************************************************************************/

        private static readonly ConditionalWeakTable<Sequence, EventSequenceDrawer>
            SequenceToDrawer = new ConditionalWeakTable<Sequence, EventSequenceDrawer>();

        /// <summary>Returns a cached <see cref="EventSequenceDrawer"/> for the `events`.</summary>
        /// <remarks>
        /// The cache uses a <see cref="ConditionalWeakTable{TKey, TValue}"/> so it doesn't prevent the `events`
        /// from being garbage collected.
        /// </remarks>
        public static EventSequenceDrawer Get(Sequence events)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Calculates the number of vertical pixels required to draw the specified `lineCount` using the
        /// <see cref="AnimancerGUI.LineHeight"/> and <see cref="AnimancerGUI.StandardSpacing"/>.
        /// </summary>
        public static float CalculateHeight(int lineCount)
            => lineCount == 0 ? 0 :
                AnimancerGUI.LineHeight * lineCount +
                AnimancerGUI.StandardSpacing * (lineCount - 1);

        /************************************************************************************************************************/

        /// <summary>Calculates the number of vertical pixels required to draw the contents of the `events`.</summary>
        public float CalculateHeight(Sequence events)
            => CalculateHeight(CalculateLineCount(events));

        /// <summary>Calculates the number of lines required to draw the contents of the `events`.</summary>
        public int CalculateLineCount(Sequence events)
        {
            return default;
        }

        /************************************************************************************************************************/

        private bool _IsExpanded;

        private static ConversionCache<int, string> _EventNumberCache;

        private static float _LogButtonWidth = float.NaN;

        /************************************************************************************************************************/

        /// <summary>Draws the GUI for the `events`.</summary>
        public void Draw(ref Rect area, Sequence events, GUIContent label)
        {
        }

        /************************************************************************************************************************/

        private static readonly ConversionCache<int, string>
            SummaryCache = new ConversionCache<int, string>((count) => $"[{count}]"),
            EndSummaryCache = new ConversionCache<int, string>((count) => $"[{count}] + End");

        /// <summary>Returns a summary of the `events`.</summary>
        public static string GetSummary(Sequence events)
        {
            return default;
        }

        /************************************************************************************************************************/

        private static ConversionCache<float, string> _EventTimeCache;

        /// <summary>Draws the GUI for the `animancerEvent`.</summary>
        public static void Draw(ref Rect area, string name, AnimancerEvent animancerEvent)
        {
        }

        /************************************************************************************************************************/

        /// <summary>Calculates the number of vertical pixels required to draw the specified <see cref="Delegate"/>.</summary>
        public static float CalculateHeight(MulticastDelegate del)
            => CalculateHeight(CalculateLineCount(del));

        /// <summary>Calculates the number of lines required to draw the specified <see cref="Delegate"/>.</summary>
        public static int CalculateLineCount(MulticastDelegate del)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Draws the target and name of the specified <see cref="Delegate"/>.</summary>
        public static void DrawInvocationList(ref Rect area, MulticastDelegate del)
        {
        }

        /************************************************************************************************************************/

        private static Delegate[] GetInvocationListIfMulticast(MulticastDelegate del)
            => AnimancerUtilities.TryGetInvocationListNonAlloc(del, out var delegates) ? delegates : del.GetInvocationList();

        /************************************************************************************************************************/

        /// <summary>Draws the target and name of the specified <see cref="Delegate"/>.</summary>
        public static void Draw(ref Rect area, Delegate del)
        {
        }

        /************************************************************************************************************************/
    }
}

#endif

