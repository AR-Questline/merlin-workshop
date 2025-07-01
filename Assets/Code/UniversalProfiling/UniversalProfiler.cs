#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_GAMECORE)
#define PIX_AVAILABLE
#endif
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_GAMECORE || UNITY_PS5
#define SUPERLUMINAL_AVAILABLE
#endif

using System.Diagnostics;
using Pix;
using Superluminal;
using UnityEngine;
using UnityEngine.Profiling;

namespace UniversalProfiling
{
	public static class UniversalProfiler
	{
		[Conditional("AR_DEBUG"), Conditional("DEBUG")]
		public static void StartEvent(in Color color, string name)
		{
#if ENABLE_PROFILER
			Profiler.BeginSample(name);
#endif
#if SUPERLUMINAL_AVAILABLE
			SuperluminalWrapper.StartEvent(color, name);
#endif
#if PIX_AVAILABLE
			PixWrapper.StartEvent(color, name);
#endif
		}

		[Conditional("AR_DEBUG"), Conditional("DEBUG")]
		public static void EndEvent()
		{
#if ENABLE_PROFILER
			Profiler.EndSample();
#endif
#if SUPERLUMINAL_AVAILABLE
			SuperluminalWrapper.EndEvent();
#endif
#if PIX_AVAILABLE
			PixWrapper.EndEvent();
#endif
		}

		[Conditional("AR_DEBUG"), Conditional("DEBUG")]
		public static void SetMarker(in Color color, string name)
		{
#if ENABLE_PROFILER
			Profiler.BeginSample(name);
			Profiler.EndSample();
#endif
#if SUPERLUMINAL_AVAILABLE
			SuperluminalWrapper.SetMarker(color, name);
#endif
#if PIX_AVAILABLE
			PixWrapper.SetMarker(color, name);
#endif
		}

		[Conditional("AR_DEBUG"), Conditional("DEBUG")]
		public static void ReportCounter(string name, float value)
		{
#if SUPERLUMINAL_AVAILABLE
			SuperluminalWrapper.ReportCounter(name, value);
#endif
#if PIX_AVAILABLE
			PixWrapper.ReportCounter(name, value);
#endif
		}
	}
}
