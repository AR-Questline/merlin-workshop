//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

//#define UIFX_OLD178_RESOLUTION_SCALING

using UnityEngine;

namespace ChocDino.UIFX
{
	[System.Flags]
	public enum PerformanceHint
	{
		Default = 0,
		UseLessPrecision = 1 << 0,
		UseMorePrecision = 1 << 1,
		AllowDownsampling = 1 << 2,
	}
	
	public class Filters
	{
#if UIFX_OLD178_RESOLUTION_SCALING
		/// <summary>
		/// Reference resolution that all pixel values are relative to.
		/// Used for calculating resolution independent pixel values.
		/// </summary>
		public static Vector2Int ReferenceResolution = new Vector2Int(1920, 1080);
#endif

		/// <summary>
		/// Override the maximum texture size allowed.
		/// Set to <= 0 is the default which means no override, uses the hardware limits 
		/// </summary>
		public static int LimitTextureResolution = 0;

		private const PerformanceHint PerfHint_Low = PerformanceHint.UseLessPrecision | PerformanceHint.AllowDownsampling;
		private const PerformanceHint PerfHint_Default = PerformanceHint.Default;

		#if !UNITY_EDITOR
			#if UNITY_ANDROID || UNITY_IOS || UNITY_VISIONOS || UNITY_TVOS
				public readonly static PerformanceHint PerfHint = PerfHint_Low;
			#else
				public readonly static PerformanceHint PerfHint = PerfHint_Default;
			#endif
		#else
			public readonly static PerformanceHint PerfHint = PerfHint_Default;
		#endif

#if UIFX_OLD178_RESOLUTION_SCALING
		internal static float GetScaling(Camera renderCamera)
		{
			if (renderCamera != null)
			{
				return GetScaling(new Vector2(renderCamera.pixelWidth, renderCamera.pixelHeight));
			}
			return GetScaling(GetMonitorResolution());
		}

		internal static float GetScaling(Vector2 targetResolution)
		{
			float targetArea = targetResolution.x * targetResolution.y;
			float refArea = ReferenceResolution.x * ReferenceResolution.y;
			float targetDiag = targetResolution.magnitude;
			float refDiag = ReferenceResolution.magnitude;

			float diagRatio = targetDiag / refDiag;
			float areaRatio = targetArea / refArea;
			float widthRatio = targetResolution.x / ReferenceResolution.x;
			float heightRatio = targetResolution.y / ReferenceResolution.y;
			return (diagRatio);// + widthRatio + heightRatio + areaRatio) * 0.25f;//areaRatio;
		}
#endif

		internal static Vector2Int GetMonitorResolution()
        {
            return default;
        }

        internal static int GetMaxiumumTextureSize()
        {
            return default;
        }
    }
}