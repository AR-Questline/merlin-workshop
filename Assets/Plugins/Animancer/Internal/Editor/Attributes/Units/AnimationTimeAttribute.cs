// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR
using Animancer.Editor;
using System;
using UnityEditor;
using UnityEngine;
#endif

namespace Animancer.Units
{
    /// <summary>[Editor-Conditional] Causes a float field to display using 3 fields: Normalized, Seconds, and Frames.</summary>
    /// <remarks>
    /// Documentation: <see href="https://kybernetik.com.au/animancer/docs/manual/transitions#time-fields">Time Fields</see>
    /// </remarks>
    /// https://kybernetik.com.au/animancer/api/Animancer.Units/AnimationTimeAttribute
    /// 
    [System.Diagnostics.Conditional(Strings.UnityEditor)]
    public sealed class AnimationTimeAttribute : UnitsAttribute
    {
        /************************************************************************************************************************/

        /// <summary>A unit of measurement used by the <see cref="AnimationTimeAttribute"/>.</summary>
        public enum Units
        {
            /// <summary>A value of 1 represents the end of the animation.</summary>
            Normalized = 0,

            /// <summary>A value of 1 represents 1 second.</summary>
            Seconds = 1,

            /// <summary>A value of 1 represents 1 frame.</summary>
            Frames = 2,
        }

        /// <summary>An explanation of the suffixes used in fields drawn by this attribute.</summary>
        public const string Tooltip = "x = Normalized, s = Seconds, f = Frame";

        /************************************************************************************************************************/

        /// <summary>Cretes a new <see cref="AnimationTimeAttribute"/>.</summary>
        public AnimationTimeAttribute(Units units)
        {
        }

        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/

        /// <summary>[Editor-Only] A converter that adds an 'x' suffix to the given number.</summary>
        public static readonly CompactUnitConversionCache
            XSuffix = new CompactUnitConversionCache("x");

        private static new readonly CompactUnitConversionCache[] DisplayConverters =
        {
            XSuffix,
            new CompactUnitConversionCache("s"),
            new CompactUnitConversionCache("f"),
        };

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] The default value to be used for the next field drawn by this attribute.</summary>
        public static float nextDefaultValue = float.NaN;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override int GetLineCount(SerializedProperty property, GUIContent label)
            => EditorGUIUtility.wideMode || TransitionDrawer.Context == null ? 1 : 2;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void OnGUI(Rect area, SerializedProperty property, GUIContent label)
        {
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Draws the GUI for this attribute.</summary>
        public void OnGUI(Rect area, GUIContent label, ref float value)
        {
        }

        /************************************************************************************************************************/

        private static new readonly float[] Multipliers = new float[3];

        private float[] CalculateMultipliers(float length, float frameRate)
        {
            return default;
        }

        /************************************************************************************************************************/

        private void DoPreviewTimeButton(ref Rect area, ref float value, ITransitionDetailed transition,
            float[] multipliers)
        {
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Options to determine how <see cref="AnimationTimeAttribute"/> displays.</summary>
        [Serializable]
        public class Settings
        {
            /************************************************************************************************************************/

            /// <summary>Should time fields show approximations if the value is too long for the GUI?</summary>
            /// <remarks>This setting is used by <see cref="CompactUnitConversionCache"/>.</remarks>
            [Tooltip("Should time fields show approximations if the value is too long for the GUI?" +
                " For example, '1.111111' could instead show '1.111~'.")]
            public bool showApproximations = true;

            /// <summary>Should the <see cref="Units.Normalized"/> field be shown?</summary>
            /// <remarks>This setting is ignored for fields which directly store the normalized value.</remarks>
            [Tooltip("Should the " + nameof(Units.Normalized) + " field be shown?")]
            public bool showNormalized = true;

            /// <summary>Should the <see cref="Units.Seconds"/> field be shown?</summary>
            /// <remarks>This setting is ignored for fields which directly store the seconds value.</remarks>
            [Tooltip("Should the " + nameof(Units.Seconds) + " field be shown?")]
            public bool showSeconds = true;

            /// <summary>Should the <see cref="Units.Frames"/> field be shown?</summary>
            /// <remarks>This setting is ignored for fields which directly store the frame value.</remarks>
            [Tooltip("Should the " + nameof(Units.Frames) + " field be shown?")]
            public bool showFrames = true;

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/
    }
}

