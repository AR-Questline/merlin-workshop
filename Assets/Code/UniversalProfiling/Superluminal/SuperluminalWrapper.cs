#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_GAMECORE || UNITY_PS5
#define SUPERLUMINAL_AVAILABLE
#endif

using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Superluminal
{
	public static class SuperluminalWrapper
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		static void Initialize()
		{
#if SUPERLUMINAL_AVAILABLE
			SuperluminalPerf.Initialize();
			SuperluminalPerf.SetCurrentThreadName("Main");
#endif
		}

		public static bool IsAvailable =>
#if SUPERLUMINAL_AVAILABLE
			true
#else
            false
#endif
		;

		[Conditional("AR_DEBUG"), Conditional("DEBUG")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void StartEvent(in Color color, string name)
		{
#if SUPERLUMINAL_AVAILABLE
			SuperluminalPerf.BeginEvent(name, null, GetColor(color));
#endif
		}

		[Conditional("AR_DEBUG"), Conditional("DEBUG")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void StartEvent(in Color color, string name, float value)
		{
#if SUPERLUMINAL_AVAILABLE
			SuperluminalPerf.BeginEvent(name, value.ToString(), GetColor(color));
#endif
		}

		[Conditional("AR_DEBUG"), Conditional("DEBUG")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void EndEvent()
		{
#if SUPERLUMINAL_AVAILABLE
			SuperluminalPerf.EndEvent();
#endif
		}

		[Conditional("AR_DEBUG"), Conditional("DEBUG")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void SetMarker(in Color color, string name)
		{
#if SUPERLUMINAL_AVAILABLE
			SuperluminalPerf.BeginEvent(name, null, GetColor(color));
			SuperluminalPerf.EndEvent();
#endif
		}

		[Conditional("AR_DEBUG"), Conditional("DEBUG")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ReportCounter(string name, float value)
		{
#if SUPERLUMINAL_AVAILABLE
			SuperluminalPerf.BeginEvent(name, value.ToString());
			SuperluminalPerf.EndEvent();
#endif
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static SuperluminalPerf.ProfilerColor GetColor(Color color)
		{
			var r = (byte)(color.r * 255);
			var g = (byte)(color.g * 255);
			var b = (byte)(color.b * 255);
			return new SuperluminalPerf.ProfilerColor(r, g, b);
		}
	}
}
