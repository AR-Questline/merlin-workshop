#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_GAMECORE)
#define PIX_AVAILABLE
#endif

using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Pix
{
	public static class PixWrapper
	{
		public static bool IsAvailable =>
#if PIX_AVAILABLE
			true
#else
			false
#endif
		;

		[Conditional("AR_DEBUG"), Conditional("DEBUG")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void StartEvent(in Color color, string name)
		{
#if PIX_AVAILABLE
			PixWrapperLib.StartEvent(color, name);
#endif
		}

		[Conditional("AR_DEBUG"), Conditional("DEBUG")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void EndEvent()
		{
#if PIX_AVAILABLE
			PixWrapperLib.EndEvent();
#endif
		}

		[Conditional("AR_DEBUG"), Conditional("DEBUG")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void SetMarker(in Color color, string name)
		{
#if PIX_AVAILABLE
			PixWrapperLib.SetMarker(color, name);
#endif
		}

		[Conditional("AR_DEBUG"), Conditional("DEBUG")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ReportCounter(string name, float value)
		{
#if PIX_AVAILABLE
			PixWrapperLib.ReportCounter(name, value);
#endif
		}
	}
}
