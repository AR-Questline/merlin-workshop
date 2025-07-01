//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;

namespace ChocDino.UIFX
{
	internal static class Log
	{
		private static int _lastFrameLog = -1;
		private static string _lastCaller;
		private static string _lastMessage;

 		[System.Diagnostics.Conditional("UIFX_LOG")]
		[MethodImpl(MethodImplOptions.NoInlining)]  //This will prevent inlining by the complier.
		internal static void LOG(string message, Object obj = null, LogType type = LogType.Log, [System.Runtime.CompilerServices.CallerMemberName] string callerName = "")
        {
        }
    }
}