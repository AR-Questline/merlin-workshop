#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_GAMECORE
#define PIX_AVAILABLE
#endif

using System.Runtime.InteropServices;
using UnityEngine;

namespace Pix
{
#if PIX_AVAILABLE
	internal static class PixWrapperLib
	{
		[DllImport("PixWrapper")] public static extern void StartEvent(in Color color, [MarshalAs(UnmanagedType.LPWStr)] string name);
		[DllImport("PixWrapper")] public static extern void EndEvent();
		[DllImport("PixWrapper")] public static extern void SetMarker(in Color color, [MarshalAs(UnmanagedType.LPWStr)] string name);
		[DllImport("PixWrapper")] public static extern void ReportCounter([MarshalAs(UnmanagedType.LPWStr)] string name, float value);
	}
#endif
}
