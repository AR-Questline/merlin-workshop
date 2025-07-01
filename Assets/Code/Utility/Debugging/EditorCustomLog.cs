#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Awaken.Utility.Extensions;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Awaken.Utility.Debugging {
    public class VoidingLogHandler : ILogHandler {
        public void LogFormat(UnityEngine.LogType logType, Object context, string format, params object[] args) { }
        public void LogException(Exception exception, Object context) { }
    }
    
    /// <summary>
    /// This script captures regular unity logging to remove unwanted warnings that cannot be disabled otherwise.
    /// </summary>
    public class EditorCustomLog : ILogHandler {
        public static ILogHandler unityLogHandler;
        public static ILogger unityLogger = Debug.unityLogger;
        static readonly StringBuilder StringBuilder = new();
        
        [InitializeOnLoadMethod]
        public static void Init() {
            unityLogHandler = unityLogger.logHandler;
            if (Application.isBatchMode) return;

            unityLogger.logHandler = new EditorCustomLog();
            EditorApplication.quitting += () => unityLogger.logHandler = new VoidingLogHandler();
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogFormat(UnityEngine.LogType logType, Object context, string format, params object[] args) {
            try {
                if (args.Length > 0 && args[0] is string arg0) {
                    if (LogsToIgnore.Any(s => arg0.Equals(s))) {
                        return;
                    }
                    if (LogsThatContainToIgnore.Any(s => arg0.Contains(s))) {
                        return;
                    }
                }
                Debug.unityLogger.logHandler = unityLogHandler;
                format += "\n" + StackTraceUtils.HrefStackTrace(SanitizeStackTrace(StackTraceUtility.ExtractStackTrace(), logType));
                
                Debug.LogFormat(logType, LogOption.NoStacktrace, context, format, args);
                //Debug.LogFormat(logType, LogOption.None, context, format, args); // Default unity behaviour

            } catch (Exception e) {
                unityLogger.LogException(e);
                unityLogger.LogFormat(logType, context, format, args);

            } finally {
                Debug.unityLogger.logHandler = this;
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogException(Exception exception, Object context) {
            unityLogHandler.LogException(exception, context);
        }

        [Obsolete("Use Log class with no stack parameter instead")]
        public static void LogNoStack(UnityEngine.LogType logType = UnityEngine.LogType.Log, Object context = null, params object[] message) {
            if (Debug.unityLogger.logHandler is VoidingLogHandler) return;
            var handler = Debug.unityLogger.logHandler;
            try {
                Debug.unityLogger.logHandler = unityLogHandler;
                Debug.LogFormat(logType, LogOption.NoStacktrace, context, "{0}", message);
            } finally {
                Debug.unityLogger.logHandler = handler;
            }
        }
        
        [Obsolete("Use Log class with no stack parameter instead")]
        public static void LogFormatNoStack(UnityEngine.LogType logType, string format, Object context = null, params object[] message) {
            if (Debug.unityLogger.logHandler is VoidingLogHandler) return;
            var handler = Debug.unityLogger.logHandler;
            try {
                Debug.unityLogger.logHandler = unityLogHandler;
                Debug.LogFormat(logType, LogOption.NoStacktrace, context, format, message);
            } finally {
                Debug.unityLogger.logHandler = handler;
            }
        }

        [MustUseReturnValue("This method returns a new string and does not modify the original string")]
        static string SanitizeStackTrace(string result, UnityEngine.LogType logType) {
            if (string.IsNullOrEmpty(result)) return result;
            
            StringBuilder.Clear();

            StringBuilder.Append($"  CustomLog:{logType.ToStringFast()}\n(at Assets/Code/Utility/Debugging/EditorCustomLog.cs:43)\n");
            ReadOnlySpan<char> resultSpan = result.AsSpan();

            bool ignoreLine = false;
            int lineStart = 0;
            int lastToCheck = LinesThatContainIgnore.Length;
            string[] startsWith = new string[lastToCheck];
            Array.Copy(LinesThatContainIgnore, startsWith, lastToCheck);
            
            for (int i = 0; i < resultSpan.Length; i++) {
                if (resultSpan[i] == '\n' || i == resultSpan.Length - 1) {
                    if (!ignoreLine) {
                        StringBuilder.Append(resultSpan.Slice(lineStart, i - lineStart));
                        StringBuilder.Append('\n');
                    }
                    lineStart = i + 1;
                    ignoreLine = false;
                    lastToCheck = LinesThatContainIgnore.Length;
                    continue;
                }

                if (ignoreLine) continue;
                
                int indexInLine = i - lineStart;
                for (int j = lastToCheck - 1; j >= 0; j--) {
                    if (startsWith[j].Length <= indexInLine) {
                        ignoreLine = true;
                        break;
                    }
                    if (startsWith[j][indexInLine] != resultSpan[i]) {
                        if (j != lastToCheck - 1) {
                            (startsWith[j], startsWith[lastToCheck - 1]) = (startsWith[lastToCheck - 1], startsWith[j]);
                        }
                        lastToCheck--;
                    }
                }
            }


            return StringBuilder.ToString();
        }

        static readonly string[] LogsToIgnore = {
            "Could not find the member 'm_PropertyDrawer' on internal Unity type 'UnityEditor.PropertyHandler'; cannot correctly set internal Unity state for drawing of custom Unity property drawers - drawers which call EditorGUI.PropertyField or EditorGUILayout.PropertyField will be drawn partially twice.",
            "FMOD Studio: Creating editor system instance",
            "FMOD: Cache updated.",
            "FMOD Studio: Destroying editor system instance",
            "GameAnalytics: REMEMBER THE SDK NEEDS TO BE MANUALLY INITIALIZED NOW",
            "Assertion failed on expression: 'foundQueueIndex'"
        };

        static readonly string[] LogsThatContainToIgnore = {
            "Could not load connection between '",
            "Failed to define "
        };

        static readonly string[] LinesThatContainIgnore = {
            "UnityEngine.Debug:Extract",
            "UnityEngine.StackTraceUtility",
            "Awaken.Utility.Debugging.EditorCustomLog",
            "UnityEngine.Logger:",
            "UnityEngine.Debug:",
            "Awaken.Utility.Debugging.Log"
        };
    }
}
#endif