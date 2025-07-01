//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using System.Collections.Generic;
using UnityEngine;
using UnityInternal = UnityEngine.Internal;

namespace ChocDino.UIFX
{
	/// <summary>
	/// The blending mode to use when combining two colors together
	/// </summary>
	public enum BlendMode
	{
		/// <summary>`Source` - Only use the original color, this ignores any trail gradient/alpha settings.</summary>
		Source,
		/// <summary>`Replace` - Ignore the original color and replace with the trail gradient/alpha settings.<br/>`Replace_Multiply`</summary>
		Replace,
		/// <summary>`Replace_Multiply` - Same as `Replace` for RGB, but multiply the original alpha with the trail gradient alpha.</summary>
		Replace_Multiply,
		/// <summary>`Multiply` - Multiply the original color with the trail gradient/alpha settings.</summary>
		Multiply,
		/// <summary>`Add_Multiply` - Add the original color RGB to the gradient gradient, but multiply the alpha value.</summary>
		Add_Multiply,
	}

	public enum BuiltInGradient
	{
		SoftRainbow,
		Grey80ToClear,
	}

	[UnityInternal.ExcludeFromDocs]
	public static class ColorUtils
	{
		private static Gradient s_softRainbowGradient;
		private static Gradient s_grey80ToClearGradient;

		[UnityInternal.ExcludeFromDocs]
		public static Gradient GetBuiltInGradient(BuiltInGradient b)
        {
            return default;
        }

        static ColorUtils()
        {
        }

        public static Gradient CloneGradient(Gradient gradient)
        {
            return default;
        }

        public static Color Blend(Color a, Color b, BlendMode mode)
        {
            return default;
        }

        public static Color EvalGradient(float t, Gradient gradient, GradientWrapMode wrapMode, float offset = 0f, float scale = 1f, float scalePivot = 0f)
        {
            return default;
        }

        public static Vector3 LinearToOklab(Color c)
        {
            return default;
        }

        // If using .NET core, please use System.Math.Cbrt for cuberoot
        private static float cbrtf(float v)
        {
            return default;
        }

        internal static void ConvertMeshVertexColorsToLinear(Mesh mesh, ref List<Color> colorCache)
        {
        }
    }
}