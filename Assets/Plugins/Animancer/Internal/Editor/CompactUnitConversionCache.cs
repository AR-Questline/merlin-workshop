// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2023 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only]
    /// A system for formatting floats as strings that fit into a limited area and storing the results so they can be
    /// reused to minimise the need for garbage collection, particularly for string construction.
    /// </summary>
    /// <example>
    /// With <c>"x"</c> as the suffix:
    /// <list type="bullet">
    /// <item><c>1.111111</c> could instead show <c>1.111~x</c>.</item>
    /// <item><c>0.00001234567</c> would normally show <c>1.234567e-05</c>, but with this it instead shows <c>0~x</c>
    /// because very small values generally aren't useful.</item>
    /// <item><c>99999999</c> shows <c>1e+08x</c> because very large values are already approximations and trying to
    /// format them correctly would be very difficult.</item>
    /// </list>
    /// This system only affects the display value. Once you select a field, it shows its actual value.
    /// </example>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/CompactUnitConversionCache
    /// 
    public class CompactUnitConversionCache
    {
        /************************************************************************************************************************/

        /// <summary>The suffix added to the end of each value.</summary>
        public readonly string Suffix;

        /// <summary>The <see cref="Suffix"/> with a <c>~</c> before it to indicate an approximation.</summary>
        public readonly string ApproximateSuffix;

        /// <summary>The value <c>0</c> with the <see cref="Suffix"/>.</summary>
        public readonly string ConvertedZero;

        /// <summary>The value <c>0</c> with the <see cref="ApproximateSuffix"/>.</summary>
        public readonly string ConvertedSmallPositive;

        /// <summary>The value <c>-0</c> with the <see cref="ApproximateSuffix"/>.</summary>
        public readonly string ConvertedSmallNegative;

        /// <summary>The pixel width of the <see cref="Suffix"/> when drawn by <see cref="EditorStyles.numberField"/>.</summary>
        public float _SuffixWidth;

        /// <summary>The caches for each character count.</summary>
        /// <remarks><c>this[x]</c> is a cache that outputs strings with <c>x</c> characters.</remarks>
        private List<ConversionCache<float, string>>
            Caches = new List<ConversionCache<float, string>>();

        /************************************************************************************************************************/

        /// <summary>Strings mapped to the width they would require for a <see cref="EditorStyles.numberField"/>.</summary>
        private static ConversionCache<string, float> _WidthCache;

        /// <summary>Padding around the text in a <see cref="EditorStyles.numberField"/>.</summary>
        public static float _FieldPadding;

        /// <summary>The pixel width of the <c>~</c> character when drawn by <see cref="EditorStyles.numberField"/>.</summary>
        public static float _ApproximateSymbolWidth;

        /// <summary>The character(s) used to separate decimal values in the current OS language.</summary>
        public static string _DecimalSeparator;

        /// <summary>Values smaller than this become <c>0~</c> or <c>-0~</c>.</summary>
        public const float
            SmallExponentialThreshold = 0.0001f;

        /// <summary>Values larger than this can't be approximated.</summary>
        public const float
            LargeExponentialThreshold = 9999999f;

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="CompactUnitConversionCache"/>.</summary>
        public CompactUnitConversionCache(string suffix)
        {
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns a cached string representing the `value` trimmed to fit within the `width` (if necessary) and with
        /// the <see cref="Suffix"/> added on the end.
        /// </summary>
        public string Convert(float value, float width)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Calculate the index of the cache to use for the given parameters.</summary>
        private int CalculateCacheIndex(float value, float width)
        {
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Creates and returns a cache for the specified `characterCount`.</summary>
        private ConversionCache<float, string> GetCache(int characterCount)
        {
            return default;
        }

        /************************************************************************************************************************/

        private static List<string> _ExponentialFormats;

        /// <summary>Returns a format string to include the specified number of `digits` in an exponential number.</summary>
        public static string GetExponentialFormat(int digits)
        {
            return default;
        }

        /************************************************************************************************************************/

        private static void TrimExponential(ref string valueString)
        {
        }

        /************************************************************************************************************************/
    }
}

#endif

